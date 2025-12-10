using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace OpenFeature.Providers.Ofrep.Configuration;

/// <summary>
/// Configuration options for the OFREP provider.
/// </summary>
public class OfrepOptions
{
    /// <summary>
    /// Environment variable name for the OFREP endpoint URL.
    /// </summary>
    public const string EnvVarEndpoint = "OFREP_ENDPOINT";

    /// <summary>
    /// Environment variable name for the OFREP headers.
    /// Format: "Key1=Value1,Key2=Value2". Use backslash to escape special characters: \\ for \, \, for comma, \= for equals.
    /// </summary>
    public const string EnvVarHeaders = "OFREP_HEADERS";

    /// <summary>
    /// Environment variable name for the OFREP timeout in milliseconds.
    /// </summary>
    public const string EnvVarTimeout = "OFREP_TIMEOUT";

    /// <summary>
    /// Gets or sets the base URL for the OFREP API.
    /// </summary>
    public string BaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the timeout for HTTP requests. Default is 10 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

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
    /// <item><description>OFREP_HEADERS (optional): HTTP headers in format "Key1=Value1,Key2=Value2". Supports escape sequences: \\ for backslash, \, for comma, \= for equals.</description></item>
    /// <item><description>OFREP_TIMEOUT (optional): Request timeout in milliseconds. Defaults to 10000 (10 seconds).</description></item>
    /// </list>
    /// </remarks>
    public static OfrepOptions FromEnvironment(ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;

        var endpoint = Environment.GetEnvironmentVariable(EnvVarEndpoint);
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException(
                $"Environment variable {EnvVarEndpoint} is required but was not set or is empty.",
                EnvVarEndpoint);
        }

        var options = new OfrepOptions(endpoint!);

        // Parse timeout
        var timeoutStr = Environment.GetEnvironmentVariable(EnvVarTimeout);
        if (!string.IsNullOrWhiteSpace(timeoutStr))
        {
            if (int.TryParse(timeoutStr, out var timeoutMs) && timeoutMs > 0)
            {
                options.Timeout = TimeSpan.FromMilliseconds(timeoutMs);
            }
            else
            {
                logger.Log(
                    LogLevel.Warning,
                    "Invalid value '{TimeoutValue}' for environment variable {EnvVar}. Using default timeout of 10 seconds.",
                    timeoutStr,
                    EnvVarTimeout);
            }
        }

        // Parse headers
        var headersStr = Environment.GetEnvironmentVariable(EnvVarHeaders);
        if (!string.IsNullOrWhiteSpace(headersStr))
        {
            options.Headers = ParseHeaders(headersStr!, logger);
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
    /// <item><description>OFREP_HEADERS (optional): HTTP headers in format "Key1=Value1,Key2=Value2". Supports escape sequences: \\ for backslash, \, for comma, \= for equals.</description></item>
    /// <item><description>OFREP_TIMEOUT (optional): Request timeout in milliseconds. Defaults to 10000 (10 seconds).</description></item>
    /// </list>
    /// When using IConfiguration, ensure AddEnvironmentVariables() is called in your configuration setup to enable environment variable support.
    /// </remarks>
    public static OfrepOptions FromConfiguration(IConfiguration? configuration, ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;

        var endpoint = GetConfigValue(configuration, EnvVarEndpoint);
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException(
                $"Configuration key '{EnvVarEndpoint}' or environment variable {EnvVarEndpoint} is required but was not set or is empty.",
                EnvVarEndpoint);
        }

        var options = new OfrepOptions(endpoint!);

        // Parse timeout
        var timeoutStr = GetConfigValue(configuration, EnvVarTimeout);
        if (!string.IsNullOrWhiteSpace(timeoutStr))
        {
            if (int.TryParse(timeoutStr, out var timeoutMs) && timeoutMs > 0)
            {
                options.Timeout = TimeSpan.FromMilliseconds(timeoutMs);
            }
            else
            {
                logger.Log(
                    LogLevel.Warning,
                    "Invalid value '{TimeoutValue}' for configuration key {ConfigKey}. Using default timeout of 10 seconds.",
                    timeoutStr,
                    EnvVarTimeout);
            }
        }

        // Parse headers
        var headersStr = GetConfigValue(configuration, EnvVarHeaders);
        if (!string.IsNullOrWhiteSpace(headersStr))
        {
            options.Headers = ParseHeaders(headersStr!, logger);
        }

        return options;
    }

