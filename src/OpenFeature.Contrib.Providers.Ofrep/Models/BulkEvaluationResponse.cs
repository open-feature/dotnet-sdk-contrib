using System.Text.Json.Serialization;

namespace OpenFeature.Contrib.Providers.Ofrep.Models;

/// <summary>
/// Represents the overall response from the OFREP bulk evaluation endpoint (/ofrep/v1/evaluate/flags).
/// </summary>
public class BulkEvaluationResponse
{
    /// <summary>
    /// A list containing the evaluation results for each requested flag.
    /// </summary>
    [JsonPropertyName("flags")]
    public List<BulkEvaluationFlag> Flags { get; set; } =
        new List<BulkEvaluationFlag>(); // Initialize to avoid null


    /// <summary>
    /// Optional metadata associated with the bulk evaluation response itself.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; }
}
