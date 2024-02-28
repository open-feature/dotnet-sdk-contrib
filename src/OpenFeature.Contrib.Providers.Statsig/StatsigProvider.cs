using OpenFeature.Constant;
using OpenFeature.Contrib.Providers.Statsig;
using OpenFeature.Model;
using Statsig;
using Statsig.Client;
using Statsig.Server;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OpenFeature.Contrib.Providers.FeatureManagement
{
    /// <summary>
    /// An OpenFeature <see cref="FeatureProvider"/> which enables the use of the Statsig Server-Side SDK for .NET
    /// with OpenFeature.
    /// </summary>
    /// <example>
    ///     var provider = new Provider(Configuration.Builder("my-sdk-key").Build());
    ///
    ///     OpenFeature.Api.Instance.SetProvider(provider);
    ///
    ///     var client = OpenFeature.Api.Instance.GetClient();
    /// </example>
    public sealed class StatsigProvider : FeatureProvider
    {
        bool initialized = false;
        private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        private readonly Metadata _providerMetadata = new Metadata("Statsig provider");

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        public StatsigProvider(StatsigProviderOptions options)
        {
            ValidateInputOptions(options);
        }

        /// <summary>
        ///     validateInputOptions is validating the different options provided when creating the provider.
        /// </summary>
        /// <param name="options">Options used while creating the provider</param>
        /// <exception cref="InvalidOption">if no options are provided or we have a wrong configuration.</exception>
        private void ValidateInputOptions(StatsigProviderOptions options)
        {
            if (options is null) throw new StatsigProviderException("No options provided");

            if (string.IsNullOrEmpty(options.Endpoint))
                throw new StatsigProviderException("endpoint is a mandatory field when initializing the provider");
        }

        /// <inheritdoc/>
        public override Metadata GetMetadata() => _providerMetadata;
        
        /// <inheritdoc/>
        public override Task<ResolutionDetails<bool>> ResolveBooleanValue(string flagKey, bool defaultValue, EvaluationContext context = null)
        {

            if (GetStatus() ==  ProviderStatus.Ready)
            {
                var result = StatsigServer.CheckGateSync(context.AsStatsigUser(), flagKey);
                return Task.FromResult(new ResolutionDetails<bool>(flagKey, result));
            }
            return Task.FromResult(new ResolutionDetails<bool>(flagKey, defaultValue, ErrorType.ProviderNotReady));
        }

        public override Task<ResolutionDetails<double>> ResolveDoubleValue(string flagKey, double defaultValue, EvaluationContext context = null)
        {
            throw new NotImplementedException();
        }

        public override Task<ResolutionDetails<int>> ResolveIntegerValue(string flagKey, int defaultValue, EvaluationContext context = null)
        {
            throw new NotImplementedException();
        }

        public override Task<ResolutionDetails<string>> ResolveStringValue(string flagKey, string defaultValue, EvaluationContext context = null)
        {
            throw new NotImplementedException();
        }

        public override Task<ResolutionDetails<Value>> ResolveStructureValue(string flagKey, Value defaultValue, EvaluationContext context = null)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override ProviderStatus GetStatus()
        {
            return initialized ? ProviderStatus.NotReady : ProviderStatus.Ready;
        }

        /// <inheritdoc />
        public override async Task Initialize(EvaluationContext context)
        {
            if (!initialized)
            {
                await semaphore.WaitAsync();
                try
                {
                    var initResult = await StatsigServer.Initialize("secret-C83HBXKmN4cYaTINdO80YFoc5ogOAExpjsTy9ZO7LO2", new StatsigServerOptions() { LocalMode = false });
                    if (initResult == InitializeResult.Success || initResult == InitializeResult.AlreadyInitialized || initResult == InitializeResult.LocalMode)
                    {
                        initialized = true;
                    }
                    else
                        initialized = false;
                }
                finally
                {
                        semaphore.Release();
                }
            }

            //// We start listening for status changes and then we check the current status change. If we do not check
            //// then we could have missed a status change. If we check before registering a listener, then we could
            //// miss a change between checking and listening. Doing it this way we can get duplicates, but we filter
            //// when the status does not actually change, so we won't emit duplicate events.
            //if (_client.Initialized)
            //{
            //    _statusProvider.SetStatus(ProviderStatus.Ready);
            //    _initCompletion.TrySetResult(true);
            //}

            //if (_client.DataSourceStatusProvider.Status.State == DataSourceState.Off)
            //{
            //    _statusProvider.SetStatus(ProviderStatus.Error, ProviderShutdownMessage);
            //    _initCompletion.TrySetException(new LaunchDarklyProviderInitException(ProviderShutdownMessage));
            //}
        }

        /// <inheritdoc />
        public override Task Shutdown()
        {
            if (initialized)
                return StatsigServer.Shutdown();
            return Task.CompletedTask;
        }
    }
}
