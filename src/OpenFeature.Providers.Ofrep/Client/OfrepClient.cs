using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
#if NETFRAMEWORK
using System.Net.Http;
#endif
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenFeature.Constant;
using OpenFeature.Providers.Ofrep.Configuration;
using OpenFeature.Providers.Ofrep.Extensions;
using OpenFeature.Providers.Ofrep.Models;
using OpenFeature.Providers.Ofrep.Serialization;
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
    private readonly TimeProvider _timeProvider;
    private bool _disposed;
    private static readonly JsonSerializerOptions LegacyJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Creates a new instance of <see cref="OfrepClient"/>.
    /// </summary>
    /// <param name="configuration">The OFREP configuration.</param>
    /// <param name="logger">The logger for the client.</param>
    /// <param name="timeProvider">The time provider for time-related operations. Defaults to TimeProvider.System.</param>
    public OfrepClient(OfrepOptions configuration, ILogger? logger = null, TimeProvider? timeProvider = null)
        : this(configuration, CreateDefaultHandler(), logger, timeProvider) // Use helper for default handler
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="OfrepClient"/> using a provided <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="httpClient">The HttpClient to use for requests. Caller may provide one from IHttpClientFactory.</param>
    /// <param name="logger">The logger for the client.</param>
    /// <param name="timeProvider">The time provider for time-related operations. Defaults to TimeProvider.System.</param>
    internal OfrepClient(HttpClient httpClient, ILogger? logger = null, TimeProvider? timeProvider = null)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(httpClient);
#else
        if (httpClient == null)
        {
            throw new ArgumentNullException(nameof(httpClient));
        }
