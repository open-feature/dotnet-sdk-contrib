using System.Text.Json.Serialization;

namespace OpenFeature.Providers.GOFeatureFlag.Models;

/// <summary>
///     This record represents a feature event, used to send evaluation events to the GO Feature Flag server.
///     Using a record provides immutability.
/// </summary>
public sealed record FeatureEvent : CommonEvent
{
    /*
     * Property "kind" is added by the JsonPolymorphic attribute in IEvent interface.
     */

    /// <summary>
    ///     Default value is true if the feature is using the default value.
    /// </summary>
    [JsonPropertyName("defaultValue")]
    public bool DefaultValue { get; init; }

    /// <summary>
    ///     Value of the feature flag evaluation result.
    /// </summary>
    [JsonPropertyName("value")]
    public object? Value { get; init; }

    /// <summary>
    ///     Variation is the variation of the feature flag that was returned by the evaluation.
    /// </summary>
    [JsonPropertyName("variation")]
    public string Variation { get; init; } = "no-variant";

    /// <summary>
    ///     Version is the version of the feature flag that was evaluated.
    ///     If the feature flag is not versioned, this will be null or empty.
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; init; }
}
