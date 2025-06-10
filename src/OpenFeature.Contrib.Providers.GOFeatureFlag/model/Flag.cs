using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.v2.model;

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
