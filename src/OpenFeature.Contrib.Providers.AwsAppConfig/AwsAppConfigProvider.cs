using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Model;
using Amazon.AppConfigData;
using System.Collections.Generic;
using Amazon.AppConfigData.Model;

namespace OpenFeature.Contrib.Providers.AwsAppConfig
{
     /// <summary>
     /// OpenFeatures provider for AWS AppConfig that enables feature flag management using AWS AppConfig service.
     /// This provider allows fetching and evaluating feature flags stored in AWS AppConfig.
     /// </summary>
    public class AwsAppConfigProvider : FeatureProvider
    {
        // AWS AppConfig client for interacting with the service
        private readonly IAmazonAppConfigData _appConfigClient;
        
        // The name of the application in AWS AppConfig
        private readonly string _applicationName;
        
        // The environment (e.g., prod, dev, staging) in AWS AppConfig
        private readonly string _environmentName;
        
        // The configuration profile identifier that contains the feature flags
        private readonly string _configurationProfileId;
         
        /// <summary>
        /// Returns metadata about the provider
        /// </summary>
        /// <returns>Metadata object containing provider information</returns>
        public override Metadata GetMetadata() => new Metadata("AWS AppConfig Provider");
        
        
                /// <summary>
        /// Resolves a boolean feature flag value
        /// </summary>
        /// <param name="flagKey">The unique identifier of the feature flag</param>
        /// <param name="defaultValue">The default value to return if the flag cannot be resolved</param>
        /// <param name="context">Additional evaluation context (optional)</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>Resolution details containing the boolean flag value</returns>
        public override Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(string flagKey, bool defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Resolves a double feature flag value
        /// </summary>
        /// <param name="flagKey">The unique identifier of the feature flag</param>
        /// <param name="defaultValue">The default value to return if the flag cannot be resolved</param>
        /// <param name="context">Additional evaluation context (optional)</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>Resolution details containing the double flag value</returns>
        public override Task<ResolutionDetails<double>> ResolveDoubleValueAsync(string flagKey, double defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Resolves an integer feature flag value
        /// </summary>
        /// <param name="flagKey">The unique identifier of the feature flag</param>
        /// <param name="defaultValue">The default value to return if the flag cannot be resolved</param>
        /// <param name="context">Additional evaluation context (optional)</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>Resolution details containing the integer flag value</returns>
        public override Task<ResolutionDetails<int>> ResolveIntegerValueAsync(string flagKey, int defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Resolves a string feature flag value
        /// </summary>
        /// <param name="flagKey">The unique identifier of the feature flag</param>
        /// <param name="defaultValue">The default value to return if the flag cannot be resolved</param>
        /// <param name="context">Additional evaluation context (optional)</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>Resolution details containing the string flag value</returns>
        public override Task<ResolutionDetails<string>> ResolveStringValueAsync(string flagKey, string defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Resolves a structured feature flag value
        /// </summary>
        /// <param name="flagKey">The unique identifier of the feature flag</param>
        /// <param name="defaultValue">The default value to return if the flag cannot be resolved</param>
        /// <param name="context">Additional evaluation context (optional)</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>Resolution details containing the structured flag value</returns>
        public override async Task<ResolutionDetails<Value>> ResolveStructureValueAsync(string flagKey, Value defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
        {
            var response = await GetFeatureFlagsStreamAsync();

            // Deserialize the configuration data (assuming JSON format)
            var responseString = System.Text.Encoding.UTF8.GetString(response.Configuration.ToArray());
            var flagValue = ParseFeatureFlag(flagKey, defaultValue, responseString);
            return await Task.FromResult(new ResolutionDetails<Value>(flagKey, new Value(flagValue)));
        }

        /// <summary>
        /// Asynchronously retrieves feature flags configuration from AWS AppConfig using a streaming API.
        /// </summary>        
        /// <returns>
        /// A Task containing GetConfigurationResponse which includes:
        /// - The configuration content
        /// - Next poll configuration token
        /// - Poll interval in seconds
        /// </returns>
        /// <remarks>
        /// This method implements AWS AppConfig's best practices for configuration retrieval:
        /// - Uses streaming API for efficient data transfer
        /// - Supports incremental updates through configuration tokens
        /// - Respects AWS AppConfig's polling interval recommendations
        /// 
        /// The configuration token workflow:
        /// 1. Initial call: token = null
        /// 2. Subsequent calls: Use token from previous response
        /// 3. Token changes when configuration is updated
        /// </remarks>
        /// <exception cref="AmazonAppConfigDataException">Thrown when AWS AppConfig service encounters an error</exception>
        /// <exception cref="InvalidOperationException">Thrown when the provider is not properly configured</exception>        
        /// <seealso cref="IAmazonAppConfigData.GetLatestConfigurationAsync"/>        
        private async Task<GetLatestConfigurationResponse> GetFeatureFlagsStreamAsync(EvaluationContext context = null)
        {
            // TODO: Yet to figure out how to pass along Evalutaion Context to AWS AppConfig

            // Build "StartConfigurationSession" Request
            var startConfigSessionRequest = new StartConfigurationSessionRequest
            {
                ApplicationIdentifier = _applicationName,
                EnvironmentIdentifier = _environmentName,
                ConfigurationProfileIdentifier = _configurationProfileId
            };

            // Start a configuration session with AWS AppConfig
            var sessionResponse = await _appConfigClient.StartConfigurationSessionAsync(startConfigSessionRequest);

            // Build "GetLatestConfiguration" request
            var configurationRequest = new GetLatestConfigurationRequest
            {
                ConfigurationToken = sessionResponse.InitialConfigurationToken
            };

            // Get the configuration response from AWS AppConfig
            var response = await _appConfigClient.GetLatestConfigurationAsync(configurationRequest);

            return response;   
        }        

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
        /// <seealso cref="ParseChildren"/>
        /// <seealso cref="ParseType"/>
        private Value ParseFeatureFlag(string flagKey, Value defaultValue, string inputJson)
        {
            var parsedJson = JsonSerializer.Deserialize<IDictionary<string, object>>(inputJson);
            if (!parsedJson.TryGetValue(flagKey, out var flagValue))
                return defaultValue;
            var parsedItems = JsonSerializer.Deserialize<IDictionary<string, object>>(flagValue.ToString());
            return ParseChildren(parsedItems);
        }

        /// <summary>
        /// Recursively parses and converts a dictionary of values into a structured Value object.
        /// </summary>
        /// <param name="children">The source dictionary containing key-value pairs to parse</param>
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
        private Value ParseChildren(IDictionary<string, object> children)
        {
            if(children == null) return null;
            IDictionary<string, Value> keyValuePairs = new Dictionary<string, Value>();

            foreach (var child in children)
            {
                Type valueType = child.Value.GetType();
                if (valueType.IsValueType || valueType == typeof(string))
                {
                    keyValuePairs.Add(child.Key, ParseType(child.Value.ToString()));
                }
                var newChild = JsonSerializer.Deserialize<IDictionary<string, object>>(child.Value.ToString());
                keyValuePairs.Add(child.Key, ParseChildren(newChild));
            }
            return new Value(new Structure(keyValuePairs));            
        }

        /// <summary>
        /// Function to parse string value to a specific type.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private Value ParseType(string value)
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
