using System.Text.Json.Serialization;

namespace OpenFeature.Contrib.Providers.Ofrep.Configuration;

/// <summary>
/// Configuration options for the OFREP provider.
/// </summary>
public class OfrepConfiguration
{
    /// <summary>
    /// Gets or sets the base URL for the OFREP API.
    /// </summary>
    [JsonPropertyName("baseUrl")]
    public string BaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the timeout for HTTP requests. Default is 5 seconds.
    /// </summary>
    [JsonPropertyName("timeout")]
    public TimeSpan Timeout { get; set; }

    /// <summary>
    /// Gets or sets additional HTTP headers to include in requests.
    /// </summary>
    [JsonPropertyName("headers")]
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Gets or sets the authorization header value.
    /// </summary>
    [JsonPropertyName("authorizationHeader")]

    public string? AuthorizationHeader { get; set; }
    /// <summary>
    /// Gets or sets the cache duration for evaluation responses. Default is 1000ms.
    /// </summary>

    [JsonPropertyName("cacheDuration")]
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMilliseconds(1000);

    /// <summary>
    /// Gets or sets the maximum number of items to cache. Default is 1000.
    /// </summary>
    [JsonPropertyName("maxCacheSize")]
    public int MaxCacheSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets whether to use absolute expiration in addition to sliding expiration.
    /// </summary>
    [JsonPropertyName("enableAbsoluteExpiration")]
    public bool EnableAbsoluteExpiration { get; set; } = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="OfrepConfiguration"/> class with the specified base URL.
    /// </summary>
    /// <param name="baseUrl">The base URL for the OFREP (OpenFeature Remote Evaluation Protocol) endpoint. Must be a valid absolute URI.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="baseUrl"/> is null, empty, or not a valid absolute URI.</exception>
    public OfrepConfiguration(string baseUrl)
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
}
