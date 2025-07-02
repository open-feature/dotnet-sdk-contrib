using System;
using System.Text.Json.Serialization;

namespace OpenFeature.Providers.GOFeatureFlag.model;

/// <summary>
///     Represents a scheduled step in the rollout of a feature flag.
/// </summary>
public class ScheduledStep : FlagBase
{
    /// <summary>
    ///     The date of the scheduled step.
    /// </summary>
    [JsonPropertyName("date")]
    public DateTime? Date { get; set; }
}
