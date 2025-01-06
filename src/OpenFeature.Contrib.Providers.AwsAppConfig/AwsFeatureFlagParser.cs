using System;
using System.Collections.Generic;
using System.Text.Json;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.AwsAppConfig
{
    /// <summary>
    /// Provides utility methods for parsing AWS AppConfig feature flag configurations into OpenFeature Value objects.
    /// This static class handles the conversion of JSON-formatted feature flag configurations from AWS AppConfig
    /// into strongly-typed OpenFeature Value objects.
    /// </summary>
    /// <remarks>
    /// The parser supports the following capabilities:
    /// - Parsing of JSON-structured feature flag configurations
    /// - Conversion of primitive types (boolean, numeric, string, datetime)
    /// - Handling of nested objects and complex structures
    /// - Support for default values when flags are not found
    /// 
    /// Type conversion precedence:
    /// 1. Boolean
    /// 2. Double
    /// 3. Integer
    /// 4. DateTime
    /// 5. String (default fallback)
    /// </remarks>    
    public static class AwsFeatureFlagParser
    {
        /// <summary>
        /// Parses a feature flag from a JSON configuration string and converts it to a Value object.
        /// </summary>
        /// <param name="flagKey">The unique identifier of the feature flag to retrieve</param>
        /// <param name="defaultValue">The default value to return if the flag is not found or cannot be parsed</param>
        /// <param name="inputJson">The JSON string containing the feature flag configuration</param>
        /// <returns>A Value object containing the parsed feature flag value, or the default value if not found</returns>
        /// <remarks>
        /// The method expects the JSON to be structured as a dictionary where:
        /// - The top level contains feature flag keys
        /// - Each feature flag value can be a primitive type or a complex object
        /// </remarks>
        /// <exception cref="JsonException">Thrown when the input JSON is invalid or cannot be deserialized</exception>
        /// <seealso cref="ParseAttributes"/>
        /// <seealso cref="ParseValueType"/>
        public static Value ParseFeatureFlag(string flagKey, Value defaultValue, string inputJson)
        {
            var parsedJson = JsonSerializer.Deserialize<IDictionary<string, object>>(inputJson);
            if (!parsedJson.TryGetValue(flagKey, out var flagValue))
                return defaultValue;
            var parsedItems = JsonSerializer.Deserialize<IDictionary<string, object>>(flagValue.ToString());
            return ParseAttributes(parsedItems);
        }

        /// <summary>
        /// Recursively parses and converts a dictionary of values into a structured Value object.
        /// </summary>
        /// <param name="attributes">The source dictionary containing key-value pairs to parse</param>
        /// <returns>A Value object containing the parsed structure</returns>
        /// <remarks>
        /// This method handles the following scenarios:
        /// - Primitive types (int, bool, double, etc.)
        /// - String values
        /// - Nested dictionaries (converted to structured Values)
        /// - Collections/Arrays (converted to list of Values)
        /// - Null values
        /// 
        /// For primitive types and strings, it creates a direct Value wrapper.
        /// For complex objects, it recursively processes their properties.
        /// </remarks>
        private static Value ParseAttributes(IDictionary<string, object> attributes)
        {
            if(attributes == null) return null;
            IDictionary<string, Value> keyValuePairs = new Dictionary<string, Value>();

            foreach (var attribute in attributes)
            {
                Type valueType = attribute.Value.GetType();
                if (valueType.IsValueType || valueType == typeof(string))
                {
                    keyValuePairs.Add(attribute.Key, ParseValueType(attribute.Value.ToString()));
                }
                else
                {
                    var newAttribute = JsonSerializer.Deserialize<IDictionary<string, object>>(attribute.Value.ToString());
                    keyValuePairs.Add(attribute.Key, ParseAttributes(newAttribute));
                }                
            }
            return new Value(new Structure(keyValuePairs));            
        }
        
        /// <summary>
        /// Function to parse string value to a specific type.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static Value ParseValueType(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return new Value();

            if (bool.TryParse(value, out bool boolValue))            
                return new Value(boolValue);
            
            if (double.TryParse(value, out double doubleValue))            
                return new Value(doubleValue);
            
            if (int.TryParse(value, out int intValue))            
                return new Value(intValue);

            if (DateTime.TryParse(value, out DateTime dateTimeValue))
                return new Value(dateTimeValue);

            // if no other type matches, return as string
            return new Value(value);
        }
    }
}


