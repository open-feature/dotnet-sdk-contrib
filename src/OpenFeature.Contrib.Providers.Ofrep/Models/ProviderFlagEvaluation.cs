using System.Text.Json.Serialization;

namespace OpenFeature.Contrib.Providers.Ofrep.Models;

/// <summary>
/// Represents the flag evaluation capabilities of the provider.
/// </summary>
public class ProviderFlagEvaluation
{
    /// <summary>
    /// Gets or sets the array of supported flag types.
    /// </summary>
    [JsonPropertyName("supportedTypes")]
    public string[] SupportedTypes { get; set; }
}
