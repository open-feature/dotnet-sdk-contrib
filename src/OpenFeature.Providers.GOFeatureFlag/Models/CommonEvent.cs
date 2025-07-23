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
#if NET7_0_OR_GREATER
    public required string ContextKind { get; init; }
# else
    public string ContextKind { get; init; } = string.Empty;
#endif


    /// <summary>
    ///     Feature flag name or key.
    /// </summary>
    [JsonPropertyName("key")]
#if NET7_0_OR_GREATER
    public required string Key { get; init; }
# else
    public string Key { get; init; } = string.Empty;
#endif

    /// <summary>
    ///     User key is the unique identifier for the user or context (the targetingKey).
    /// </summary>
    [JsonPropertyName("userKey")]
#if NET7_0_OR_GREATER
    public required string UserKey { get; init; }
# else
    public string UserKey { get; init; } = string.Empty;
#endif

}
