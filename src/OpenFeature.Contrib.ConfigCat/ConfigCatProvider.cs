using System;
using System.Text.Json;
using System.Threading.Tasks;
using ConfigCat.Client;
using ConfigCat.Client.Configuration;
using OpenFeature.Constant;
using OpenFeature.Model;

namespace OpenFeature.Contrib.ConfigCat
{
    /// <summary>
    /// ConfigCatProvider is the .NET provider implementation for the feature flag solution ConfigCat.
    /// </summary>
    public class ConfigCatProvider: FeatureProvider
    {
        private const string Name = "ConfigCat Provider";
        internal readonly IConfigCatClient Client;

        /// <summary>
        /// Creates new instance of <see cref="ConfigCatProvider"/>
        /// </summary>
        /// <param name="sdkKey">SDK Key to access the ConfigCat config.</param>
        /// <param name="configBuilder">The action used to configure the client.</param>
        /// <exception cref="ArgumentNullException"><paramref name="sdkKey"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="sdkKey"/> is an empty string or in an invalid format.</exception>
        public ConfigCatProvider(string sdkKey, Action<ConfigCatClientOptions> configBuilder = null)
        {
            Client = ConfigCatClient.Get(sdkKey, configBuilder);
        }

        /// <inheritdoc/>
        public override Metadata GetMetadata()
        {
            return new Metadata(Name);
        }

        /// <inheritdoc/>
        public override async Task<ResolutionDetails<bool>> ResolveBooleanValue(string flagKey, bool defaultValue, EvaluationContext context = null)
        {
            return await ProcessFlag(flagKey, context, defaultValue);
        }

        /// <inheritdoc/>
        public override Task<ResolutionDetails<string>> ResolveStringValue(string flagKey, string defaultValue, EvaluationContext context = null)
        {
            return ProcessFlag(flagKey, context, defaultValue);
        }

        /// <inheritdoc/>
        public override Task<ResolutionDetails<int>> ResolveIntegerValue(string flagKey, int defaultValue, EvaluationContext context = null)
        {
            return ProcessFlag(flagKey, context, defaultValue);
        }

        /// <inheritdoc/>
        public override Task<ResolutionDetails<double>> ResolveDoubleValue(string flagKey, double defaultValue, EvaluationContext context = null)
        {
            return ProcessFlag(flagKey, context, defaultValue);
        }

        /// <inheritdoc/>
        public override Task<ResolutionDetails<Value>> ResolveStructureValue(string flagKey, Value defaultValue, EvaluationContext context = null)
        {
            throw new NotSupportedException();
        }

        private async Task<ResolutionDetails<T>> ProcessFlag<T>(string flagKey, EvaluationContext context, T defaultValue)
        {
            var user = context?.BuildUser();
            var result = await Client.GetValueDetailsAsync(flagKey, defaultValue, user);
            return string.IsNullOrEmpty(result.ErrorMessage)
                ? new ResolutionDetails<T>(flagKey, result.Value, variant: result.VariationId)
                : new ResolutionDetails<T>(flagKey, defaultValue, ParseErrorType(result.ErrorMessage), errorMessage: result.ErrorMessage);
        }

        private static ErrorType ParseErrorType(string errorMessage)
        {
            if(errorMessage.Contains("Config JSON is not present when evaluating setting"))
            {
                return ErrorType.ParseError;
            }
            if(errorMessage.Contains("Config JSON is not present"))
            {
                return ErrorType.ParseError;
            }
            if(errorMessage.Contains("the key was not found in config JSON"))
            {
                return ErrorType.FlagNotFound;
            }
            if(errorMessage.Contains("The type of a setting must match the type of the specified default value"))
            {
                return ErrorType.TypeMismatch;
            }
            return ErrorType.General;
        }
    }
}