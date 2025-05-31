using System.Net;
#if NETFRAMEWORK
using System.Net.Http;
#endif
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OpenFeature.Contrib.Providers.Ofrep.Configuration;
using OpenFeature.Contrib.Providers.Ofrep.Extensions;
using OpenFeature.Contrib.Providers.Ofrep.Models;
using OpenFeature.Model;
using Polly;

namespace OpenFeature.Contrib.Providers.Ofrep.Client;

/// <summary>
/// Exception thrown when there is a configuration error in the OFREP client.
/// </summary>
public class OfrepConfigurationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OfrepConfigurationException"/> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public OfrepConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Implementation of the OFREP HTTP client.
/// </summary>
internal sealed partial class OfrepClient : IOfrepClient
{
    private const string OfrepEvaluatePathPrefix = "/ofrep/v1/evaluate/flags/";
    private const string OfrepBulkEvaluatePath = "/ofrep/v1/evaluate/flags";
    private const string OfrepConfigurationPath = "/ofrep/v1/configuration";
    private const string ErrorCodeProviderNotReady = "provider_not_ready";
    private const string ErrorCodeParsingError = "parsing_error";

    private const string ErrorCodeGeneralError = "general_error";

    // Factor for absolute expiration based on sliding expiration. Consider making configurable if needed.
    private const double AbsoluteExpirationFactor = 5.0;
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger _logger;
    private bool _disposed;
    private TimeSpan _cacheDuration;

    private readonly bool _enableAbsoluteExpiration;

