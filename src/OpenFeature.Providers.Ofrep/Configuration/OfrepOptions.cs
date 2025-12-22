using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace OpenFeature.Providers.Ofrep.Configuration;

/// <summary>
/// Configuration options for the OFREP provider.
/// </summary>
public partial class OfrepOptions
{
    /// <summary>
    /// Environment variable name for the OFREP endpoint URL.
    /// </summary>
    public const string EnvVarEndpoint = "OFREP_ENDPOINT";

    /// <summary>
    /// Environment variable name for the OFREP headers.
    /// Format: \"Key1=Value1,Key2=Value2\". Supports URL-encoded values. Commas are always header separators.
    /// </summary>
    public const string EnvVarHeaders = "OFREP_HEADERS";

    /// <summary>
    /// Environment variable name for the OFREP timeout in milliseconds.
    /// </summary>
    public const string EnvVarTimeout = "OFREP_TIMEOUT_MS";

    /// <summary>
    /// Gets or sets the base URL for the OFREP API.
    /// </summary>
    public string BaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the timeout for HTTP requests. Default is 10 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = DefaultTimeout;
    internal static TimeSpan DefaultTimeout => TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets additional HTTP headers to include in requests.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="OfrepOptions"/> class with the specified base URL.
    /// </summary>
    /// <param name="baseUrl">The base URL for the OFREP (OpenFeature Remote Evaluation Protocol) endpoint. Must be a valid absolute URI.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="baseUrl"/> is null, empty, or not a valid absolute URI.</exception>
    public OfrepOptions(string baseUrl)
    {
        if (string.IsNullOrEmpty(baseUrl))
        {
            throw new ArgumentException("BaseUrl is required", nameof(baseUrl));
        }


        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
        {
            throw new ArgumentException("BaseUrl must be a valid absolute URI", nameof(baseUrl));
        }

        this.BaseUrl = baseUrl;
    }

    /// <summary>
    /// Creates an <see cref="OfrepOptions"/> instance from environment variables.
    /// </summary>
    /// <param name="logger">Optional logger for warnings about malformed values. Defaults to NullLogger.</param>
    /// <returns>A new <see cref="OfrepOptions"/> instance configured from environment variables.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the OFREP_ENDPOINT environment variable is not set, empty, or not a valid absolute URI.
    /// </exception>
    /// <remarks>
    /// Reads the following environment variables:
    /// <list type="bullet">
    /// <item><description>OFREP_ENDPOINT (required): The OFREP server endpoint URL.</description></item>
    /// <item><description>OFREP_HEADERS (optional): HTTP headers in format "Key1=Value1,Key2=Value2". Values may be URL-encoded to include special characters.</description></item>
    /// <item><description>OFREP_TIMEOUT_MS (optional): Request timeout in milliseconds. Defaults to 10000 (10 seconds).</description></item>
    /// </list>
    /// </remarks>
    public static OfrepOptions FromEnvironment(ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;

        var endpoint = GetEndpointValue();

        var options = new OfrepOptions(endpoint);

        // Parse timeout
        var timeoutStr = Environment.GetEnvironmentVariable(EnvVarTimeout);
        options.Timeout = GetTimeout(timeoutStr, logger);

        // Parse headers
        var headersStr = Environment.GetEnvironmentVariable(EnvVarHeaders);
        if (!string.IsNullOrWhiteSpace(headersStr))
        {
            options.Headers = ParseHeaders(headersStr, logger);
        }

        return options;
    }

    /// <summary>
    /// Creates an <see cref="OfrepOptions"/> instance from IConfiguration with fallback to environment variables.
    /// </summary>
    /// <param name="configuration">The configuration to read from. If null, falls back to environment variables only.</param>
    /// <param name="logger">Optional logger for warnings about malformed values. Defaults to NullLogger.</param>
    /// <returns>A new <see cref="OfrepOptions"/> instance configured from IConfiguration or environment variables.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when neither IConfiguration nor environment variables provide a valid OFREP_ENDPOINT value.
    /// </exception>
    /// <remarks>
    /// Reads the following configuration keys (with environment variable fallback):
    /// <list type="bullet">
    /// <item><description>OFREP_ENDPOINT (required): The OFREP server endpoint URL.</description></item>
    /// <item><description>OFREP_HEADERS (optional): HTTP headers in format "Key1=Value1,Key2=Value2". Values may be URL-encoded to include special characters.</description></item>
    /// <item><description>OFREP_TIMEOUT_MS (optional): Request timeout in milliseconds. Defaults to 10000 (10 seconds).</description></item>
    /// </list>
    /// When using IConfiguration, ensure AddEnvironmentVariables() is called in your configuration setup to enable environment variable support.
    /// </remarks>
    public static OfrepOptions FromConfiguration(IConfiguration? configuration, ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;

        var endpoint = GetEndpointValue(configuration);

        var options = new OfrepOptions(endpoint);

        // Parse timeout
        var timeoutStr = GetConfigValue(configuration, EnvVarTimeout);
        options.Timeout = GetTimeout(timeoutStr, logger);

        // Parse headers
        var headersStr = GetConfigValue(configuration, EnvVarHeaders);
        if (!string.IsNullOrWhiteSpace(headersStr))
        {
            options.Headers = ParseHeaders(headersStr, logger);
        }

        return options;
    }