    /// <summary>
    /// Gets a configuration value by key, falling back to environment variable if IConfiguration is not available or doesn't contain the key.
    /// </summary>
    private static string? GetConfigValue(IConfiguration? configuration, string key)
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
    /// Parses a header string in the format "Key1=Value1,Key2=Value2" with escape sequence support.
    /// Escape sequences: \\ for literal backslash, \, for literal comma, \= for literal equals.
    /// </summary>
    /// <param name="headersString">The headers string to parse.</param>
    /// <param name="logger">Optional logger for warnings about malformed entries.</param>
    /// <returns>A dictionary of parsed headers.</returns>
    internal static Dictionary<string, string> ParseHeaders(string headersString, ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        var headers = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(headersString))
        {
            return headers;
        }

        // Split by unescaped commas
        var pairs = SplitByUnescapedDelimiter(headersString, ',');

        foreach (var pair in pairs)
        {
            if (string.IsNullOrWhiteSpace(pair))
            {
                continue;
            }

            // Split by first unescaped equals sign
            var keyValue = SplitByUnescapedDelimiter(pair, '=', maxParts: 2);

            if (keyValue.Count < 2)
            {
                logger.Log(
                    LogLevel.Warning,
                    "Malformed header entry '{Entry}' in {EnvVar}. Expected format: Key=Value. Skipping.",
                    pair,
                    EnvVarHeaders);
                continue;
            }

            var key = Unescape(keyValue[0]).Trim();
            var value = Unescape(keyValue[1]).Trim();

            if (string.IsNullOrEmpty(key))
            {
                logger.Log(
                    LogLevel.Warning,
                    "Empty header key in entry '{Entry}' in {EnvVar}. Skipping.",
                    pair,
                    EnvVarHeaders);
                continue;
            }

            headers[key] = value;
        }

        return headers;
    }

    /// <summary>
    /// Splits a string by an unescaped delimiter character.
    /// Backslash is the escape character: \\ = literal \, \{delimiter} = literal delimiter.
    /// </summary>
    private static List<string> SplitByUnescapedDelimiter(string input, char delimiter, int maxParts = int.MaxValue)
    {
        var parts = new List<string>();
        var current = new StringBuilder();
        var i = 0;

        while (i < input.Length)
        {
            if (parts.Count >= maxParts - 1)
            {
                // Last part: take the rest of the string
                current.Append(input.Substring(i));
                break;
            }

            var c = input[i];

            if (c == '\\' && i + 1 < input.Length)
            {
                // Escape sequence: keep the backslash and next character for later unescaping
                current.Append(c);
                current.Append(input[i + 1]);
                i += 2;
            }
            else if (c == delimiter)
            {
                parts.Add(current.ToString());
                current.Clear();
                i++;
            }
            else
            {
                current.Append(c);
                i++;
            }
        }

        parts.Add(current.ToString());
        return parts;
    }

    /// <summary>
    /// Unescapes a string by processing escape sequences: \\ -> \, \, -> ,, \= -> =.
    /// </summary>
    private static string Unescape(string input)
    {
        if (string.IsNullOrEmpty(input) || !input.Contains('\\'))
        {
            return input;
        }

        var result = new StringBuilder(input.Length);
        var i = 0;

        while (i < input.Length)
        {
            var c = input[i];

            if (c == '\\' && i + 1 < input.Length)
            {
                var next = input[i + 1];
                // Unescape known sequences
                if (next == '\\' || next == ',' || next == '=')
                {
                    result.Append(next);
                    i += 2;
                    continue;
                }
            }

            result.Append(c);
            i++;
        }

        return result.ToString();
    }
}
