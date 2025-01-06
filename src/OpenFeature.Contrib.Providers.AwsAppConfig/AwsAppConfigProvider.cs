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
        public override async Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(string flagKey, bool defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
        {
            var responseString = await GetFeatureFlagsResponseJson();

            var flagValue = AwsFeatureFlagParser.ParseFeatureFlag(flagKey, new Value(defaultValue), responseString);           

            return new ResolutionDetails<bool>(flagKey, flagValue.AsBoolean ?? defaultValue);
        }

        /// <summary>
        /// Resolves a double feature flag value
        /// </summary>
        /// <param name="flagKey">The unique identifier of the feature flag</param>
        /// <param name="defaultValue">The default value to return if the flag cannot be resolved</param>
        /// <param name="context">Additional evaluation context (optional)</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>Resolution details containing the double flag value</returns>
        public override async Task<ResolutionDetails<double>> ResolveDoubleValueAsync(string flagKey, double defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
        {
            var responseString = await GetFeatureFlagsResponseJson();

            var flagValue = AwsFeatureFlagParser.ParseFeatureFlag(flagKey, new Value(defaultValue), responseString);           

            return new ResolutionDetails<double>(flagKey, flagValue.AsDouble ?? defaultValue);
        }

        /// <summary>
        /// Resolves an integer feature flag value
        /// </summary>
        /// <param name="flagKey">The unique identifier of the feature flag</param>
        /// <param name="defaultValue">The default value to return if the flag cannot be resolved</param>
        /// <param name="context">Additional evaluation context (optional)</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>Resolution details containing the integer flag value</returns>
        public override async Task<ResolutionDetails<int>> ResolveIntegerValueAsync(string flagKey, int defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
        {
            var responseString = await GetFeatureFlagsResponseJson();

            var flagValue = AwsFeatureFlagParser.ParseFeatureFlag(flagKey, new Value(defaultValue), responseString);           

            return new ResolutionDetails<int>(flagKey, flagValue.AsInteger ?? defaultValue);
        }

        /// <summary>
        /// Resolves a string feature flag value
        /// </summary>
        /// <param name="flagKey">The unique identifier of the feature flag</param>
        /// <param name="defaultValue">The default value to return if the flag cannot be resolved</param>
        /// <param name="context">Additional evaluation context (optional)</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>Resolution details containing the string flag value</returns>
        public override async Task<ResolutionDetails<string>> ResolveStringValueAsync(string flagKey, string defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
        {
            var responseString = await GetFeatureFlagsResponseJson();

            var flagValue = AwsFeatureFlagParser.ParseFeatureFlag(flagKey, new Value(defaultValue), responseString);           

            return new ResolutionDetails<string>(flagKey, flagValue.AsString ?? defaultValue);
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
            var responseString = await GetFeatureFlagsResponseJson();

            var flagValue = AwsFeatureFlagParser.ParseFeatureFlag(flagKey, defaultValue, responseString);
            return await Task.FromResult(new ResolutionDetails<Value>(flagKey, new Value(flagValue)));
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
