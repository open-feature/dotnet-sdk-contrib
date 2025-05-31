using System.Text.Json.Serialization;

namespace OpenFeature.Contrib.Providers.Ofrep.Models;

/// <summary>
/// Represents a single flag evaluation within a bulk evaluation response.
/// Inherits common evaluation properties from <see cref="OfrepEvaluationBase{TValue}"/>,
/// using <see cref="object"/> as the type for the value to accommodate mixed types.
/// </summary>
public class BulkEvaluationFlag : OfrepEvaluationBase<object>
{
    /// <summary>
    /// The unique key of the evaluated flag.
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty; // Initialize to avoid null warnings


    /// <summary>
    /// Optional metadata associated with the flag evaluation.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}
