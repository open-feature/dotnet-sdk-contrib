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
}
