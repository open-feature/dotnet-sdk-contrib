﻿using System;
using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Model;
using Amazon.AppConfigData;
using Amazon.AppConfigData.Model;

namespace OpenFeature.Contrib.Providers.AwsAppConfig
{
     /// <summary>
     /// OpenFeatures provider for AWS AppConfig that enables feature flag management using AWS AppConfig service.
     /// This provider allows fetching and evaluating feature flags stored in AWS AppConfig.
     /// </summary>
    public class AppConfigProvider : FeatureProvider
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
        /// Constructor for AwsAppConfigProvider
        /// </summary>
        /// <param name="appConfigClient">The AWS AppConfig client</param>
        /// <param name="applicationName">The name of the application in AWS AppConfig</param>
        /// <param name="environmentName">The environment (e.g., prod, dev, staging) in AWS AppConfig</param>
        /// <param name="configurationProfileId">The configuration profile identifier that contains the feature flags</param>
        public AppConfigProvider(IAmazonAppConfigData appConfigClient, string applicationName, string environmentName, string configurationProfileId)
        {
            // Application name, environment name and configuration profile ID is needed for connecting to AWS Appconfig.
            // If any of these are missing, an exception will be thrown.
            
            if (string.IsNullOrEmpty(applicationName))
                throw new ArgumentNullException(nameof(applicationName));
            
            if (string.IsNullOrEmpty(environmentName))
                throw new ArgumentNullException(nameof(environmentName));
                
            if (string.IsNullOrEmpty(configurationProfileId))
                throw new ArgumentNullException(nameof(configurationProfileId));

            _appConfigClient = appConfigClient;
            _applicationName = applicationName;
            _environmentName = environmentName;
            _configurationProfileId = configurationProfileId;
        }
        
        /// <summary>
        /// Resolves a boolean feature flag value
        /// </summary>
        /// <param name="flagKey">The feature flag key, which can include an attribute specification in the format "flagKey:attributeKey"</param>
        /// <param name="defaultValue">The default value to return if the flag cannot be resolved</param>
        /// <param name="context">Additional evaluation context (optional)</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>Resolution details containing the boolean flag value</returns>
        public override async Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(string flagKey, bool defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
        {
            var attributeValue = await ResolveFeatureFlagValue(flagKey, new Value(defaultValue));
            return new ResolutionDetails<bool>(flagKey, attributeValue.AsBoolean ?? defaultValue);
        }

        /// <summary>
        /// Resolves a double feature flag value
        /// </summary>
        /// <param name="flagKey">The feature flag key, which can include an attribute specification in the format "flagKey:attributeKey"</param>
        /// <param name="defaultValue">The default value to return if the flag cannot be resolved</param>
        /// <param name="context">Additional evaluation context (optional)</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>Resolution details containing the double flag value</returns>
        public override async Task<ResolutionDetails<double>> ResolveDoubleValueAsync(string flagKey, double defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
        {
            var attributeValue = await ResolveFeatureFlagValue(flagKey, new Value(defaultValue));
            return new ResolutionDetails<double>(flagKey, attributeValue.AsDouble ?? defaultValue);
        }

        /// <summary>
        /// Resolves an integer feature flag value
        /// </summary>
        /// <param name="flagKey">The feature flag key, which can include an attribute specification in the format "flagKey:attributeKey"</param>
        /// <param name="defaultValue">The default value to return if the flag cannot be resolved</param>
        /// <param name="context">Additional evaluation context (optional)</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>Resolution details containing the integer flag value</returns>
        public override async Task<ResolutionDetails<int>> ResolveIntegerValueAsync(string flagKey, int defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
        {
            var attributeValue = await ResolveFeatureFlagValue(flagKey, new Value(defaultValue));
            return new ResolutionDetails<int>(flagKey, attributeValue.AsInteger ?? defaultValue);
        }

        /// <summary>
        /// Resolves a string feature flag value
        /// </summary>
        /// <param name="flagKey">The feature flag key, which can include an attribute specification in the format "flagKey:attributeKey"</param>
        /// <param name="defaultValue">The default value to return if the flag cannot be resolved</param>
        /// <param name="context">Additional evaluation context (optional)</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>Resolution details containing the string flag value</returns>
        public override async Task<ResolutionDetails<string>> ResolveStringValueAsync(string flagKey, string defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
        {
            var attributeValue = await ResolveFeatureFlagValue(flagKey, new Value(defaultValue));
            return new ResolutionDetails<string>(flagKey, attributeValue.AsString ?? defaultValue);
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
            var flagValue = await ResolveFeatureFlagValue(flagKey, defaultValue);
            return new ResolutionDetails<Value>(flagKey, new Value(flagValue));
        }

        /// <summary>
        /// Resolves a feature flag value from AWS AppConfig, optionally extracting a specific attribute.
        /// </summary>
        /// <param name="flagKey">The feature flag key, which can include an attribute specification in the format "flagKey:attributeKey"</param>
        /// <param name="defaultValue">The default value to return if the flag or attribute cannot be resolved</param>
        /// <returns>
        /// A Value object containing the resolved feature flag value. If the key includes an attribute specification,
        /// returns the value of that attribute. Otherwise, returns the entire flag value.
        /// </returns>
        /// <remarks>
        /// This method handles two types of feature flag resolution:
        /// 1. Simple flag resolution: When flagKey is a simple key (e.g., "myFlag")
        /// 2. Attribute resolution: When flagKey includes an attribute specification (e.g., "myFlag:someAttribute")
        /// 
        /// The method first retrieves the complete feature flag configuration and then:
        /// - For simple flags: Returns the entire flag value
        /// - For attribute-based flags: Returns the specific attribute value if it exists, otherwise returns the default value
        /// </remarks>
        /// <example>
        /// Simple flag usage:
        /// <code>
        /// var value = await ResolveFeatureFlagValue("myFlag", new Value(defaultValue));
        /// </code>
        /// 
        /// Attribute-based usage:
        /// <code>
        /// var value = await ResolveFeatureFlagValue("myFlag:color", new Value("blue"));
        /// </code>
        /// </example>
        private async Task<Value> ResolveFeatureFlagValue(string flagKey, Value defaultValue)
        {
            var responseString = await GetFeatureFlagsResponseJson();
            var appConfigKey = new AppConfigKey(flagKey);

            var flagValues = FeatureFlagParser.ParseFeatureFlag(appConfigKey.FlagKey, defaultValue, responseString);

            if (!appConfigKey.HasAttribute) return flagValues;

            var structuredValues = flagValues.AsStructure;

            if(structuredValues == null) return defaultValue;

            return structuredValues.TryGetValue(appConfigKey.AttributeKey, out var returnValue) ? returnValue : defaultValue;
        }


        /// <summary>
        /// Retrieves feature flag configurations as a string (json) from AWS AppConfig.
        /// </summary>
        /// <returns>A string containing JSON of the feature flag configuration data from AWS AppConfig.</returns>
        /// <remarks>
        /// This method fetches the feature flag configuration from AWS AppConfig service
        /// and returns it in its raw string format. The returned string is expected to be
        /// in JSON format that can be parsed into feature flag configurations.
        /// </remarks>
        /// <exception cref="AmazonAppConfigException">Thrown when there is an error retrieving the configuration from AWS AppConfig.</exception>
        private async Task<string> GetFeatureFlagsResponseJson()
        {
            var response = await GetFeatureFlagsStreamAsync();
            return System.Text.Encoding.UTF8.GetString(response.Configuration.ToArray());
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
    }
}
