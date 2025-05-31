using System.Text.Json.Serialization;

namespace OpenFeature.Contrib.Providers.Ofrep.Models;

/// <summary>
/// Abstract base class for OFREP evaluation results, containing common properties.
/// </summary>
/// <typeparam name="TValue">The type of the flag value.</typeparam>
public abstract class OfrepEvaluationBase<TValue>
{
    /// <summary>
    /// The evaluated value of the flag.
    /// </summary>
    [JsonPropertyName("value")]
    public TValue Value { get; set; }

    /// <summary>
    /// The reason for the evaluation result (e.g., STATIC, TARGETING_MATCH).
    /// </summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; }

    /// <summary>
    /// The specific variant of the flag that was evaluated.
    /// </summary>
    [JsonPropertyName("variant")]
    public string Variant { get; set; }
}
