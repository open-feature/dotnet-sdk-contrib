using System.Text.Json.Serialization;

namespace OpenFeature.Providers.GOFeatureFlag.Models;

/// <summary>
///     Represents the progressive rollout of a feature flag.
/// </summary>
public class ProgressiveRollout
{
    /// <summary>
    ///     The initial step of the progressive rollout.
    /// </summary>
    [JsonPropertyName("initial")]
    public ProgressiveRolloutStep Initial { get; set; }

    /// <summary>
    ///     The end step of the progressive rollout.
    /// </summary>
    [JsonPropertyName("end")]
    public ProgressiveRolloutStep End { get; set; }
}
