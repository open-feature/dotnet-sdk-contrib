using System.Text.Json.Serialization;

namespace OpenFeature.Contrib.Providers.Ofrep.Models;

/// <summary>
/// Represents the polling configuration for feature cache invalidation.
/// </summary>
public class FeatureCacheInvalidationPolling
{
    /// <summary>
    /// Gets or sets a value indicating whether polling is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the minimum polling interval in milliseconds.
    /// </summary>
    [JsonPropertyName("minPollingIntervalMs")]
    public int MinPollingIntervalMs { get; set; }
}
