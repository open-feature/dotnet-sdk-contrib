using System.Text.Json.Serialization;

namespace OpenFeature.Providers.GOFeatureFlag.Models;

/// <summary>
///     Interface for all events that are sent to the GO Feature Flag server.
/// </summary>
[JsonDerivedType(typeof(FeatureEvent), "feature")] // If kind == "feature", use FeatureEvent
[JsonDerivedType(typeof(TrackingEvent), "tracking")] // If kind == "tracking", use TrackingEvent
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")] // The property to check is "kind"
public interface IEvent
{
}
