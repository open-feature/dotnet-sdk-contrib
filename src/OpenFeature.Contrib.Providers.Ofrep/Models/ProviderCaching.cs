using System.Text.Json.Serialization;

namespace OpenFeature.Contrib.Providers.Ofrep.Models
{
    /// <summary>
    /// Represents the caching configuration for the provider.
    /// </summary>
    public class ProviderCaching
    {
        /// <summary>
        /// Gets or sets a value indicating whether caching is enabled.
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the time-to-live for cached items in milliseconds.
        /// </summary>
        [JsonPropertyName("ttl")]
        public int TimeTolive { get; set; }
    }
}