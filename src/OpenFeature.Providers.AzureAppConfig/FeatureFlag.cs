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
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the feature flag is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the variants for the feature flag.
    /// </summary>
    [JsonPropertyName("variants")]
    public List<FeatureFlagVariant>? Variants { get; set; }

    /// <summary>
    /// Gets or sets the default value when the feature flag is enabled.
    /// </summary>
    [JsonPropertyName("allocation")]
    public Allocation Allocation { get; set; } = new Allocation();
}

/// <summary>
/// Represents the allocation for a feature flag.
/// </summary>
internal class Allocation
{
    /// <summary>
    /// Gets or sets the default value when the feature flag is enabled.
    /// </summary>
    [JsonPropertyName("default_when_enabled")]
    public string DefaultWhenEnabled { get; set; } = string.Empty;
}

/// <summary>
/// Represents a variant for a feature flag.
/// </summary>
internal class FeatureFlagVariant
{
    /// <summary>
    /// Gets or sets the name of the variant.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configuration value for the variant.
    /// </summary>
    [JsonPropertyName("configuration_value")]
    public bool ConfigurationValue { get; set; }
}
