using System.Text.Json.Serialization;

namespace OpenFeature.Providers.GOFeatureFlag.Models;

/// <summary>
///     Represents the progressive rollout of a feature flag.
/// </summary>
public class ProgressiveRollout
{
    /// <summary>
    /// Constructs a new instance of <see cref="ProgressiveRollout"/> with the specified initial and end steps.
    /// </summary>
    /// <param name="initial">The initial step of the progressive rollout.</param>
    /// <param name="end">The end step of the progressive rollout.</param>
    public ProgressiveRollout(ProgressiveRolloutStep initial, ProgressiveRolloutStep end)
    {
        Initial = initial;
        End = end;
    }
    /// <summary>
    ///     The initial step of the progressive rollout.
    /// </summary>
    [JsonPropertyName("initial")]
    public required ProgressiveRolloutStep Initial { get; set; }

    /// <summary>
    ///     The end step of the progressive rollout.
    /// </summary>
    [JsonPropertyName("end")]
    public required ProgressiveRolloutStep End { get; set; }
}
