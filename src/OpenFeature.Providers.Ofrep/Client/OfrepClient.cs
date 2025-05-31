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
using OpenFeature.Providers.Ofrep.Configuration;
using OpenFeature.Providers.Ofrep.Extensions;
using OpenFeature.Providers.Ofrep.Models;
using OpenFeature.Model;
using Polly;

namespace OpenFeature.Providers.Ofrep.Client;

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
    public OfrepConfigurationException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Implementation of the OFREP HTTP client.
/// </summary>
internal sealed partial class OfrepClient : IOfrepClient
{
    // OFREP API paths
    private const string OfrepEvaluatePathPrefix = "/ofrep/v1/evaluate/flags/";
    private const string OfrepBulkEvaluatePath = "/ofrep/v1/evaluate/flags";
    private const string OfrepConfigurationPath = "/ofrep/v1/configuration";

    // Error codes
    private const string ErrorCodeProviderNotReady = "provider_not_ready";
    private const string ErrorCodeParsingError = "parsing_error";
    private const string ErrorCodeGeneralError = "general_error";

    // Factor for absolute expiration based on sliding expiration. Consider making configurable if needed.
    private const double AbsoluteExpirationFactor = 5.0;
    private readonly HttpClient _httpClient;
    private readonly MemoryCache _cache;
    private readonly ILogger _logger;
    private readonly bool _enableAbsoluteExpiration;
    private bool _disposed;
    private TimeSpan _cacheDuration;

    private static readonly JsonSerializerOptions JsonOptions = new()
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
    public OfrepClient(OfrepConfiguration configuration, ILogger? logger = null)
        : this(configuration, CreateDefaultHandler(), logger) // Use helper for default handler
    {
    }

