using System;

namespace OpenFeature.Contrib.Providers.AwsAppConfig
{
    /// <summary>
    /// Represents a key structure for AWS AppConfig feature flags with optional attributes.
    /// Keys can be in the format "flagKey" or "flagKey:attributeKey"
    /// </summary>
    public class AppConfigKey
    {
        /// <summary>
        /// The separator used to split the flag key from its attribute key
        /// </summary>
        private const string Separator = ":";

        /// <summary>
        /// Gets the main feature flag key
        /// </summary>
        public string FlagKey { get; }

        /// <summary>
        /// Gets the optional attribute key associated with the feature flag
        /// </summary>
        public string AttributeKey { get; }

        /// <summary>
        /// Gets whether this key has an attribute component
        /// </summary>
        public bool HasAttribute => !string.IsNullOrEmpty(AttributeKey);

        /// <summary>
        /// Initializes a new instance of the AppConfigKey class
        /// </summary>
        /// <param name="key">The composite key string in format "flagKey" or "flagKey:attributeKey"</param>
        /// <exception cref="ArgumentException">Thrown when the key is null, empty, or consists only of whitespace</exception>
        public AppConfigKey(string key)
        {
            if(string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or empty");
            }

            var parts = key.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
            
            FlagKey = parts[0];
            // At this point, AWS AppConfig allows only value types for attributes. 
            // Hence ignoring anything afterwords.
            if (parts.Length > 1)
            {
                AttributeKey = parts[1];
            }   
        }
    }
}
