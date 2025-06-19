using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.v2.model;

/// <summary>
///     FlagConfigResponse is a class that represents the response of the flag configuration.
/// </summary>
public class FlagConfigResponse
{
    /// <summary>
    ///     Flags is a dictionary that contains the flag key and its corresponding Flag object.
    /// </summary>
    [JsonPropertyName("flags")]
    public IDictionary<string, Flag> Flags { get; set; }

    /// <summary>
    ///     EvaluationContextEnrichment is a dictionary that contains additional context for the evaluation of flags.
    /// </summary>
    [JsonPropertyName("evaluationContextEnrichment")]
    public IDictionary<string, object> EvaluationContextEnrichment { get; set; }

    /// <summary>
    ///     Etag is a string that represents the entity tag of the flag configuration response.
    /// </summary>
    [JsonPropertyName("etag")]
    public string Etag { get; set; }

    /// <summary>
    ///     LastUpdated is a nullable DateTime that represents the last time the flag configuration was updated.
    /// </summary>
    [JsonPropertyName("lastUpdated")]
    public DateTimeOffset? LastUpdated { get; set; }
}
