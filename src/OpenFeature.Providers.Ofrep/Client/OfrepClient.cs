using System.Net;
#if NETFRAMEWORK
using System.Net.Http;
#endif
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenFeature.Constant;
using OpenFeature.Providers.Ofrep.Configuration;
using OpenFeature.Providers.Ofrep.Extensions;
using OpenFeature.Providers.Ofrep.Models;
using OpenFeature.Model;
using OpenFeature.Providers.Ofrep.Client.Constants;

namespace OpenFeature.Providers.Ofrep.Client;

/// <summary>
/// Implementation of the OFREP HTTP client.
/// </summary>
internal sealed partial class OfrepClient : IOfrepClient
{
    private readonly HttpClient _httpClient;
    private DateTimeOffset? _retryAfterDate;
    private readonly ILogger _logger;
    private bool _disposed;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

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

        this._logger = logger ?? NullLogger<OfrepClient>.Instance;
        this._httpClient = new HttpClient(handler, disposeHandler: true)
        {
            BaseAddress = new Uri(configuration.BaseUrl), Timeout = configuration.Timeout
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

        if (this._retryAfterDate.HasValue && this._retryAfterDate.Value > DateTimeOffset.UtcNow)
        {
            return new OfrepResponse<T>(flagKey, defaultValue)
            {
                ErrorCode = ErrorCodes.ProviderNotReady, Reason = Reason.Error, ErrorMessage = "Rate limit exceeded."
            };
        }

        try
        {
            var request = CreateEvaluationRequest(flagKey, context);
            var response = await this._httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            // Get Status Code
            var statusCode = response.StatusCode;

            return statusCode switch
            {
                HttpStatusCode.OK => await this
                    .ProcessOkResponseAsync(flagKey, defaultValue, response, cancellationToken)
                    .ConfigureAwait(false),
                HttpStatusCode.BadRequest => await this
                    .ProcessBadRequestResponse(flagKey, defaultValue, response, cancellationToken)
                    .ConfigureAwait(false),
                HttpStatusCode.NotFound => ProcessNotFoundResponse(flagKey, defaultValue),
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => ProcessAuthenticationErrorResponse(flagKey,
                    defaultValue),
#if NET8_0_OR_GREATER
                HttpStatusCode.TooManyRequests => ProcessTooManyRequestsResponse(flagKey, defaultValue, response),
#else
                (HttpStatusCode)429 => ProcessTooManyRequestsResponse(flagKey, defaultValue, response),
#endif
                _ => ProcessNotMappedErrorResponse(flagKey, defaultValue)
            };
        }
        catch (HttpRequestException ex)
        {
            this.LogHttpRequestFailed(flagKey, ex.Message, ex);
            return HandleEvaluationError(flagKey, ex, defaultValue);
        }
        catch (JsonException ex)
        {
            this.LogJsonParseError(flagKey, ex.Message, ex);
            return HandleEvaluationError(flagKey, ex, defaultValue);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            this.LogRequestCancelled(flagKey, ex);
            return HandleEvaluationError(flagKey, ex, defaultValue);
        }
        catch (OperationCanceledException ex)
        {
            this.LogRequestTimeout(flagKey, ex.Message, ex);
            return HandleEvaluationError(flagKey, ex, defaultValue);
        }
        catch (Exception ex)
        {
            this.LogOperationError(flagKey, ex.Message, ex);
            return HandleEvaluationError(flagKey, ex, defaultValue);
        }
    }

    private static OfrepResponse<T> ProcessNotMappedErrorResponse<T>(string key, T defaultValue)
    {
        return new OfrepResponse<T>(key, defaultValue)
        {
            ErrorCode = ErrorCodes.GeneralError,
            Reason = Reason.Error,
            ErrorMessage = "General error during flag evaluation."
        };
    }

    private OfrepResponse<T> ProcessTooManyRequestsResponse<T>(string key, T defaultValue, HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter?.Delta.HasValue == true)
        {
            this._retryAfterDate = DateTimeOffset.UtcNow.Add(retryAfter.Delta.Value);
        }
        else if (retryAfter?.Date.HasValue == true)
        {
            this._retryAfterDate = retryAfter.Date.Value;
        }
        else
        {
            this._retryAfterDate = DateTimeOffset.UtcNow.AddMinutes(1);
        }

