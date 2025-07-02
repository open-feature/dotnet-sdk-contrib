using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenFeature.Providers.GOFeatureFlag.model;

/// <summary>
///     Represents a feature flag for GO Feature Flag.
/// </summary>
public class Flag : FlagBase
{
    /// <summary>
    ///     The list of scheduled rollout steps for this flag.
    /// </summary>
    [JsonPropertyName("scheduledRollout")]
    public List<ScheduledStep> ScheduledRollout { get; set; }
}
