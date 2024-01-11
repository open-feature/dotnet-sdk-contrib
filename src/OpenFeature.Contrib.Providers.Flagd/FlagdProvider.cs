using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using OpenFeature.Model;
using OpenFeature.Error;

using OpenFeature.Flagd.Grpc;
using Metadata = OpenFeature.Model.Metadata;
using Value = OpenFeature.Model.Value;
using ProtoValue = Google.Protobuf.WellKnownTypes.Value;
using System.Net.Sockets;
using System.Net.Http;
using System.Collections.Generic;

namespace OpenFeature.Contrib.Providers.Flagd
{
    /// <summary>
    ///     FlagdProvider is the OpenFeature provider for flagD.
    /// </summary>
    public sealed class FlagdProvider : FeatureProvider
    {
        static int EventStreamRetryBaseBackoff = 1;
        private readonly FlagdConfig _config;
        private readonly Service.ServiceClient _client;
        private readonly Metadata _providerMetadata = new Metadata("flagd Provider");

        private readonly Resolver _resolver;

        private readonly ICache<string, object> _cache;
        private int _eventStreamRetries;
        private int _eventStreamRetryBackoff = EventStreamRetryBaseBackoff;

        private readonly System.Threading.Mutex _mtx;

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

            // TODO set to in process resolver if set appropriately
            _resolver = new RpcResolver(config);
        }

        // just for testing, internal but visible in tests
        internal FlagdProvider(Service.ServiceClient client, FlagdConfig config, ICache<string, object> cache = null)
        {
            _resolver = new RpcResolver(client, config, cache);
        }

        // just for testing, internal but visible in tests
        internal FlagdConfig GetConfig() => _config;

        /// <summary>
        /// Get the provider name.
        /// </summary>
        public static string GetProviderName()
        {
            return Api.Instance.GetProviderMetadata().Name;
        }

        /// <summary>
        ///     Return the metadata associated to this provider.
        /// </summary>
        public override Metadata GetMetadata() => _providerMetadata;

        /// <summary>
        ///     Return the Grpc client of the provider
        /// </summary>
        public Service.ServiceClient GetClient() => _client;

        /// <summary>
        ///     ResolveBooleanValue resolve the value for a Boolean Flag.
        /// </summary>
        /// <param name="flagKey">Name of the flag</param>
        /// <param name="defaultValue">Default value used in case of error.</param>
        /// <param name="context">Context about the user</param>
        /// <returns>A ResolutionDetails object containing the value of your flag</returns>
        public override async Task<ResolutionDetails<bool>> ResolveBooleanValue(string flagKey, bool defaultValue, EvaluationContext context = null)
        {
            return await this._resolver.ResolveBooleanValue(flagKey, defaultValue, context);
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
            return await this._resolver.ResolveStringValue(flagKey, defaultValue, context);
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
            return await this._resolver.ResolveIntegerValue(flagKey, defaultValue, context);
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
            return await this._resolver.ResolveDoubleValue(flagKey, defaultValue, context);
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
            return await this._resolver.ResolveStructureValue(flagKey, defaultValue, context);
        }
    }
}