        return new OfrepResponse<T>(key, defaultValue)
        {
            ErrorCode = ErrorCodes.ProviderNotReady, Reason = Reason.Error, ErrorMessage = "Rate limit exceeded."
        };
    }

    private static OfrepResponse<T> ProcessNotFoundResponse<T>(string key, T defaultValue)
    {
        return new OfrepResponse<T>(key, defaultValue)
        {
            ErrorCode = ErrorCodes.FlagNotFound, Reason = Reason.Error, ErrorMessage = "Flag not found."
        };
    }

    private static OfrepResponse<T> ProcessAuthenticationErrorResponse<T>(string key, T defaultValue)
    {
        return new OfrepResponse<T>(key, defaultValue)
        {
            ErrorCode = ErrorCodes.ProviderNotReady,
            Reason = Reason.Error,
            ErrorMessage = "Unauthorized access to flag evaluation."
        };
    }

    private async Task<OfrepResponse<T>> ProcessBadRequestResponse<T>(string flagKey, T defaultValue,
        HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        var evaluationResponse = await response.Content
            .ReadFromJsonAsync<OfrepResponse<T>>(JsonOptions, cancellationToken).ConfigureAwait(false);
        if (evaluationResponse == null)
        {
            this.LogNullResponse(flagKey);
            return new OfrepResponse<T>(flagKey, defaultValue)
            {
                ErrorCode = ErrorCodes.ParseError, ErrorMessage = "Received null or empty response from server."
            };
        }

        return evaluationResponse;
    }

    private async Task<OfrepResponse<T>> ProcessOkResponseAsync<T>(string flagKey, T defaultValue,
        HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        var evaluationResponse = await response.Content
            .ReadFromJsonAsync<OfrepResponse<T>>(JsonOptions, cancellationToken).ConfigureAwait(false);
        if (evaluationResponse == null)
        {
            this.LogNullResponse(flagKey);
            return new OfrepResponse<T>(flagKey, defaultValue)
            {
                ErrorCode = ErrorCodes.ParseError, ErrorMessage = "Received null or empty response from server."
            };
        }

        return evaluationResponse;
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
            }

            this._disposed = true;
        }
    }

    /// <summary>
    /// Create an HTTP request for flag evaluation
    /// </summary>
    private static HttpRequestMessage CreateEvaluationRequest(string flagKey, EvaluationContext? context)
    {
        string path = $"{OfrepPaths.Evaluate}{Uri.EscapeDataString(flagKey)}";
        var evaluationContextDict = (context ?? EvaluationContext.Empty).ToDictionary();
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(new OfrepRequest { Context = evaluationContextDict }, options: JsonOptions)
        };

        return request;
    }

    /// <summary>
    /// Helper to handle errors during flag evaluation.
    /// </summary>
    private static OfrepResponse<T> HandleEvaluationError<T>(string key, Exception ex, T defaultValue)
    {
        return new OfrepResponse<T>(key, defaultValue)
        {
            ErrorCode = ErrorCodes.GeneralError, Reason = Reason.Error, ErrorMessage = ex.Message
        };
    }

    private static HttpClientHandler CreateDefaultHandler()
    {
        return new HttpClientHandler
        {
            UseProxy = true, AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
    }

    // Define high-performance logging delegates using source generators
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Error,
        Message = "Received null response body from server for flag {FlagKey}")]
    partial void LogNullResponse(string flagKey);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Error,
        Message = "HTTP request failed for flag {FlagKey}: {Message}")]
    partial void LogHttpRequestFailed(string flagKey, string message, Exception ex);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Error,
        Message = "Failed to parse JSON response for flag {FlagKey}: {Message}")]
    partial void LogJsonParseError(string flagKey, string message, Exception ex);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Warning,
        Message = "Request cancelled for flag {FlagKey}")]
    partial void LogRequestCancelled(string flagKey, Exception ex);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Error,
        Message = "Request timed out for flag {FlagKey}: {Message}")]
    partial void LogRequestTimeout(string flagKey, string message, Exception ex);

    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Error,
        Message = "Operation error for flag {FlagKey}: {Message}")]
    partial void LogOperationError(string flagKey, string message, Exception ex);
}
