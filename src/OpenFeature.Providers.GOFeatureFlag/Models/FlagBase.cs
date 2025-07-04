using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenFeature.Providers.GOFeatureFlag.Models;

/// <summary>
///     Represents the base structure of a feature flag for GO Feature Flag.
/// </summary>
public abstract class FlagBase
{
    /// <summary>
    ///     The variations available for this flag.
    /// </summary>
    [JsonPropertyName("variations")]
    public Dictionary<string, object> Variations { get; set; }

    /// <summary>
    ///     The list of targeting rules for this flag.
    /// </summary>
    [JsonPropertyName("targeting")]
    public List<Rule> Targeting { get; set; }

    /// <summary>
    ///     The key used for bucketing users.
    /// </summary>
    [JsonPropertyName("bucketingKey")]
    public string BucketingKey { get; set; }

    /// <summary>
    ///     The default rule to apply if no targeting rule matches.
    /// </summary>
    [JsonPropertyName("defaultRule")]
    public Rule DefaultRule { get; set; }

    /// <summary>
    ///     The experimentation rollout configuration.
    /// </summary>
    [JsonPropertyName("experimentation")]
    public ExperimentationRollout Experimentation { get; set; }

    /// <summary>
    ///     Indicates if events should be tracked for this flag.
    /// </summary>
    [JsonPropertyName("trackEvents")]
    public bool? TrackEvents { get; set; }

    /// <summary>
    ///     Indicates if the flag is disabled.
    /// </summary>
    [JsonPropertyName("disable")]
    public bool? Disable { get; set; }

    /// <summary>
    ///     The version of the flag.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; }

    /// <summary>
    ///     Additional metadata for the flag.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; }
}