    /// <summary>
    /// Internal constructor for testing purposes.
    /// </summary>
    /// <param name="configuration">The OFREP configuration.</param>
    /// <param name="handler">The HTTP message handler.</param>
    /// <param name="logger">The logger for the client.</param>
    internal OfrepClient(OfrepConfiguration configuration, HttpMessageHandler handler, ILogger? logger = null)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(handler);
#else
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }
#endif

        this._logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        this._getConfigurationRetryPolicy = Policy
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: 5,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1)),
                onRetry: (exception, _, retryAttempt, _) =>
                {
                    this.LogRetrying(exception.GetType().Name, retryAttempt);
                });
        this._cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = configuration.MaxCacheSize > 0 ? configuration.MaxCacheSize : null
        });
        this._cacheDuration = configuration.CacheDuration;
        this._enableAbsoluteExpiration = configuration.EnableAbsoluteExpiration;
        this._httpClient = new HttpClient(handler, disposeHandler: true)
        {
            BaseAddress = new Uri(configuration.BaseUrl), Timeout = TimeSpan.FromSeconds(5)
        };
        if (configuration.Headers != null)
        {
            foreach (var header in configuration.Headers)
            {
                this._httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        if (!string.IsNullOrEmpty(configuration.AuthorizationHeader))
        {
            this._httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", configuration.AuthorizationHeader);
        }
    }

    private static HttpClientHandler CreateDefaultHandler()
    {
        return new HttpClientHandler
        {
            UseProxy = true, AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
    }

    /// <inheritdoc/>
    public async Task<OfrepResponse<T>> EvaluateFlag<T>(string flagKey, string type, T defaultValue,
        EvaluationContext? context, CancellationToken cancellationToken = default)
    {
#if NET8_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(flagKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
#else
        if (string.IsNullOrWhiteSpace(flagKey))
        {
            throw new ArgumentException("Flag key cannot be null or whitespace", nameof(flagKey));
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Type cannot be null or whitespace", nameof(type));
        }
#endif

        var cacheKey = $"flag:{flagKey};type:{type};ctx:{(context ?? EvaluationContext.Empty).GenerateETag()}";

        // Check cache first
        if (this.TryGetCachedResponse<T>(cacheKey, out var cachedResponse) && cachedResponse != null)
        {
            this.LogCacheHit(cacheKey);
            return cachedResponse;
        }

        this.LogCacheMiss(cacheKey);

        try
        {
            var request = this.CreateEvaluationRequest(flagKey, context, cacheKey);
            var response = await this._httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            // Handle 304 Not Modified
            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                return this.HandleNotModified<T>(cacheKey, flagKey);
            }

            response.EnsureSuccessStatusCode();

            var evaluationResponse = await response.Content
                .ReadFromJsonAsync<OfrepResponse<T>>(JsonOptions, cancellationToken).ConfigureAwait(false);
            if (evaluationResponse == null)
            {
                this.LogNullResponse(flagKey);
                return new OfrepResponse<T>(defaultValue)
                {
                    ErrorCode = ErrorCodeParsingError, ErrorMessage = "Received null or empty response from server."
                };
            }

            this.CacheResponse(cacheKey, evaluationResponse, response.Headers.ETag?.Tag);
            this.LogCacheMetrics(flagKey, cacheKey, response.Headers.ETag?.Tag ?? "N/A",
                this._cacheDuration.TotalMilliseconds);

            return evaluationResponse;
        }
        catch (HttpRequestException ex)
        {
            this.LogHttpRequestFailed(flagKey, ex.Message, ex);
            return this.HandleEvaluationError(ex, cacheKey, defaultValue);
        }
        catch (JsonException ex)
        {
            this.LogJsonParseError(flagKey, ex.Message, ex);
            return this.HandleEvaluationError(ex, cacheKey, defaultValue);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            this.LogRequestCancelled(flagKey, ex);
            return this.HandleEvaluationError(ex, cacheKey, defaultValue);
        }
        catch (OperationCanceledException ex)
        {
            this.LogRequestTimeout(flagKey, ex.Message, ex);
            return this.HandleEvaluationError(ex, cacheKey, defaultValue);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            this.LogCacheOperationError(flagKey, ex.Message, ex);
            return this.HandleEvaluationError(ex, cacheKey, defaultValue);
        }
    }

    /// <inheritdoc/>
    public async Task<BulkEvaluationResponse> BulkEvaluate(EvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"bulk;ctx:{context.GenerateETag()}";

        // Check cache first
        if (this.TryGetBulkCachedResponse(cacheKey, out var cachedResponse) && cachedResponse != null)
        {
            this.LogCacheHit(cacheKey);
            return cachedResponse;
        }

        this.LogCacheMiss(cacheKey);

        try
        {
            var request = this.CreateBulkEvaluationRequest(context, cacheKey);
            var response = await this._httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            // Handle 304 Not Modified
            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                return this.HandleBulkNotModified(cacheKey);
            }

            response.EnsureSuccessStatusCode();
            var evaluationResponse = await response.Content
                .ReadFromJsonAsync<BulkEvaluationResponse>(JsonOptions, cancellationToken).ConfigureAwait(false);
            if (evaluationResponse == null)
            {
                this.LogBulkNullResponse("BulkEvaluate");
                throw new OfrepConfigurationException(
                    "Received null or empty response from server for bulk evaluation.", null);
            }

            this.CacheBulkResponse(cacheKey, evaluationResponse, response.Headers.ETag?.Tag);
            this.LogBulkCacheMetrics("BulkEvaluate", cacheKey, response.Headers.ETag?.Tag ?? "N/A",
                this._cacheDuration.TotalMilliseconds);

            return evaluationResponse;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException
                                       or ArgumentNullException)
        {
            return this.HandleBulkEvaluationError(ex, cacheKey);
        }
    }

    /// <summary>
    /// Retrieves the OFREP provider configuration.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns></returns>
    /// <exception cref="OfrepConfigurationException"></exception>
    public async Task<ConfigurationResponse?> GetConfiguration(CancellationToken cancellationToken = default)
    {
        try
        {
            // Execute the HTTP GET request within the retry policy
            var response = await this._getConfigurationRetryPolicy.ExecuteAsync(async ct =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, OfrepConfigurationPath);
                var httpResponse = await this._httpClient.SendAsync(request, ct).ConfigureAwait(false);
                httpResponse.EnsureSuccessStatusCode();
                return httpResponse;
            }, cancellationToken).ConfigureAwait(false);
            var configurationResponse =
                await response.Content.ReadFromJsonAsync<ConfigurationResponse>(JsonOptions, cancellationToken)
                    .ConfigureAwait(false);
            this.LogConfigurationSuccess();
            return configurationResponse;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException
                                       or ArgumentNullException)
        {
            this.LogConfigurationError(ex.GetType().Name, ex.Message,
                $"{this._httpClient.BaseAddress}{OfrepConfigurationPath}", ex);
            throw new OfrepConfigurationException(
                $"Failed to retrieve OFREP provider configuration from {this._httpClient.BaseAddress}{OfrepConfigurationPath}.",
                ex);
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

        this.LogCacheDurationChange(duration);
        this._cacheDuration = duration;
    }

    public void Dispose()
    {
        this.Dispose(true);
        // ReSharper disable once GCSuppressFinalizeForTypeWithoutDestructor
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!this._disposed)
        {
            if (disposing)
            {
                this._httpClient.Dispose();
                this._cache.Dispose();
            }

            this._disposed = true;
        }
    }

    /// <summary>
    /// Try to get a cached response for the given cache key
    /// </summary>
    private bool TryGetCachedResponse<T>(string cacheKey, out OfrepResponse<T>? cachedResponse)
    {
        if (this._cache.TryGetValue(cacheKey, out object? cachedResponseObject) &&
            cachedResponseObject is OfrepResponse<T> response)
        {
            cachedResponse = response;
            return true;
        }

        cachedResponse = null;
        return false;
    }

    /// <summary>
    /// Create an HTTP request for flag evaluation
    /// </summary>
    private HttpRequestMessage CreateEvaluationRequest(string flagKey, EvaluationContext? context, string cacheKey)
    {
        string path = $"{OfrepEvaluatePathPrefix}{Uri.EscapeDataString(flagKey)}";
        var evaluationContextDict = (context ?? EvaluationContext.Empty).ToDictionary();
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(new OfrepRequest { Context = evaluationContextDict }, options: JsonOptions)
        };

        // Add If-None-Match header if we have a cached ETag
        var etagCacheKey = $"etag:{cacheKey}";
        if (this._cache.TryGetValue(etagCacheKey, out object? cachedETagObject) &&
            cachedETagObject is string cachedETag && !string.IsNullOrEmpty(cachedETag))
        {
            request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(cachedETag, isWeak: true));
            this.LogSendingRequestWithETag(flagKey, cachedETag);
        }
        else
        {
            this.LogSendingRequestWithoutETag(flagKey);
        }

        return request;
    }

    /// <summary>
    /// Handle 304 Not Modified response
    /// </summary>
    private OfrepResponse<T> HandleNotModified<T>(string cacheKey, string flagKey)
    {
        if (this.TryGetCachedResponse<T>(cacheKey, out var notModifiedResponse) && notModifiedResponse != null)
        {
            this.LogNotModified(flagKey);
            return notModifiedResponse;
        }

        // If cache check fails after 304 (unlikely but possible), log and fall through
        this.LogNotModifiedNoCache(flagKey);
        throw new InvalidOperationException("Received 304 Not Modified but no cached response available");
    }

    /// <summary>
    /// Cache the response and ETag
    /// </summary>
    private void CacheResponse<T>(string cacheKey, OfrepResponse<T> response, string? responseETag)
    {
        var cacheEntryOptions = this.CreateCacheEntryOptions();
        this._cache.Set(cacheKey, response, cacheEntryOptions);

        var etagCacheKey = $"etag:{cacheKey}";
        if (responseETag != null)
        {
            this._cache.Set(etagCacheKey, responseETag, cacheEntryOptions);
        }
        else
        {
            this._cache.Remove(etagCacheKey);
        }
    }

    /// <summary>
    /// Create cache entry options with configured expiration
    /// </summary>
    private MemoryCacheEntryOptions CreateCacheEntryOptions()
    {
        var options = new MemoryCacheEntryOptions().SetSize(1);

        if (this._cacheDuration > TimeSpan.Zero)
        {
            options.SetSlidingExpiration(this._cacheDuration);

            if (this._enableAbsoluteExpiration)
            {
                var absoluteExpiration =
                    TimeSpan.FromTicks((long)(this._cacheDuration.Ticks * AbsoluteExpirationFactor));
                options.SetAbsoluteExpiration(absoluteExpiration);
            }
        }

        return options;
    }

    /// <summary>
    /// Try to get a cached bulk evaluation response for the given cache key
    /// </summary>
    private bool TryGetBulkCachedResponse(string cacheKey, out BulkEvaluationResponse? cachedResponse)
    {
        if (this._cache.TryGetValue(cacheKey, out object? cachedResponseObject) &&
            cachedResponseObject is ValueTuple<BulkEvaluationResponse, string> tuple)
        {
            cachedResponse = tuple.Item1;
            return true;
        }

        cachedResponse = null;
        return false;
    }

    /// <summary>
    /// Create an HTTP request for bulk evaluation
    /// </summary>
    private HttpRequestMessage CreateBulkEvaluationRequest(EvaluationContext context, string cacheKey)
    {
        var evaluationContextDict = context.ToDictionary();
        var request = new HttpRequestMessage(HttpMethod.Post, OfrepBulkEvaluatePath)
        {
            Content = JsonContent.Create(new OfrepRequest { Context = evaluationContextDict }, options: JsonOptions)
        };

        // Add If-None-Match header if we have a cached ETag
        var etagCacheKey = $"etag:{cacheKey}";
        if (this._cache.TryGetValue(etagCacheKey, out object? cachedETagObject) &&
            cachedETagObject is string cachedETag && !string.IsNullOrEmpty(cachedETag))
        {
            request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(cachedETag, isWeak: true));
            this.LogBulkRequestWithETag("BulkEvaluate", cachedETag, cacheKey);
        }
        else
        {
            this.LogBulkRequestWithoutETag("BulkEvaluate", cacheKey);
        }

        return request;
    }

    /// <summary>
    /// Handle 304 Not Modified response for bulk evaluation
    /// </summary>
    private BulkEvaluationResponse HandleBulkNotModified(string cacheKey)
    {
        if (this.TryGetBulkCachedResponse(cacheKey, out var notModifiedResponse) && notModifiedResponse != null)
        {
            this.LogBulkNotModified("BulkEvaluate", cacheKey);
            return notModifiedResponse;
        }

        // If cache check fails after 304 (unlikely but possible), log and throw
        this.LogBulkNotModifiedNoCache("BulkEvaluate", cacheKey);
        throw new InvalidOperationException("Received 304 Not Modified but no cached response available");
    }

    /// <summary>
    /// Cache the bulk evaluation response and ETag
    /// </summary>
    private void CacheBulkResponse(string cacheKey, BulkEvaluationResponse response, string? responseETag)
    {
        var cacheEntryOptions = this.CreateCacheEntryOptions();
        this._cache.Set(cacheKey, (response, responseETag ?? string.Empty), cacheEntryOptions);

        var etagCacheKey = $"etag:{cacheKey}";
        if (responseETag != null)
        {
            this._cache.Set(etagCacheKey, responseETag, cacheEntryOptions);
        }
        else
        {
            this._cache.Remove(etagCacheKey);
        }
    }

    /// <summary>
    /// Handle errors during bulk evaluation, attempting to return stale cache if available
    /// </summary>
    private BulkEvaluationResponse HandleBulkEvaluationError(Exception ex, string cacheKey)
    {
        if (this.TryGetBulkCachedResponse(cacheKey, out var staleResponse) && staleResponse != null)
        {
            this.LogBulkStaleCacheReturn(cacheKey, ex.GetType().Name, "BulkEvaluate", ex);
            return staleResponse;
        }

        throw new OfrepConfigurationException(
            $"Failed during OFREP bulk evaluation request to {OfrepBulkEvaluatePath}.", ex);
    }

    /// <summary>
    /// Helper to handle errors during flag evaluation, attempting to return stale cache if available.
    /// </summary>
    private OfrepResponse<T> HandleEvaluationError<T>(Exception ex, string cacheKey, T defaultValue)
    {
        if (this._cache.TryGetValue(cacheKey, out object? cachedResponseObject) &&
            cachedResponseObject is OfrepResponse<T> staleResponse)
        {
            this.LogStaleCacheReturn(cacheKey, ex.GetType().Name);
            return staleResponse;
        }

        return new OfrepResponse<T>(defaultValue)
        {
            ErrorCode = MapExceptionToErrorCode(ex), Reason = "ERROR", ErrorMessage = ex.Message
        };
    }

    private static string MapExceptionToErrorCode(Exception ex)
    {
        return ex switch
        {
            HttpRequestException => ErrorCodeProviderNotReady,
            JsonException => ErrorCodeParsingError,
            OperationCanceledException => ErrorCodeProviderNotReady,
            ArgumentNullException => ErrorCodeParsingError,
            _ => ErrorCodeGeneralError
        };
    }


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
    partial void LogSendingRequestWithETag(string flagKey, string? eTag);

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
        Message =
            "Received 304 Not Modified for bulk evaluation {Operation} but no data in cache entry. Key: {CacheKey}, proceeding as failure")]
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
        Message =
            "Returning stale cache data for bulk key {CacheKey} due to error: {ErrorType}, Operation: {Operation}")]
    partial void LogBulkStaleCacheReturn(string cacheKey, string errorType, string operation, Exception ex);

    [LoggerMessage(
        EventId = 1023,
        Level = LogLevel.Error,
        Message =
            "Failed to get configuration after retries (if applicable). Error: {ErrorType}, Message: {Message}, Endpoint: {Endpoint}")]
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
}