    /// <summary>
    /// Gets a configuration value by key, falling back to environment variable if IConfiguration is not available or doesn't contain the key.
    /// </summary>
    internal static string? GetConfigValue(IConfiguration? configuration, string key)
    {
        // Try IConfiguration first
        var value = configuration?[key];
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        // Fall back to direct environment variable access
        return Environment.GetEnvironmentVariable(key);
    }

    /// <summary>
    /// Gets a required configuration value, throwing if not present. Returns a non-null string.
    /// This is a shim for .NET Framework where nullable flow analysis doesn't recognize IsNullOrWhiteSpace guards.
    /// </summary>
    private static string GetEndpointValue(IConfiguration? configuration = null)
    {
        var value = configuration != null ? GetConfigValue(configuration, EnvVarEndpoint) : Environment.GetEnvironmentVariable(EnvVarEndpoint);
        value ??= string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Configuration key '{EnvVarEndpoint}' or environment variable {EnvVarEndpoint} is required but was not set or is empty.", EnvVarEndpoint);
        }
        return value;
    }

    /// <summary>
    /// Parses a timeout string in milliseconds, returning a TimeSpan. Logs warnings for invalid values.
    /// </summary>
    /// <param name="timeoutStr">The timeout string to parse.</param>
    /// <param name="logger">The logger to use for warnings.</param>
    /// <returns>The parsed TimeSpan, or the default timeout if parsing fails.</returns>
    private static TimeSpan GetTimeout(string? timeoutStr, ILogger logger)
    {
        // Handle null by treating as empty (this fixes nullable flow analysis issues in .NET Framework)
        var timeoutStringNotNull = timeoutStr ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(timeoutStringNotNull))
        {
            if (int.TryParse(timeoutStringNotNull, out var timeoutMs) && timeoutMs > 0)
            {
                return TimeSpan.FromMilliseconds(timeoutMs);
            }
            else
            {
                LogInvalidTimeout(logger, timeoutStringNotNull);
            }
        }

        return DefaultTimeout;
    }

    /// <summary>
    /// Parses a header string in the format "Key1=Value1,Key2=Value2" with URL-encoding support.
    /// Values may be URL-encoded to include special characters (e.g., use %3D for equals).
    /// Note: Commas are always treated as separators and cannot be escaped or encoded into header values.
    /// </summary>
    /// <param name="headersString">The headers string to parse. Can be null or empty.</param>
    /// <param name="logger">Optional logger for warnings about malformed entries.</param>
    /// <returns>A dictionary of parsed headers.</returns>
    internal static Dictionary<string, string> ParseHeaders(string? headersString, ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        var headers = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(headersString))
        {
            return headers;
        }

        // URL-decode the entire string to support encoded special characters
        var decoded = Uri.UnescapeDataString(headersString);

        foreach (var pair in decoded.Split(','))
        {
            if (string.IsNullOrWhiteSpace(pair))
            {
                continue;
            }

            var equalsIndex = pair.IndexOf('=');
            if (equalsIndex <= 0)
            {
                LogMalformedHeaderEntry(logger, pair, EnvVarHeaders);
                continue;
            }

            var key = pair.Substring(0, equalsIndex).Trim();
            var value = pair.Substring(equalsIndex + 1).Trim();

            if (string.IsNullOrEmpty(key))
            {
                LogEmptyHeaderKey(logger, pair, EnvVarHeaders);
                continue;
            }

            headers[key] = value;
        }

        return headers;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid value '{TimeoutValue}'. Using default timeout of 10 seconds.")]
    private static partial void LogInvalidTimeout(ILogger logger, string timeoutValue);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Malformed header entry '{Entry}' in {EnvVar}. Expected format: Key=Value. Skipping.")]
    private static partial void LogMalformedHeaderEntry(ILogger logger, string entry, string envVar);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Empty header key in entry '{Entry}' in {EnvVar}. Skipping.")]
    private static partial void LogEmptyHeaderKey(ILogger logger, string entry, string envVar);
}
