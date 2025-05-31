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

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderFlagEvaluation"/> class with the specified supported types.
    /// </summary>
    /// <param name="supportedTypes">An array of strings representing the types supported by this provider flag evaluation.</param>
    public ProviderFlagEvaluation(string[] supportedTypes)
    {
        SupportedTypes = supportedTypes;
    }
}
