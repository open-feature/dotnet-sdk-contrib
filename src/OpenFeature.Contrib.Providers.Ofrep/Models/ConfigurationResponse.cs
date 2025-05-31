using System.Text.Json.Serialization;

namespace OpenFeature.Contrib.Providers.Ofrep.Models;

/// <summary>
/// Represents a response from the OFREP API.
/// </summary>
public class ConfigurationResponse
{
    /// <summary>
    /// Gets or sets the name of the OFREP server.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the capabilities of the OFREP server.
    /// </summary>
    [JsonPropertyName("capabilities")]
    public ProviderCapabilities? Capabilities { get; set; }
}
