using System;
using System.Text.Json.Serialization;

namespace OpenFeature.Providers.GOFeatureFlag.Models;

/// <summary>
///     Represents a step in the progressive rollout of a feature flag.
/// </summary>
public class ProgressiveRolloutStep
{
    /// <summary>
    ///     The variation to be served at this rollout step.
    /// </summary>
    [JsonPropertyName("variation")]
    public string Variation { get; set; }

    /// <summary>
    ///     The percentage of users to receive this variation at this step.
    /// </summary>
    [JsonPropertyName("percentage")]
    public float? Percentage { get; set; }

    /// <summary>
    ///     The date when this rollout step becomes active.
    /// </summary>
    [JsonPropertyName("date")]
    public DateTime Date { get; set; }
}
