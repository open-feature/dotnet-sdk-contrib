using System.Text.Json.Serialization;

namespace OpenFeature.Providers.GOFeatureFlag.Models;

/// <summary>
///     CommonEvent is an abstract record that represents the common properties of an event.
///     Using a record provides immutability.
/// </summary>
public abstract record CommonEvent : IEvent
{
    /// <summary>
    ///     Creation date of the event in seconds since epoch.
    /// </summary>
    [JsonPropertyName("creationDate")]
    public long CreationDate { get; init; }

    /// <summary>
    ///     ContextKind is the kind of context that generated an event.
    /// </summary>
    [JsonPropertyName("contextKind")]
    public required string ContextKind { get; init; }


    /// <summary>
    ///     Feature flag name or key.
    /// </summary>
    [JsonPropertyName("key")]
    public required string Key { get; init; }

    /// <summary>
    ///     User key is the unique identifier for the user or context (the targetingKey).
    /// </summary>
    [JsonPropertyName("userKey")]
    public required string UserKey { get; init; }
}
