﻿using System;
using System.Threading.Tasks;
using OpenFeature.Contrib.Providers.Flagd.Resolver.Rpc;
using OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess;
using OpenFeature.Model;
using Metadata = OpenFeature.Model.Metadata;
using Value = OpenFeature.Model.Value;
using OpenFeature.Constant;

namespace OpenFeature.Contrib.Providers.Flagd
{
    /// <summary>
    ///     FlagdProvider is the OpenFeature provider for flagD.
    /// </summary>
    public sealed class FlagdProvider : FeatureProvider
    {
        const string ProviderName = "flagd Provider";
        private readonly FlagdConfig _config;
        private ProviderStatus _status;
        private readonly Metadata _providerMetadata = new Metadata(ProviderName);

        private readonly Resolver.Resolver _resolver;

        /// <summary>
        ///     Constructor of the provider. This constructor uses the value of the following
        ///     environment variables to initialise its client:
        ///     FLAGD_HOST                     - The host name of the flagd server (default="localhost")
        ///     FLAGD_PORT                     - The port of the flagd server (default="8013")
        ///     FLAGD_TLS                      - Determines whether to use https or not (default="false")
        ///     FLAGD_FLAGD_SERVER_CERT_PATH   - The path to the client certificate (default="")
        ///     FLAGD_SOCKET_PATH              - Path to the unix socket (default="")
        ///     FLAGD_CACHE                    - Enable or disable the cache (default="false")
        ///     FLAGD_MAX_CACHE_SIZE           - The maximum size of the cache (default="10")
        ///     FLAGD_MAX_EVENT_STREAM_RETRIES - The maximum amount of retries for establishing the EventStream
        ///     FLAGD_RESOLVER_TYPE            - The type of resolver (in-process or rpc) to be used for the provider
        /// </summary>
        public FlagdProvider() : this(new FlagdConfig())
        {
        }

        /// <summary>
        ///     Constructor of the provider. This constructor uses the value of the following
        ///     environment variables to initialise its client:
        ///     FLAGD_FLAGD_SERVER_CERT_PATH   - The path to the client certificate (default="")
        ///     FLAGD_CACHE                    - Enable or disable the cache (default="false")
        ///     FLAGD_MAX_CACHE_SIZE           - The maximum size of the cache (default="10")
        ///     FLAGD_MAX_EVENT_STREAM_RETRIES - The maximum amount of retries for establishing the EventStream
        ///     FLAGD_RESOLVER_TYPE            - The type of resolver (in-process or rpc) to be used for the provider
        ///     <param name="url">The URL of the flagd server</param>
        ///     <exception cref="ArgumentNullException">if no url is provided.</exception>
        /// </summary>
        public FlagdProvider(Uri url) : this(new FlagdConfig(url))
        {
        }

        /// <summary>
        ///     Constructor of the provider.
        ///     <param name="config">The FlagdConfig object</param>
        ///     <exception cref="ArgumentNullException">if no config object is provided.</exception>
        /// </summary>
        public FlagdProvider(FlagdConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _config = config;

            if (_config.ResolverType == ResolverType.IN_PROCESS)
            {
                _resolver = new InProcessResolver(_config);
            }
            else
            {
                _resolver = new RpcResolver(config);
            }
        }

        // just for testing, internal but visible in tests
        internal FlagdProvider(Resolver.Resolver resolver)
        {
            _resolver = resolver;
            _resolver.Init();
        }

        /// <inheritdoc/>
        public override ProviderStatus GetStatus()
        {
            return _status;
        }

        // just for testing, internal but visible in tests
        internal FlagdConfig GetConfig() => _config;

        /// <summary>
        /// Get the provider name.
        /// </summary>
        public static string GetProviderName()
        {
            return ProviderName;
        }

        /// <summary>
        ///     Return the metadata associated to this provider.
        /// </summary>
        public override Metadata GetMetadata() => _providerMetadata;

        /// <summary>
        ///     Return the resolver of the provider
        /// </summary>
        internal Resolver.Resolver GetResolver() => _resolver;

        /// <inheritdoc/>
        public override Task Initialize(EvaluationContext context)
        {
            return Task.Run(async () =>
            {
                await _resolver.Init();
                _status = ProviderStatus.Ready;

            }).ContinueWith((t) =>
            {
                _status = ProviderStatus.Error;
                if (t.IsFaulted) throw t.Exception;
            });
        }

        /// <inheritdoc/>
        public override Task Shutdown()
        {
            return _resolver.Shutdown().ContinueWith((t) =>
            {
                _status = ProviderStatus.NotReady;
                if (t.IsFaulted) throw t.Exception;
            });
        }

        /// <summary>
        ///     ResolveBooleanValue resolve the value for a Boolean Flag.
        /// </summary>
        /// <param name="flagKey">Name of the flag</param>
        /// <param name="defaultValue">Default value used in case of error.</param>
        /// <param name="context">Context about the user</param>
        /// <returns>A ResolutionDetails object containing the value of your flag</returns>
        public override async Task<ResolutionDetails<bool>> ResolveBooleanValue(string flagKey, bool defaultValue, EvaluationContext context = null)
        {
            return await _resolver.ResolveBooleanValue(flagKey, defaultValue, context).ConfigureAwait(false);
        }

        /// <summary>
        ///     ResolveStringValue resolve the value for a string Flag.
        /// </summary>
        /// <param name="flagKey">Name of the flag</param>
        /// <param name="defaultValue">Default value used in case of error.</param>
        /// <param name="context">Context about the user</param>
        /// <returns>A ResolutionDetails object containing the value of your flag</returns>
        public override async Task<ResolutionDetails<string>> ResolveStringValue(string flagKey, string defaultValue, EvaluationContext context = null)
        {
            return await _resolver.ResolveStringValue(flagKey, defaultValue, context).ConfigureAwait(false);
        }

        /// <summary>
        ///     ResolveIntegerValue resolve the value for an int Flag.
        /// </summary>
        /// <param name="flagKey">Name of the flag</param>
        /// <param name="defaultValue">Default value used in case of error.</param>
        /// <param name="context">Context about the user</param>
        /// <returns>A ResolutionDetails object containing the value of your flag</returns>
        public override async Task<ResolutionDetails<int>> ResolveIntegerValue(string flagKey, int defaultValue, EvaluationContext context = null)
        {

            return await _resolver.ResolveIntegerValue(flagKey, defaultValue, context).ConfigureAwait(false);
        }

        /// <summary>
        ///     ResolveDoubleValue resolve the value for a double Flag.
        /// </summary>
        /// <param name="flagKey">Name of the flag</param>
        /// <param name="defaultValue">Default value used in case of error.</param>
        /// <param name="context">Context about the user</param>
        /// <returns>A ResolutionDetails object containing the value of your flag</returns>
        public override async Task<ResolutionDetails<double>> ResolveDoubleValue(string flagKey, double defaultValue, EvaluationContext context = null)
        {
            return await _resolver.ResolveDoubleValue(flagKey, defaultValue, context).ConfigureAwait(false);
        }

        /// <summary>
        ///     ResolveStructureValue resolve the value for a Boolean Flag.
        /// </summary>
        /// <param name="flagKey">Name of the flag</param>
        /// <param name="defaultValue">Default value used in case of error.</param>
        /// <param name="context">Context about the user</param>
        /// <returns>A ResolutionDetails object containing the value of your flag</returns>
        public override async Task<ResolutionDetails<Value>> ResolveStructureValue(string flagKey, Value defaultValue, EvaluationContext context = null)
        {
            return await _resolver.ResolveStructureValue(flagKey, defaultValue, context).ConfigureAwait(false);
        }
    }
}
