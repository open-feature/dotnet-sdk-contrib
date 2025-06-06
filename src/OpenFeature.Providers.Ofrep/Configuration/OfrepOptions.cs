using System.Text.Json.Serialization;

namespace OpenFeature.Providers.Ofrep.Configuration;

/// <summary>
/// Configuration options for the OFREP provider.
/// </summary>
public class OfrepOptions
{
    /// <summary>
    /// Gets or sets the base URL for the OFREP API.
    /// </summary>
    [JsonPropertyName("baseUrl")]
    public string BaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the timeout for HTTP requests. Default is 10 seconds.
    /// </summary>
    [JsonPropertyName("timeout")]
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets additional HTTP headers to include in requests.
    /// </summary>
    [JsonPropertyName("headers")]
    public Dictionary<string, string>? Headers { get; set; }

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
}
