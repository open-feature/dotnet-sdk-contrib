using System.Collections.Immutable;
using System.Text.Json.Serialization;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.v2.model;

/// <summary>
///     TrackingEvent is a record that represents a tracking event for a feature flag.
///     A tracking event is generated when we call the track method on the client.
///     Using a record provides immutability.
/// </summary>
public sealed record TrackingEvent : CommonEvent
{
    /*
     * Property "kind" is added by the JsonPolymorphic attribute in IEvent interface.
     */

    /// <summary>
    ///     EvaluationContext contains the evaluation context used for the tracking.
    /// </summary>
    [JsonPropertyName("evaluationContext")]
    public IImmutableDictionary<string, Value> EvaluationContext { get; init; }

    /// <summary>
    ///     TrackingDetails contains the details of the tracking event.
    /// </summary>
    [JsonPropertyName("trackingEventDetails")]
    public IImmutableDictionary<string, Value> TrackingEventDetails { get; init; }
}
