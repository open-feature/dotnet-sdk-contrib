using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.v2.model;

/// <summary>
///     Represents the context of a flag in the GO Feature Flag system.
///     Contains the default SDK value and evaluation context enrichment.
/// </summary>
public class FlagContext
{
    /// <summary>
    ///     The default value to return from the SDK if no rule matches.
    /// </summary>
    [JsonPropertyName("defaultSdkValue")]
    public object DefaultSdkValue { get; set; }

    /// <summary>
    ///     Additional context values to enrich the evaluation context.
    /// </summary>
    [JsonPropertyName("evaluationContextEnrichment")]
    public IDictionary<string, object> EvaluationContextEnrichment { get; set; }
}
