using System.Text.Json.Serialization;

namespace OpenFeature.Contrib.Providers.Ofrep.Models
{
    /// <summary>
    /// Represents the capabilities of the OFREP provider.
    /// </summary>
    public class ProviderCapabilities
    {
        /// <summary>
        /// Gets or sets the cache invalidation capabilities.
        /// </summary>
        [JsonPropertyName("cacheInvalidation")]
        public ProviderCacheInvalidation CacheInvalidation { get; set; }

        /// <summary>
        /// Gets or sets the flag evaluation capabilities.
        /// </summary>
        [JsonPropertyName("flagEvaluation")]
        public ProviderFlagEvaluation FlagEvaluation { get; set; }

        /// <summary>
        /// Gets or sets the caching capabilities.
        /// </summary>
        [JsonPropertyName("caching")]
        public ProviderCaching Caching { get; set; }
    }
}