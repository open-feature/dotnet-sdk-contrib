using OpenFeature.Constant;
using OpenFeature.Model;
using Statsig;
using Statsig.Server;
using Statsig.Server.Evaluation;
using System;
using System.Threading.Tasks;

namespace OpenFeature.Contrib.Providers.Statsig
{
    /// <summary>
    /// An OpenFeature <see cref="FeatureProvider"/> which enables the use of the Statsig Server-Side SDK for .NET
    /// with OpenFeature.
    /// </summary>
    /// <example>
    ///     var provider = new StatsigProvider("my-sdk-key"), new StatsigProviderOptions(){LocalMode = false});
    ///
    ///     OpenFeature.Api.Instance.SetProvider(provider);
    ///
    ///     var client = OpenFeature.Api.Instance.GetClient();
    /// </example>
    public sealed class StatsigProvider : FeatureProvider
    {
        volatile bool initialized = false;
        private readonly Metadata _providerMetadata = new Metadata("Statsig provider");
        private readonly string _sdkKey = "secret-"; //Dummy sdk key that works with local mode
        internal readonly ServerDriver ServerDriver;

        /// <summary>
        /// Creates new instance of <see cref="StatsigProvider"/>
        /// </summary>
        /// <param name="sdkKey">SDK Key to access Statsig.</param>
        /// <param name="statsigServerOptions">The StatsigServerOptions to configure the provider.</param>
        public StatsigProvider(string sdkKey = null, StatsigServerOptions statsigServerOptions = null)
        {
            if (sdkKey != null)
            {
                _sdkKey = sdkKey;
            }
            ServerDriver = new ServerDriver(_sdkKey, statsigServerOptions);
        }

        /// <inheritdoc/>
        public override Metadata GetMetadata() => _providerMetadata;

        /// <inheritdoc/>
        public override Task<ResolutionDetails<bool>> ResolveBooleanValue(string flagKey, bool defaultValue, EvaluationContext context = null)
        {
            var result = ServerDriver.GetFeatureGate(context.AsStatsigUser(), flagKey);
            var gateFound = false;
            var responseType = ErrorType.None;

            switch (result.Reason)
            {
                case EvaluationReason.Network:
                case EvaluationReason.LocalOverride:
                case EvaluationReason.Bootstrap:
                case EvaluationReason.DataAdapter:
                    gateFound = true;
                    break;
                case EvaluationReason.Unrecognized:
                    responseType = ErrorType.FlagNotFound;
                    break;
                case EvaluationReason.Uninitialized:
                    responseType = ErrorType.ProviderNotReady;
                    break;
                case EvaluationReason.Unsupported:
                    responseType = ErrorType.InvalidContext;
                    break;
                case EvaluationReason.Error:
                    responseType = ErrorType.General;
                    break;
                case null:
                    break;
            }
            return Task.FromResult(new ResolutionDetails<bool>(flagKey, gateFound ? result.Value : defaultValue, responseType));
        }

        /// <inheritdoc/>
        public override Task<ResolutionDetails<double>> ResolveDoubleValue(string flagKey, double defaultValue, EvaluationContext context = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override Task<ResolutionDetails<int>> ResolveIntegerValue(string flagKey, int defaultValue, EvaluationContext context = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override Task<ResolutionDetails<string>> ResolveStringValue(string flagKey, string defaultValue, EvaluationContext context = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override Task<ResolutionDetails<Value>> ResolveStructureValue(string flagKey, Value defaultValue, EvaluationContext context = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override ProviderStatus GetStatus()
        {
            return initialized ? ProviderStatus.Ready : ProviderStatus.NotReady;
        }

        /// <inheritdoc/>
        public override async Task Initialize(EvaluationContext context)
        {
            var initResult = await ServerDriver.Initialize();
            if (initResult == InitializeResult.Success || initResult == InitializeResult.LocalMode || initResult == InitializeResult.AlreadyInitialized)
            {
                initialized = true;
            }
        }

        /// <inheritdoc/>
        public override Task Shutdown()
        {
            return ServerDriver.Shutdown();
        }
    }
}
