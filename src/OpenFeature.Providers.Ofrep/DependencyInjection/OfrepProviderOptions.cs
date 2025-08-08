#if NETFRAMEWORK
using System.Net.Http;
#endif

namespace OpenFeature.Providers.Ofrep.DependencyInjection;

/// <summary>
/// Configuration options for registering the OfrepProvider via DI.
/// </summary>
public record OfrepProviderOptions
{
    /// <summary>
    /// Default options name for Ofrep provider registrations.
    /// </summary>
    public const string DefaultName = "OfrepProvider";

    /// <summary>
    /// The base URL for the OFREP endpoint. Required.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// HTTP request timeout in seconds. Defaults to 10.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Optional additional HTTP headers.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Optional named HttpClient to use via IHttpClientFactory.
    /// If set, the provider will resolve an IHttpClientFactory and create the named client.
    /// You must register the client in your ServiceCollection using AddHttpClient(name, ...).
    /// </summary>
    public string? HttpClientName { get; set; }

    /// <summary>
    /// Optional callback to configure the HttpClient used by the provider.
    /// If <see cref="HttpClientName"/> is set, the named client will be resolved first and then this delegate is invoked.
    /// If not set, a default client will be created (preferably from IHttpClientFactory if available) and then configured.
    /// </summary>
    public Action<IServiceProvider, HttpClient>? ConfigureHttpClient { get; set; }
}
