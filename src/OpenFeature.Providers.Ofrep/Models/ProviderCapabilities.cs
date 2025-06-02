using System.Text.Json.Serialization;

namespace OpenFeature.Providers.Ofrep.Models;

/// <summary>
/// Represents the capabilities of the OFREP provider.
/// </summary>
public class ProviderCapabilities
{
    /// <summary>
    /// Gets or sets the flag evaluation capabilities.
    /// </summary>
    [JsonPropertyName("flagEvaluation")]
    public ProviderFlagEvaluation? FlagEvaluation { get; set; }
}