#endif

        this._logger = logger ?? NullLogger<OfrepClient>.Instance;
        this._httpClient = httpClient;
        this._timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Internal constructor for testing purposes.
    /// </summary>
    /// <param name="configuration">The OFREP configuration.</param>
    /// <param name="handler">The HTTP message handler.</param>
    /// <param name="logger">The logger for the client.</param>
    /// <param name="timeProvider">The time provider for time-related operations. Defaults to TimeProvider.System.</param>
    internal OfrepClient(OfrepOptions configuration, HttpMessageHandler handler, ILogger? logger = null, TimeProvider? timeProvider = null)
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
            BaseAddress = new Uri(configuration.BaseUrl),
            Timeout = configuration.Timeout
        };

        foreach (var header in configuration.Headers)
        {
            this._httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
        }

        this._timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    public Task<OfrepResponse<bool>> EvaluateBooleanFlag(string flagKey, bool defaultValue,
        EvaluationContext? context, CancellationToken cancellationToken = default) =>
        this.EvaluateFlagInternal(flagKey, defaultValue, context, cancellationToken);

    /// <inheritdoc/>
    public Task<OfrepResponse<string>> EvaluateStringFlag(string flagKey, string defaultValue,
        EvaluationContext? context, CancellationToken cancellationToken = default) =>
        this.EvaluateFlagInternal(flagKey, defaultValue, context, cancellationToken);

    /// <inheritdoc/>
    public Task<OfrepResponse<int>> EvaluateIntegerFlag(string flagKey, int defaultValue,
        EvaluationContext? context, CancellationToken cancellationToken = default) =>
        this.EvaluateFlagInternal(flagKey, defaultValue, context, cancellationToken);

    /// <inheritdoc/>
    public Task<OfrepResponse<double>> EvaluateDoubleFlag(string flagKey, double defaultValue,
        EvaluationContext? context, CancellationToken cancellationToken = default) =>
        this.EvaluateFlagInternal(flagKey, defaultValue, context, cancellationToken);

    /// <inheritdoc/>
    public Task<OfrepResponse<JsonElement?>> EvaluateStructureFlag(string flagKey, JsonElement? defaultValue,
        EvaluationContext? context, CancellationToken cancellationToken = default) =>
        this.EvaluateFlagInternal(flagKey, defaultValue, context, cancellationToken);

    /// <summary>
    /// Evaluates a flag value using the OFREP API. This generic method is provided for backward compatibility.
    /// For Native AOT scenarios, use the typed methods (EvaluateBooleanFlag, EvaluateStringFlag, etc.) instead.
    /// </summary>
    /// <typeparam name="T">The type of the flag value.</typeparam>
    /// <param name="flagKey">The key of the flag to evaluate.</param>
    /// <param name="defaultValue">The default value to return if evaluation fails.</param>
    /// <param name="context">The evaluation context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The evaluated flag response.</returns>
    /// <remarks>
    /// This method delegates to the AOT-safe typed implementation for OFREP-supported value types.
    /// Other generic types use the legacy reflection-based JSON path and are not AOT-safe.
    /// </remarks>
    [RequiresDynamicCode(
        "Generic flag evaluation for arbitrary types requires runtime JSON serialization metadata. Use the typed evaluation methods for Native AOT scenarios.")]
    [RequiresUnreferencedCode(
        "Generic flag evaluation for arbitrary types may require trimmed type metadata. Use the typed evaluation methods for Native AOT scenarios.")]
    public Task<OfrepResponse<T>> EvaluateFlag<T>(string flagKey, T defaultValue,
        EvaluationContext? context, CancellationToken cancellationToken = default) =>
        IsAotSafeEvaluationType<T>()
            ? this.EvaluateFlagInternal(flagKey, defaultValue, context, cancellationToken)
            : this.EvaluateFlagLegacyAsync(flagKey, defaultValue, context, cancellationToken);

    internal async Task<OfrepResponse<T>> EvaluateFlagInternal<T>(string flagKey, T defaultValue,
        EvaluationContext? context, CancellationToken cancellationToken = default)
    {
#if NET8_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(flagKey);
#else
        if (string.IsNullOrWhiteSpace(flagKey))
        {
            throw new ArgumentException("Flag key cannot be null or whitespace", nameof(flagKey));
        }
#endif

        if (this._retryAfterDate.HasValue && this._retryAfterDate.Value > this._timeProvider.GetUtcNow())
        {
            return new OfrepResponse<T>(flagKey, defaultValue)
            {
                ErrorCode = ErrorCodes.GeneralError,
                Reason = Reason.Error,
                ErrorMessage = "Rate limit exceeded."
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
                HttpStatusCode.TooManyRequests => this.ProcessTooManyRequestsResponse(flagKey, defaultValue, response),
#else
                (HttpStatusCode)429 => this.ProcessTooManyRequestsResponse(flagKey, defaultValue, response),
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
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
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

    [RequiresDynamicCode(
        "Generic flag evaluation for arbitrary types requires runtime JSON serialization metadata. Use the typed evaluation methods for Native AOT scenarios.")]
    [RequiresUnreferencedCode(
        "Generic flag evaluation for arbitrary types may require trimmed type metadata. Use the typed evaluation methods for Native AOT scenarios.")]
    private async Task<OfrepResponse<T>> EvaluateFlagLegacyAsync<T>(string flagKey, T defaultValue,
        EvaluationContext? context, CancellationToken cancellationToken = default)
    {
#if NET8_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(flagKey);
#else
        if (string.IsNullOrWhiteSpace(flagKey))
        {
            throw new ArgumentException("Flag key cannot be null or whitespace", nameof(flagKey));
        }
#endif

        if (this._retryAfterDate.HasValue && this._retryAfterDate.Value > this._timeProvider.GetUtcNow())
        {
            return new OfrepResponse<T>(flagKey, defaultValue)
            {
                ErrorCode = ErrorCodes.GeneralError,
                Reason = Reason.Error,
                ErrorMessage = "Rate limit exceeded."
            };
        }

        try
        {
            var request = CreateEvaluationRequest(flagKey, context);
            var response = await this._httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            return response.StatusCode switch
            {
                HttpStatusCode.OK => await this.ProcessOkResponseLegacyAsync(flagKey, defaultValue, response, cancellationToken)
                    .ConfigureAwait(false),
                HttpStatusCode.BadRequest => await this.ProcessBadRequestResponseLegacyAsync(flagKey, defaultValue, response, cancellationToken)
                    .ConfigureAwait(false),
                HttpStatusCode.NotFound => ProcessNotFoundResponse(flagKey, defaultValue),
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => ProcessAuthenticationErrorResponse(flagKey,
                    defaultValue),
#if NET8_0_OR_GREATER
                HttpStatusCode.TooManyRequests => this.ProcessTooManyRequestsResponse(flagKey, defaultValue, response),
#else
                (HttpStatusCode)429 => this.ProcessTooManyRequestsResponse(flagKey, defaultValue, response),
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
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
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
            this._retryAfterDate = this._timeProvider.GetUtcNow().Add(retryAfter.Delta.Value);
        }
        else if (retryAfter?.Date.HasValue == true)
        {
            this._retryAfterDate = retryAfter.Date.Value;
        }
        else
        {
            this._retryAfterDate = this._timeProvider.GetUtcNow().AddMinutes(1);
        }

        return new OfrepResponse<T>(key, defaultValue)
        {
            ErrorCode = ErrorCodes.GeneralError,
            Reason = Reason.Error,
            ErrorMessage = "Rate limit exceeded."
        };
    }

    private static OfrepResponse<T> ProcessNotFoundResponse<T>(string key, T defaultValue)
    {
        return new OfrepResponse<T>(key, defaultValue)
        {
            ErrorCode = ErrorCodes.FlagNotFound,
            Reason = Reason.Error,
            ErrorMessage = "Flag not found."
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
        var rawResponse = await ReadRawResponseAsync(response, cancellationToken).ConfigureAwait(false);
        if (rawResponse == null)
        {
            this.LogNullResponse(flagKey);
            return new OfrepResponse<T>(flagKey, defaultValue)
            {
                ErrorCode = ErrorCodes.ParseError,
                ErrorMessage = "Received null or empty response from server."
            };
        }

        T resolvedValue = defaultValue;
        var hasValue = rawResponse.Value.ValueKind != JsonValueKind.Undefined &&
                       rawResponse.Value.ValueKind != JsonValueKind.Null;

        if (hasValue)
        {
            try
            {
                resolvedValue = DeserializeResponseValue(rawResponse.Value, defaultValue);
            }
            catch (JsonException ex)
            {
                this.LogJsonParseError(flagKey, ex.Message, ex);
            }
        }

        return new OfrepResponse<T>(rawResponse.Key ?? flagKey, resolvedValue)
        {
            ErrorCode = rawResponse.ErrorCode,
            ErrorMessage = rawResponse.ErrorMessage,
            Reason = rawResponse.Reason,
            Variant = rawResponse.Variant,
            Metadata = rawResponse.Metadata
        };
    }

    private async Task<OfrepResponse<T>> ProcessOkResponseAsync<T>(string flagKey, T defaultValue,
        HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        var rawResponse = await ReadRawResponseAsync(response, cancellationToken).ConfigureAwait(false);
        if (rawResponse == null)
        {
            this.LogNullResponse(flagKey);
            return new OfrepResponse<T>(flagKey, defaultValue)
            {
                ErrorCode = ErrorCodes.ParseError,
                ErrorMessage = "Received null or empty response from server."
            };
        }

        var hasValue = rawResponse.Value.ValueKind != JsonValueKind.Undefined &&
                       rawResponse.Value.ValueKind != JsonValueKind.Null;

        T resolvedValue;
        if (hasValue)
        {
            try
            {
                resolvedValue = DeserializeResponseValue(rawResponse.Value, defaultValue);
            }
            catch (JsonException ex)
            {
                this.LogJsonParseError(flagKey, ex.Message, ex);
                return HandleEvaluationError(flagKey, ex, defaultValue);
            }
        }
        else
        {
            resolvedValue = defaultValue;
        }

        var evaluationResponse = new OfrepResponse<T>(rawResponse.Key ?? flagKey, resolvedValue)
        {
            ErrorCode = rawResponse.ErrorCode,
            ErrorMessage = rawResponse.ErrorMessage,
            Reason = rawResponse.Reason,
            Variant = rawResponse.Variant,
            Metadata = rawResponse.Metadata
        };

        if (!hasValue)
        {
            evaluationResponse.Reason ??= Reason.Default;
        }

        return evaluationResponse;
    }

    [RequiresDynamicCode(
        "Generic flag evaluation for arbitrary types requires runtime JSON serialization metadata. Use the typed evaluation methods for Native AOT scenarios.")]
    [RequiresUnreferencedCode(
        "Generic flag evaluation for arbitrary types may require trimmed type metadata. Use the typed evaluation methods for Native AOT scenarios.")]
    private async Task<OfrepResponse<T>> ProcessBadRequestResponseLegacyAsync<T>(string flagKey, T defaultValue,
        HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        var rawResponse = await ReadRawResponseAsync(response, cancellationToken).ConfigureAwait(false);
        if (rawResponse == null)
        {
            this.LogNullResponse(flagKey);
            return new OfrepResponse<T>(flagKey, defaultValue)
            {
                ErrorCode = ErrorCodes.ParseError,
                ErrorMessage = "Received null or empty response from server."
            };
        }

        T resolvedValue = defaultValue;
        var hasValue = rawResponse.Value.ValueKind != JsonValueKind.Undefined &&
                       rawResponse.Value.ValueKind != JsonValueKind.Null;

        if (hasValue)
        {
            resolvedValue = DeserializeLegacyResponseValue(rawResponse.Value, defaultValue);
        }

        return new OfrepResponse<T>(rawResponse.Key ?? flagKey, resolvedValue)
        {
            ErrorCode = rawResponse.ErrorCode,
            ErrorMessage = rawResponse.ErrorMessage,
            Reason = rawResponse.Reason,
            Variant = rawResponse.Variant,
            Metadata = rawResponse.Metadata
        };
    }

    [RequiresDynamicCode(
        "Generic flag evaluation for arbitrary types requires runtime JSON serialization metadata. Use the typed evaluation methods for Native AOT scenarios.")]
    [RequiresUnreferencedCode(
        "Generic flag evaluation for arbitrary types may require trimmed type metadata. Use the typed evaluation methods for Native AOT scenarios.")]
    private async Task<OfrepResponse<T>> ProcessOkResponseLegacyAsync<T>(string flagKey, T defaultValue,
        HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        var rawResponse = await ReadRawResponseAsync(response, cancellationToken).ConfigureAwait(false);
        if (rawResponse == null)
        {
            this.LogNullResponse(flagKey);
            return new OfrepResponse<T>(flagKey, defaultValue)
            {
                ErrorCode = ErrorCodes.ParseError,
                ErrorMessage = "Received null or empty response from server."
            };
        }

        var hasValue = rawResponse.Value.ValueKind != JsonValueKind.Undefined &&
                       rawResponse.Value.ValueKind != JsonValueKind.Null;

        T resolvedValue;
        if (hasValue)
        {
            try
            {
                resolvedValue = DeserializeLegacyResponseValue(rawResponse.Value, defaultValue);
            }
            catch (JsonException ex)
            {
                this.LogJsonParseError(flagKey, ex.Message, ex);
                return HandleEvaluationError(flagKey, ex, defaultValue);
            }
        }
        else
        {
            resolvedValue = defaultValue;
        }

        var evaluationResponse = new OfrepResponse<T>(rawResponse.Key ?? flagKey, resolvedValue)
        {
            ErrorCode = rawResponse.ErrorCode,
            ErrorMessage = rawResponse.ErrorMessage,
            Reason = rawResponse.Reason,
            Variant = rawResponse.Variant,
            Metadata = rawResponse.Metadata
        };

        if (!hasValue)
        {
            evaluationResponse.Reason ??= Reason.Default;
        }

        return evaluationResponse;
    }

    private static async Task<OfrepResponse<JsonElement>?> ReadRawResponseAsync(HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return await JsonSerializer.DeserializeAsync(responseStream,
                OfrepJsonSerializerContext.Default.OfrepResponseJsonElement, cancellationToken)
            .ConfigureAwait(false);
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
        // Ensure proper URL encoding by explicitly encoding the flag key
        var encodedFlagKey = Uri.EscapeDataString(flagKey);
        string path = $"{OfrepPaths.Evaluate}{encodedFlagKey}";

        var evaluationContextDict = (context ?? EvaluationContext.Empty).ToDictionary();

        var requestBody = JsonSerializer.Serialize(new OfrepRequest { Context = evaluationContextDict },
            OfrepJsonSerializerContext.Default.OfrepRequest);

        var request = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(path, UriKind.Relative),
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };

        return request;
    }

    private static T DeserializeResponseValue<T>(JsonElement valueElement, T defaultValue)
    {
        if (typeof(T) == typeof(bool))
        {
            return (T)(object)valueElement.GetBoolean();
        }

        if (typeof(T) == typeof(string))
        {
            var stringValue = valueElement.GetString();
            return stringValue is null ? defaultValue : (T)(object)stringValue;
        }

        if (typeof(T) == typeof(int))
        {
            return (T)(object)valueElement.GetInt32();
        }

        if (typeof(T) == typeof(double))
        {
            return (T)(object)valueElement.GetDouble();
        }

        if (typeof(T) == typeof(JsonElement))
        {
            return (T)(object)valueElement;
        }

        if (typeof(T) == typeof(JsonElement?))
        {
            JsonElement? nullableElement = valueElement;
            return (T)(object)nullableElement;
        }

        return defaultValue;
    }

    [RequiresDynamicCode(
        "Generic flag evaluation for arbitrary types requires runtime JSON serialization metadata. Use the typed evaluation methods for Native AOT scenarios.")]
    [RequiresUnreferencedCode(
        "Generic flag evaluation for arbitrary types may require trimmed type metadata. Use the typed evaluation methods for Native AOT scenarios.")]
    private static T DeserializeLegacyResponseValue<T>(JsonElement valueElement, T defaultValue)
    {
        return JsonSerializer.Deserialize<T>(valueElement.GetRawText(), LegacyJsonOptions) ?? defaultValue;
    }

    private static bool IsAotSafeEvaluationType<T>()
    {
        return typeof(T) == typeof(bool) ||
               typeof(T) == typeof(string) ||
               typeof(T) == typeof(int) ||
               typeof(T) == typeof(double) ||
               typeof(T) == typeof(JsonElement) ||
               typeof(T) == typeof(JsonElement?);
    }

    /// <summary>
    /// Helper to handle errors during flag evaluation.
    /// </summary>
    private static OfrepResponse<T> HandleEvaluationError<T>(string key, Exception ex, T defaultValue)
    {
        return new OfrepResponse<T>(key, defaultValue)
        {
            ErrorCode = ErrorCodes.GeneralError,
            Reason = Reason.Error,
            ErrorMessage = ex.Message
        };
    }

    private static HttpClientHandler CreateDefaultHandler()
    {
        return new HttpClientHandler
        {
            UseProxy = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
    }

    // Define high-performance logging delegates using source generators
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "Received null response body from server for flag {FlagKey}")]
    partial void LogNullResponse(string flagKey);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "HTTP request failed for flag {FlagKey}: {Message}")]
    partial void LogHttpRequestFailed(string flagKey, string message, Exception ex);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "Failed to parse JSON response for flag {FlagKey}: {Message}")]
    partial void LogJsonParseError(string flagKey, string message, Exception ex);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "Request cancelled for flag {FlagKey}")]
    partial void LogRequestCancelled(string flagKey, Exception ex);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Error,
        Message = "Request timed out for flag {FlagKey}: {Message}")]
    partial void LogRequestTimeout(string flagKey, string message, Exception ex);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Error,
        Message = "Operation error for flag {FlagKey}: {Message}")]
    partial void LogOperationError(string flagKey, string message, Exception ex);
}
