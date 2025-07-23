using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenFeature.Providers.GOFeatureFlag.Models;

/// <summary>
///     EvaluationResponse represents the response from the GO Feature Flag evaluation.
/// </summary>
public class EvaluationResponse
{
    /// <summary>
    ///     Variation is the variation of the flag that was returned by the evaluation.
    /// </summary>
    [JsonPropertyName("variationType")]
    public string? VariationType { get; set; }

    /// <summary>
    ///     trackEvents indicates whether events should be tracked for this evaluation.
    /// </summary>
    [JsonPropertyName("trackEvents")]
    public bool TrackEvents { get; set; }

    /// <summary>
    ///     reason is the reason for the evaluation result.
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    /// <summary>
    ///     errorCode is the error code for the evaluation result, if any.
    /// </summary>
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    /// <summary>
    ///     errorDetails provides additional details about the error, if any.
    /// </summary>
    [JsonPropertyName("errorDetails")]
    public string? ErrorDetails { get; set; }

    /// <summary>
    ///     value is the evaluated value of the flag.
    /// </summary>
    [JsonPropertyName("value")]
    public object? Value { get; set; }

    /// <summary>
    ///     metadata is a dictionary containing additional metadata about the evaluation.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }


    // [JsonPropertyName("cacheable")]
    // public bool Cacheable { get; set; }
    // [JsonPropertyName("failed")]
    // public bool Failed { get; set; }
    // [JsonPropertyName("version")]
    // public string Version { get; set; }
}
