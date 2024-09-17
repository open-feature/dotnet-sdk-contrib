using System;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client;
using ConfigCat.Client.Configuration;
using OpenFeature.Constant;
using OpenFeature.Error;
using OpenFeature.Model;

namespace OpenFeature.Contrib.ConfigCat
{
    /// <summary>
    /// ConfigCatProvider is the .NET provider implementation for the feature flag solution ConfigCat.
    /// </summary>
    public sealed class ConfigCatProvider : FeatureProvider
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
        public override Task InitializeAsync(EvaluationContext context, CancellationToken cancellationToken = default)
        {
            return Client.WaitForReadyAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public override Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            Client.Dispose();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public override Metadata GetMetadata()
        {
            return new Metadata(Name);
        }

        /// <inheritdoc/>
        public override Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(string flagKey, bool defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
        {
            return ResolveFlag(flagKey, context, defaultValue, cancellationToken);
        }

        /// <inheritdoc/>
        public override Task<ResolutionDetails<string>> ResolveStringValueAsync(string flagKey, string defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
        {
            return ResolveFlag(flagKey, context, defaultValue, cancellationToken);
        }

        /// <inheritdoc/>
        public override Task<ResolutionDetails<int>> ResolveIntegerValueAsync(string flagKey, int defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
        {
            return ResolveFlag(flagKey, context, defaultValue, cancellationToken);
        }

        /// <inheritdoc/>
        public override Task<ResolutionDetails<double>> ResolveDoubleValueAsync(string flagKey, double defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
        {
            return ResolveFlag(flagKey, context, defaultValue, cancellationToken);
        }

        /// <inheritdoc/>
        public override async Task<ResolutionDetails<Value>> ResolveStructureValueAsync(string flagKey, Value defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
        {
            var user = context?.BuildUser();
            var result = await Client.GetValueDetailsAsync(flagKey, defaultValue?.AsObject, user, cancellationToken);
            var returnValue = result.IsDefaultValue ? defaultValue : new Value(result.Value);
            var details = new ResolutionDetails<Value>(flagKey, returnValue, TranslateErrorCode(result.ErrorCode), errorMessage: result.ErrorMessage, variant: result.VariationId);
            if (details.ErrorType == ErrorType.None)
            {
                return details;
            }

            throw new FeatureProviderException(details.ErrorType, details.ErrorMessage);
        }

        private async Task<ResolutionDetails<T>> ResolveFlag<T>(string flagKey, EvaluationContext context, T defaultValue, CancellationToken cancellationToken)
        {
            var user = context?.BuildUser();
            var result = await Client.GetValueDetailsAsync(flagKey, defaultValue, user, cancellationToken);
            var details = new ResolutionDetails<T>(flagKey, result.Value, TranslateErrorCode(result.ErrorCode), errorMessage: result.ErrorMessage, variant: result.VariationId);
            if (details.ErrorType == ErrorType.None)
            {
                return details;
            }

            throw new FeatureProviderException(details.ErrorType, details.ErrorMessage);
        }

        private static ErrorType TranslateErrorCode(EvaluationErrorCode errorCode)
        {
            switch (errorCode)
            {
                case EvaluationErrorCode.None:
                    return ErrorType.None;
                case EvaluationErrorCode.InvalidConfigModel:
                    return ErrorType.ParseError;
                case EvaluationErrorCode.SettingValueTypeMismatch:
                    return ErrorType.TypeMismatch;
                case EvaluationErrorCode.ConfigJsonNotAvailable:
                    return ErrorType.ProviderNotReady;
                case EvaluationErrorCode.SettingKeyMissing:
                    return ErrorType.FlagNotFound;
                default:
                    return ErrorType.General;
            }
        }
    }
}