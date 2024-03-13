using OpenFeature.Constant;
using OpenFeature.Error;
using OpenFeature.Model;
using Statsig;
using Statsig.Server;
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
        private readonly StatsigServerOptions _options;
        internal readonly ServerDriver ServerDriver;

        /// <summary>
        /// Creates new instance of <see cref="StatsigProvider"/>
        /// </summary>
        /// <param name="sdkKey">SDK Key to access Statsig.</param>
        /// <param name="configurationAction">The action used to configure the client.</param>
        public StatsigProvider(string sdkKey = null, Action<StatsigServerOptions> configurationAction = null)
        {
            if (sdkKey != null)
            {
                _sdkKey = sdkKey;
            }
            _options = new StatsigServerOptions();
            configurationAction?.Invoke(_options);
            ServerDriver = new ServerDriver(_sdkKey, _options);
        }

        /// <inheritdoc/>
        public override Metadata GetMetadata() => _providerMetadata;

        /// <inheritdoc/>
        public override Task<ResolutionDetails<bool>> ResolveBooleanValue(string flagKey, bool defaultValue, EvaluationContext context = null)
        {
            //TODO: defaultvalue = true not yet supported due to https://github.com/statsig-io/dotnet-sdk/issues/33
            if (defaultValue == true)
                throw new FeatureProviderException(ErrorType.General, "defaultvalue = true not supported (https://github.com/statsig-io/dotnet-sdk/issues/33)");
            if (GetStatus() != ProviderStatus.Ready)
                return Task.FromResult(new ResolutionDetails<bool>(flagKey, defaultValue, ErrorType.ProviderNotReady));
            var result = ServerDriver.CheckGateSync(context.AsStatsigUser(), flagKey);
            return Task.FromResult(new ResolutionDetails<bool>(flagKey, result));
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
