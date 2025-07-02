using System;
using System.Text.Json.Serialization;

namespace OpenFeature.Providers.GOFeatureFlag.model;

/// <summary>
///     Represents the rollout period of an experimentation.
/// </summary>
public class ExperimentationRollout
{
    /// <summary>
    ///     The start date of the experimentation rollout.
    /// </summary>
    [JsonPropertyName("start")]
    public DateTime Start { get; set; }

    /// <summary>
    ///     The end date of the experimentation rollout.
    /// </summary>
    [JsonPropertyName("end")]
    public DateTime End { get; set; }
}
