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
        /// Gets the App config's Configuration Profile ID
        /// </summary>
        public string ConfigurationProfileId {get; }

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
        /// Initializes a new instance of the <see cref="AppConfigKey"/> class that represents a structured key
        /// for AWS AppConfig feature flags.
        /// </summary>
        /// <param name="key">
        /// The composite key string that must be in the format "configurationProfileId:flagKey[:attributeKey]" where:
        /// <list type="bullet">
        ///     <item><description>configurationProfileId - The AWS AppConfig configuration profile identifier</description></item>
        ///     <item><description>flagKey - The feature flag key name</description></item>
        ///     <item><description>attributeKey - (Optional) The specific attribute key to access within the feature flag</description></item>
        /// </list>
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when:
        /// <list type="bullet">
        ///     <item><description>The key parameter is null, empty, or consists only of whitespace</description></item>
        ///     <item><description>The key format is invalid (missing required parts)</description></item>
        ///     <item><description>The key doesn't contain at least configurationProfileId and flagKey parts</description></item>
        /// </list>
        /// </exception>
        /// <remarks>
        /// The constructor parses the provided key string and populates the corresponding properties:
        /// <list type="bullet">
        ///     <item><description><see cref="ConfigurationProfileId"/> - First part of the key</description></item>
        ///     <item><description><see cref="FlagKey"/> - Second part of the key</description></item>
        ///     <item><description><see cref="AttributeKey"/> - Third part of the key (if provided)</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// Valid key formats:
        /// <code>
        /// // Basic usage with configuration profile and flag key
        /// var key1 = new AppConfigKey("myProfile:myFlag");
        /// 
        /// // Usage with an attribute key
        /// var key2 = new AppConfigKey("myProfile:myFlag:myAttribute");
        /// </code>
        /// </example>
        public AppConfigKey(string key)
        {
            if(string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or empty");
            }

            var parts = key.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

            if(parts.Length < 2 )
            {
                throw new ArgumentException("Invalid key format. Flag key is expected in configurationProfileId:flagKey[:attributeKey] format");
            }
            
            ConfigurationProfileId = parts[0];
            FlagKey = parts[1];
            // At this point, AWS AppConfig allows only value types for attributes. 
            // Hence ignoring anything afterwords.
            if (parts.Length > 2)
            {
                AttributeKey = parts[2];
            }   
        }
    }
}
