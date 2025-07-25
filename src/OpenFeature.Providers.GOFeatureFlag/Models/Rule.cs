using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenFeature.Providers.GOFeatureFlag.Models;

/// <summary>
///     Represents a rule in the GO Feature Flag system.
/// </summary>
public class Rule
{
    /// <summary>
    ///     The name of the rule.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    ///     The query associated with the rule.
    /// </summary>
    [JsonPropertyName("query")]
    public string? Query { get; set; }

    /// <summary>
    ///     The variation to serve if the rule matches.
    /// </summary>
    [JsonPropertyName("variation")]
    public string? Variation { get; set; }

    /// <summary>
    ///     The percentage mapping for variations.
    /// </summary>
    [JsonPropertyName("percentage")]
    public Dictionary<string, double>? Percentage { get; set; }

    /// <summary>
    ///     Indicates if the rule is disabled.
    /// </summary>
    [JsonPropertyName("disable")]
    public bool Disable { get; set; }

    /// <summary>
    ///     The progressive rollout configuration for this rule.
    /// </summary>
    [JsonPropertyName("progressiveRollout")]
    public ProgressiveRollout? ProgressiveRollout { get; set; }
}
