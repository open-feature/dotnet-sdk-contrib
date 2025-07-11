using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenFeature.Providers.AzureAppConfig;

/// <summary>
/// Represents a feature flag configuration from Azure App Configuration.
/// </summary>
internal class FeatureFlag
{
    /// <summary>
    /// Gets or sets the unique identifier for the feature flag.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the feature flag is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the conditions under which the feature flag should be enabled.
    /// </summary>
    [JsonPropertyName("conditions")]
    public FeatureFlagConditions Conditions { get; set; }
}

/// <summary>
/// Represents the conditions for a feature flag.
/// </summary>
internal class FeatureFlagConditions
{
    /// <summary>
    /// Gets or sets the client filters that determine when the feature flag is enabled.
    /// </summary>
    [JsonPropertyName("client_filters")]
    public List<ClientFilter> ClientFilters { get; set; }
}

/// <summary>
/// Represents a client filter for feature flag evaluation.
/// </summary>
internal class ClientFilter
{
    /// <summary>
    /// Gets or sets the name of the filter.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the parameters for the filter.
    /// </summary>
    [JsonPropertyName("parameters")]
    public Dictionary<string, object> Parameters { get; set; }
}