    // Define high-performance logging delegates using source generators
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Debug,
        Message = "Cache hit for key: {CacheKey}")]
    partial void LogCacheHit(string cacheKey);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Trace,
        Message = "Cache miss for key: {CacheKey}")]
    partial void LogCacheMiss(string cacheKey);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Trace,
        Message = "Sending request for {FlagKey} with If-None-Match: {ETag}")]
    partial void LogSendingRequestWithETag(string flagKey, string eTag);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Trace,
        Message = "Sending request for {FlagKey} without If-None-Match header")]
    partial void LogSendingRequestWithoutETag(string flagKey);

    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Debug,
        Message = "Received 304 Not Modified for {FlagKey}. Returning cached value")]
    partial void LogNotModified(string flagKey);

    [LoggerMessage(
        EventId = 1006,
        Level = LogLevel.Warning,
        Message = "Received 304 Not Modified for {FlagKey} but no data in cache entry. Proceeding as failure")]
    partial void LogNotModifiedNoCache(string flagKey);

    [LoggerMessage(
        EventId = 1007,
        Level = LogLevel.Error,
        Message = "Received null response body from server for flag {FlagKey}")]
    partial void LogNullResponse(string flagKey);

    [LoggerMessage(
        EventId = 1008,
        Level = LogLevel.Debug,
        Message = "Cached evaluation for {FlagKey}. Key: {CacheKey}, ETag: {ETag}, Duration: {DurationMs}ms")]
    partial void LogCacheMetrics(string flagKey, string cacheKey, string eTag, double durationMs);

    [LoggerMessage(
        EventId = 1009,
        Level = LogLevel.Error,
        Message = "HTTP request failed for flag {FlagKey}: {Message}")]
    partial void LogHttpRequestFailed(string flagKey, string message, Exception ex);

    [LoggerMessage(
        EventId = 1010,
        Level = LogLevel.Error,
        Message = "Failed to parse JSON response for flag {FlagKey}: {Message}")]
    partial void LogJsonParseError(string flagKey, string message, Exception ex);

    [LoggerMessage(
        EventId = 1011,
        Level = LogLevel.Warning,
        Message = "Request cancelled for flag {FlagKey}")]
    partial void LogRequestCancelled(string flagKey, Exception ex);

    [LoggerMessage(
        EventId = 1012,
        Level = LogLevel.Error,
        Message = "Request timed out for flag {FlagKey}: {Message}")]
    partial void LogRequestTimeout(string flagKey, string message, Exception ex);

    [LoggerMessage(
        EventId = 1013,
        Level = LogLevel.Error,
        Message = "Cache operation error for flag {FlagKey}: {Message}")]
    partial void LogCacheOperationError(string flagKey, string message, Exception ex);

    [LoggerMessage(
        EventId = 1014,
        Level = LogLevel.Warning,
        Message = "Returning stale cache data for key {CacheKey} due to error: {ErrorType}")]
    partial void LogStaleCacheReturn(string cacheKey, string errorType);

    [LoggerMessage(
        EventId = 1015,
        Level = LogLevel.Trace,
        Message = "Sending bulk request for {Operation} with If-None-Match: {ETag}, Key: {CacheKey}")]
    partial void LogBulkRequestWithETag(string operation, string eTag, string cacheKey);

    [LoggerMessage(
        EventId = 1016,
        Level = LogLevel.Trace,
        Message = "Sending bulk request for {Operation} without If-None-Match header, Key: {CacheKey}")]
    partial void LogBulkRequestWithoutETag(string operation, string cacheKey);

    [LoggerMessage(
        EventId = 1017,
        Level = LogLevel.Debug,
        Message = "Received 304 Not Modified for bulk evaluation {Operation}. Key: {CacheKey}, returning cached value")]
    partial void LogBulkNotModified(string operation, string cacheKey);

    [LoggerMessage(
        EventId = 1018,
        Level = LogLevel.Warning,
        Message = "Received 304 Not Modified for bulk evaluation {Operation} but no data in cache entry. Key: {CacheKey}, proceeding as failure")]
    partial void LogBulkNotModifiedNoCache(string operation, string cacheKey);

    [LoggerMessage(
        EventId = 1019,
        Level = LogLevel.Error,
        Message = "Received null response body from server for bulk evaluation {Operation}")]
    partial void LogBulkNullResponse(string operation);

    [LoggerMessage(
        EventId = 1020,
        Level = LogLevel.Debug,
        Message = "Cached bulk evaluation {Operation}. Key: {CacheKey}, ETag: {ETag}, Duration: {DurationMs}ms")]
    partial void LogBulkCacheMetrics(string operation, string cacheKey, string eTag, double durationMs);

    [LoggerMessage(
        EventId = 1021,
        Level = LogLevel.Error,
        Message = "Request or parsing failed for bulk evaluation {Operation}: {ErrorType} - {Message}")]
    partial void LogBulkRequestFailed(string operation, string errorType, string message, Exception ex);

    [LoggerMessage(
        EventId = 1022,
        Level = LogLevel.Warning,
        Message = "Returning stale cache data for bulk key {CacheKey} due to error: {ErrorType}, Operation: {Operation}")]
    partial void LogBulkStaleCacheReturn(string cacheKey, string errorType, string operation, Exception ex);

    [LoggerMessage(
        EventId = 1023,
        Level = LogLevel.Error,
        Message = "Failed to get configuration after retries (if applicable). Error: {ErrorType}, Message: {Message}, Endpoint: {Endpoint}")]
    partial void LogConfigurationError(string errorType, string message, string endpoint, Exception ex);

    [LoggerMessage(
        EventId = 1024,
        Level = LogLevel.Information,
        Message = "Retrying GetConfiguration due to {ExceptionType}. Attempt: {RetryAttempt}")]
    partial void LogRetrying(string exceptionType, int retryAttempt);

    [LoggerMessage(
        EventId = 1025,
        Level = LogLevel.Information,
        Message = "Successfully retrieved OFREP configuration")]
    partial void LogConfigurationSuccess();

    [LoggerMessage(
        EventId = 1026,
        Level = LogLevel.Debug,
        Message = "Setting cache duration to: {Duration}")]
    partial void LogCacheDurationChange(TimeSpan duration);

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly Polly.Retry.AsyncRetryPolicy _getConfigurationRetryPolicy;

    /// <summary>
    /// Creates a new instance of <see cref="OfrepClient"/>.
    /// </summary>
    /// <param name="configuration">The OFREP configuration.</param>
    /// <param name="logger">The logger for the client.</param>
    public OfrepClient(OfrepConfiguration configuration, ILogger logger = null)
        : this(configuration, CreateDefaultHandler(), logger) // Use helper for default handler
    {
    }

    /// <summary>
    /// Internal constructor for testing purposes.
    /// </summary>
    /// <param name="configuration">The OFREP configuration.</param>
    /// <param name="handler">The HTTP message handler.</param>
    /// <param name="logger">The logger for the client.</param>
    internal OfrepClient(OfrepConfiguration configuration, HttpMessageHandler handler, ILogger logger = null)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        configuration.Validate();

        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        _getConfigurationRetryPolicy = Policy
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: 5,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1)),
                onRetry: (exception, timespan, retryAttempt, _) =>
                {
                    LogRetrying(exception.GetType().Name, retryAttempt);
                });
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = configuration.MaxCacheSize > 0 ? (int?)configuration.MaxCacheSize : null
        });
        _cacheDuration = configuration.CacheDuration;
        _enableAbsoluteExpiration = configuration.EnableAbsoluteExpiration;
        _httpClient = new HttpClient(handler, disposeHandler: true)
        {
            BaseAddress = new Uri(configuration.BaseUrl),
            Timeout = TimeSpan.FromSeconds(5)
        };
        if (configuration.Headers != null)
        {
            foreach (var header in configuration.Headers)
            {
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        if (!string.IsNullOrEmpty(configuration.AuthorizationHeader))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", configuration.AuthorizationHeader);
        }
    }

    private static HttpClientHandler CreateDefaultHandler()
    {
        return new HttpClientHandler
        {
            UseProxy = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
    }

    /// <inheritdoc/>
    public async Task<OfrepResponse<T>> EvaluateFlag<T>(string flagKey, string type, T defaultValue,
        EvaluationContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(flagKey))
        {
            throw new ArgumentException("Flag key cannot be null or whitespace", nameof(flagKey));
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Type cannot be null or whitespace", nameof(type));
        }

        var cacheKey = $"flag:{flagKey};type:{type};ctx:{(context ?? EvaluationContext.Empty).GenerateETag()}";
        var etagCacheKey = $"etag:{cacheKey}";
        if (_cache.TryGetValue(cacheKey, out object cachedResponseObject))
        {
            if (cachedResponseObject is OfrepResponse<T> cachedResponse)
            {
                LogCacheHit(cacheKey);
                return cachedResponse;
            }
        }

        LogCacheMiss(cacheKey);
        string cachedETag = null;
        if (_cache.TryGetValue(etagCacheKey, out object cachedETagObject))
        {
            if (cachedETagObject is string eTagValue)
            {
                cachedETag = eTagValue;
            }
        }

        try
        {
            string path = $"{OfrepEvaluatePathPrefix}{Uri.EscapeDataString(flagKey)}";
            var evaluationContextDict = (context ?? EvaluationContext.Empty).ToDictionary();
            var request = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(new OfrepRequest
                {
                    Context = evaluationContextDict,
                }, options: JsonOptions)
            };
            // Add If-None-Match header if we have a cached ETag from the initial check
            if (!string.IsNullOrEmpty(cachedETag))
            {
                request.Headers.IfNoneMatch.Add(
                    new EntityTagHeaderValue(cachedETag, isWeak: true)
                );
                LogSendingRequestWithETag(flagKey, cachedETag);
            }
            else
            {
                LogSendingRequestWithoutETag(flagKey);
            }

            HttpResponseMessage response =
                await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            // Handle 304 Not Modified - re-check cache for the response object
            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                if (_cache.TryGetValue(cacheKey, out object notModifiedResponseObject))
                {
                    if (notModifiedResponseObject is OfrepResponse<T> notModifiedResponse)
                    {
                        LogNotModified(flagKey);
                        return notModifiedResponse;
                    }
                }

                // If cache check fails after 304 (unlikely but possible), log and fall through
                LogNotModifiedNoCache(flagKey);
            }

            response
                .EnsureSuccessStatusCode();
            var evaluationResponse = await response.Content
                .ReadFromJsonAsync<OfrepResponse<T>>(JsonOptions, cancellationToken).ConfigureAwait(false);
            if (evaluationResponse == null)
            {
                LogNullResponse(flagKey);
                return new OfrepResponse<T>
                {
                    Value = defaultValue,
                    ErrorCode = ErrorCodeParsingError,
                    ErrorMessage = "Received null or empty response from server."
                };
            }

            // Get ETag from response if available
            var responseETag = response.Headers.ETag != null ? response.Headers.ETag.Tag : null;

            // Cache the successful response and ETag separately
            var cacheEntryOptions = new MemoryCacheEntryOptions().SetSize(1);
            var etagCacheEntryOptions = new MemoryCacheEntryOptions().SetSize(1);
            // Configure expiration for both
            if (_cacheDuration > TimeSpan.Zero)
            {
                cacheEntryOptions.SetSlidingExpiration(_cacheDuration);
                etagCacheEntryOptions.SetSlidingExpiration(_cacheDuration); // ETag should expire with response
                if (_enableAbsoluteExpiration)
                {
                    var absoluteExpiration =
                        TimeSpan.FromTicks((long)(_cacheDuration.Ticks * AbsoluteExpirationFactor));
                    cacheEntryOptions.SetAbsoluteExpiration(absoluteExpiration);
                    etagCacheEntryOptions.SetAbsoluteExpiration(absoluteExpiration);
                }
            }

            // Set cache entries
            _cache.Set(cacheKey, evaluationResponse, cacheEntryOptions);
            if (responseETag != null)
            {
                _cache.Set(etagCacheKey, responseETag, etagCacheEntryOptions);
            }
            else
            {
                _cache.Remove(etagCacheKey);
            }

            LogCacheMetrics(flagKey, cacheKey, responseETag ?? "N/A", _cacheDuration.TotalMilliseconds);
            return evaluationResponse;
        }
        catch (HttpRequestException ex)
        {
            LogHttpRequestFailed(flagKey, ex.Message, ex);
            return HandleEvaluationError(ex, cacheKey, defaultValue);
        }
        catch (JsonException ex)
        {
            // Failed to parse the JSON response
            LogJsonParseError(flagKey, ex.Message, ex);
            return HandleEvaluationError(ex, cacheKey, defaultValue);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            // Request was cancelled by the caller
            LogRequestCancelled(flagKey, ex);
            return HandleEvaluationError(ex, cacheKey, defaultValue);
        }
        catch (OperationCanceledException ex)
        {
            // Request timed out
            LogRequestTimeout(flagKey, ex.Message, ex);
            return HandleEvaluationError(ex, cacheKey, defaultValue);
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
        {
            LogCacheOperationError(flagKey, ex.Message, ex);
            return HandleEvaluationError(ex, cacheKey, defaultValue);
        }
    }

    /// <inheritdoc/>
    public async Task<BulkEvaluationResponse> BulkEvaluate(EvaluationContext context,
        CancellationToken cancellationToken)
    {
        // Generate cache key using the extension method
        var cacheKey = $"bulk;ctx:{(context ?? EvaluationContext.Empty).GenerateETag()}";
        // Try to get from cache first
        if (_cache.TryGetValue(cacheKey, out object cachedEntry))
        {
            var typedEntry = ((BulkEvaluationResponse, string))cachedEntry;
            LogCacheHit(cacheKey);
            return typedEntry.Item1;
        }

        LogCacheMiss(cacheKey);
        string cachedETag = null;
        try
        {
            var evaluationContextDict = (context ?? EvaluationContext.Empty).ToDictionary();
            var request =
                new HttpRequestMessage(HttpMethod.Post, OfrepBulkEvaluatePath)
                {
                    Content = JsonContent.Create(new OfrepRequest
                    {
                        Context = evaluationContextDict,
                    }, options: JsonOptions)
                };
            // Re-fetch ETag just before the request if needed for If-None-Match
            if (_cache.TryGetValue(cacheKey, out object currentCachedDataObject))
            {
                var currentCachedData = ((BulkEvaluationResponse, string))currentCachedDataObject;
                if (!string.IsNullOrEmpty(currentCachedData.Item2))
                {
                    cachedETag = currentCachedData.Item2;
                    request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(cachedETag,
                        isWeak: true));

                    LogBulkRequestWithETag("BulkEvaluate", cachedETag, cacheKey);
                }
            }
            else
            {
                LogBulkRequestWithoutETag("BulkEvaluate", cacheKey);
            }

            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            // Handle 304 Not Modified - return the previously cached response
            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                // We need to re-fetch from cache here as the initial TryGetValue was only a check
                if (_cache.TryGetValue(cacheKey, out object entryFor304Object))
                {
                    var entryFor304 = ((BulkEvaluationResponse, string))entryFor304Object;
                    if (entryFor304.Item1 != null)
                    {
                        LogBulkNotModified("BulkEvaluate", cacheKey);
                        return entryFor304.Item1;
                    }
                }

                // This case is unlikely if ETag logic is correct, but handle defensively
                LogBulkNotModifiedNoCache("BulkEvaluate", cacheKey);
            }

            response
                .EnsureSuccessStatusCode();

            var evaluationResponse = await response.Content
                .ReadFromJsonAsync<BulkEvaluationResponse>(JsonOptions, cancellationToken).ConfigureAwait(false);
            if (evaluationResponse == null)
            {
                LogBulkNullResponse("BulkEvaluate");
                throw new OfrepConfigurationException(
                    "Received null or empty response from server for bulk evaluation.", null);
            }

            // Get ETag from response if available
            var responseETag = response.Headers.ETag != null ? response.Headers.ETag.Tag : null;
            // Cache the successful response
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSize(1);
            // Configure expiration
            if (_cacheDuration > TimeSpan.Zero)
            {
                cacheEntryOptions.SetSlidingExpiration(_cacheDuration);
                if (_enableAbsoluteExpiration)
                {
                    cacheEntryOptions.SetAbsoluteExpiration(
                        TimeSpan.FromTicks((long)(_cacheDuration.Ticks * AbsoluteExpirationFactor)));
                }
            }

            _cache.Set(cacheKey, (evaluationResponse, responseETag), cacheEntryOptions);
            LogBulkCacheMetrics("BulkEvaluate", cacheKey, responseETag ?? "N/A",
                _cacheDuration.TotalMilliseconds);
            return evaluationResponse;
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is JsonException ||
                                   ex is OperationCanceledException
                                   || ex is ArgumentNullException)
        {
            LogBulkRequestFailed("BulkEvaluate", ex.GetType().Name, ex.Message, ex);
            if (_cache.TryGetValue(cacheKey, out object staleEntryObject))
            {
                var staleEntry = ((BulkEvaluationResponse, string))staleEntryObject;
                LogBulkStaleCacheReturn(cacheKey, ex.GetType().Name, "BulkEvaluate", ex);
                return staleEntry.Item1;
            }

            throw new OfrepConfigurationException(
                $"Failed during OFREP bulk evaluation request to {OfrepBulkEvaluatePath}.", ex);
        }
    }

    /// <summary>
    /// Helper to handle errors during flag evaluation, attempting to return stale cache if available.
    /// </summary>
    private OfrepResponse<T> HandleEvaluationError<T>(Exception ex, string cacheKey, T defaultValue)
    {
        if (_cache.TryGetValue(cacheKey, out object cachedResponseObject))
        {
            if (cachedResponseObject is OfrepResponse<T> staleResponse)
            {
                LogStaleCacheReturn(cacheKey, ex.GetType().Name);
                return staleResponse;
            }
        }

        return new OfrepResponse<T>
        {
            Value = defaultValue,
            ErrorCode = MapExceptionToErrorCode(ex),
            Reason = "ERROR",
            ErrorMessage = ex.Message
        };
    }

    /// <summary>
    /// Retrieves the OFREP provider configuration.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns></returns>
    /// <exception cref="OfrepConfigurationException"></exception>
    public async Task<ConfigurationResponse> GetConfiguration(CancellationToken cancellationToken)
    {
        try
        {
            // Execute the HTTP GET request within the retry policy
            var response = await _getConfigurationRetryPolicy.ExecuteAsync(async ct =>
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, OfrepConfigurationPath))
                {
                    var httpResponse = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
                    httpResponse.EnsureSuccessStatusCode();
                    return httpResponse;
                }
            }, cancellationToken).ConfigureAwait(false);
            var configurationResponse =
                await response.Content.ReadFromJsonAsync<ConfigurationResponse>(JsonOptions, cancellationToken)
                    .ConfigureAwait(false);
            LogConfigurationSuccess();
            return configurationResponse;
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is JsonException ||
                                   ex is OperationCanceledException
                                   || ex is ArgumentNullException)
        {
            LogConfigurationError(ex.GetType().Name, ex.Message,
                $"{_httpClient.BaseAddress}{OfrepConfigurationPath}", ex);
            throw new OfrepConfigurationException(
                $"Failed to retrieve OFREP provider configuration from {_httpClient.BaseAddress}{OfrepConfigurationPath}.",
                ex);
        }
    }

    private static string MapExceptionToErrorCode(Exception ex)
    {
        if (ex is HttpRequestException)
        {
            return ErrorCodeProviderNotReady;
        }
        else if (ex is JsonException)
        {
            return ErrorCodeParsingError;
        }
        else if (ex is OperationCanceledException)
        {
            return ErrorCodeProviderNotReady;
        }
        else if (ex is ArgumentNullException)
        {
            return ErrorCodeParsingError;
        }
        else if (ex is InvalidOperationException || ex is ArgumentException || ex is OfrepConfigurationException)
        {
            return ErrorCodeGeneralError;
        }
        else
        {
            return ErrorCodeGeneralError;
        }
    }

    /// <summary>
    /// Sets the cache duration for evaluation responses. Use TimeSpan.Zero to disable expiration.
    /// </summary>
    /// <param name="duration">New cache duration. Must be non-negative and reasonable (e.g., less than or equal to 1 day).</param>
    public void SetCacheDuration(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero || duration.TotalDays > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(duration),
                "Cache duration must be non-negative and not excessively long (e.g., <= 1 day).");
        }

        LogCacheDurationChange(duration);
        _cacheDuration = duration;
    }

    public void Dispose()
    {
        Dispose(true);
        // ReSharper disable once GCSuppressFinalizeForTypeWithoutDestructor
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                if (_httpClient != null)
                {
                    _httpClient.Dispose();
                }

                if (_cache != null)
                {
                    _cache.Dispose();
                }
            }

            _disposed = true;
        }
    }
}
