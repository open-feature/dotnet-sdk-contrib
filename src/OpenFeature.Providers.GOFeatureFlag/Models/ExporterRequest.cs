using System.Collections.Generic;
using System.Text.Json.Serialization;
using OpenFeature.Model;

namespace OpenFeature.Providers.GOFeatureFlag.Models;

/// <summary>
///     ExporterRequest is a class that represents the request to the GO Feature Flag data collector API.
/// </summary>
public class ExporterRequest
{
    /// <summary>
    ///     metadata is the metadata that will be sent in your evaluation data collector.
    /// </summary>
    [JsonPropertyName("meta")]
    public Structure metadata { get; set; } = Structure.Builder().Build();

    /// <summary>
    ///     events is the list of events that will be sent in your evaluation data collector.
    /// </summary>
    [JsonPropertyName("events")]
    public List<IEvent> Events { get; set; } = new();
}
