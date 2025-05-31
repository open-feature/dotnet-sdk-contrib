using System.Text.Json.Serialization;

namespace OpenFeature.Providers.Ofrep.Models;

/// <summary>
/// Represents the cache invalidation configuration for the provider.
/// </summary>
public class ProviderCacheInvalidation
{
    /// <summary>
    /// Gets or sets the polling configuration for cache invalidation.
    /// </summary>
    [JsonPropertyName("polling")]
    public FeatureCacheInvalidationPolling? Polling { get; set; }
}
