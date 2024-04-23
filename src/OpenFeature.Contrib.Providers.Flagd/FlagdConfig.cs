using System;
using System.Numerics;
using JsonLogic.Net;

namespace OpenFeature.Contrib.Providers.Flagd

{
    /// <summary>
    ///     ResolverType represents the flag evaluator type.
    /// </summary>
    public enum ResolverType
    {
        /// <summary>
        ///     This is the default resolver type, which connects to flagd instance with flag evaluation gRPC contract.
        ///     Evaluations are performed remotely.
        /// </summary>
        RPC,
        /// <summary>
        ///     This is the in-process resolving type, where flags are fetched with flag sync gRPC contract and stored
        ///     locally for in-process evaluation.
        ///     Evaluations are preformed in-process.
        /// </summary>
        IN_PROCESS
    }

    /// <summary>
    ///     FlagdConfig is the configuration object for flagd.
    /// </summary>
    public class FlagdConfig
    {
        internal const string EnvVarHost = "FLAGD_HOST";
        internal const string EnvVarPort = "FLAGD_PORT";
        internal const string EnvVarTLS = "FLAGD_TLS";
        internal const string EnvCertPart = "FLAGD_SERVER_CERT_PATH";
        internal const string EnvVarSocketPath = "FLAGD_SOCKET_PATH";
        internal const string EnvVarCache = "FLAGD_CACHE";
        internal const string EnvVarMaxCacheSize = "FLAGD_MAX_CACHE_SIZE";
        internal const string EnvVarMaxEventStreamRetries = "FLAGD_MAX_EVENT_STREAM_RETRIES";
        internal const string EnvVarResolverType = "FLAGD_RESOLVER_TYPE";
        internal const string EnvVarSourceSelector = "FLAGD_SOURCE_SELECTOR";
        internal static int CacheSizeDefault = 10;

        /// <summary>
        /// Get a FlagdConfigBuilder instance.
        /// </summary>
        /// <returns>A new FlagdConfigBuilder.</returns>
        public static FlagdConfigBuilder Builder()
        {
            return new FlagdConfigBuilder();
        }

        /// <summary>
        ///     The host for the provider to connect to.
        /// </summary>
        public string Host
        {
            get => _host;
            set => _host = value;
        }

        /// <summary>
        ///     The port of the host to connect to.
        /// </summary>
        public int Port
        {
            get => _port;
            set => _port = value;
        }

        /// <summary>
        ///     Use TLS for communication between the provider and the host.
        /// </summary>
        public bool UseTls
        {
            get => _useTLS;
            set => _useTLS = value;
        }

        /// <summary>
        ///     Enable/disable the local cache for static flag values.
        /// </summary>
        public bool CacheEnabled
        {
            get => _cache;
            set => _cache = value;
        }

        /// <summary>
        ///     The maximum size of the cache.
        /// </summary>
        public int MaxCacheSize
        {
            get => _maxCacheSize;
            set => _maxCacheSize = value;
        }

        /// <summary>
        ///     Path to the certificate file.
        /// </summary>
        public string CertificatePath
        {
            get => _cert;
            set => _cert = value;
        }

        /// <summary>
        ///     Path to the socket.
        /// </summary>
        public string SocketPath
        {
            get => _socketPath;
            set => _socketPath = value;
        }

        /// <summary>
        ///     Maximum number of times the connection to the event stream should be re-attempted
        ///     0 = infinite
        /// </summary>
        public int MaxEventStreamRetries
        {
            get
            {
                if (_maxEventStreamRetries == 0)
                {
                    return int.MaxValue;
                }
                return _maxEventStreamRetries;
            }
            set => _maxEventStreamRetries = value;
        }

        /// <summary>
        ///     Which type of resolver to use.
        /// </summary>
        public ResolverType ResolverType
        {
            get => _resolverType;
            set => _resolverType = value;
        }

        /// <summary>
        ///     Source selector for the in-process provider.
        /// </summary>
        public string SourceSelector
        {
            get => _sourceSelector;
            set => _sourceSelector = value;
        }

        internal bool UseCertificate => _cert.Length > 0;

        private string _host;
        private int _port;
        private bool _useTLS;
        private string _cert;
        private string _socketPath;
        private bool _cache;
        private int _maxCacheSize;
        private int _maxEventStreamRetries;
        private string _sourceSelector;
        private ResolverType _resolverType;

        internal FlagdConfig()
        {
            _host = Environment.GetEnvironmentVariable(EnvVarHost) ?? "localhost";
            _port = int.TryParse(Environment.GetEnvironmentVariable(EnvVarPort), out var port) ? port : 8013;
            _useTLS = bool.Parse(Environment.GetEnvironmentVariable(EnvVarTLS) ?? "false");
            _cert = Environment.GetEnvironmentVariable(EnvCertPart) ?? "";
            _socketPath = Environment.GetEnvironmentVariable(EnvVarSocketPath) ?? "";
            _sourceSelector = Environment.GetEnvironmentVariable(EnvVarSourceSelector) ?? "";
            var cacheStr = Environment.GetEnvironmentVariable(EnvVarCache) ?? "";

            if (string.Equals(cacheStr, "LRU", StringComparison.OrdinalIgnoreCase))
            {
                _cache = true;
                _maxCacheSize = int.Parse(Environment.GetEnvironmentVariable(EnvVarMaxCacheSize) ?? $"{CacheSizeDefault}");
                _maxEventStreamRetries = int.Parse(Environment.GetEnvironmentVariable(EnvVarMaxEventStreamRetries) ?? "3");
            }

            var resolverTypeStr = Environment.GetEnvironmentVariable(EnvVarResolverType) ?? "RPC";
            _resolverType = resolverTypeStr.ToUpper().Equals("IN_PROCESS") ? ResolverType.IN_PROCESS : ResolverType.RPC;
        }

        internal FlagdConfig(Uri url)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            _host = url.Host;
            _port = url.Port;
            _useTLS = string.Equals(url.Scheme, "https", StringComparison.OrdinalIgnoreCase);
            _cert = Environment.GetEnvironmentVariable(EnvCertPart) ?? "";
            _socketPath = string.Equals(url.Scheme, "unix", StringComparison.OrdinalIgnoreCase) ? url.GetComponents(UriComponents.AbsoluteUri & ~UriComponents.Scheme, UriFormat.UriEscaped) : "";
            _sourceSelector = Environment.GetEnvironmentVariable(EnvVarSourceSelector) ?? "";

            var cacheStr = Environment.GetEnvironmentVariable(EnvVarCache) ?? "";

            if (string.Equals(cacheStr, "LRU", StringComparison.OrdinalIgnoreCase))
            {
                _cache = true;
                _maxCacheSize = int.Parse(Environment.GetEnvironmentVariable(EnvVarMaxCacheSize) ?? $"{CacheSizeDefault}");
                _maxEventStreamRetries = int.Parse(Environment.GetEnvironmentVariable(EnvVarMaxEventStreamRetries) ?? "3");
            }

            var resolverTypeStr = Environment.GetEnvironmentVariable(EnvVarResolverType) ?? "RPC";
            _resolverType = resolverTypeStr.ToUpper().Equals("IN_PROCESS") ? ResolverType.IN_PROCESS : ResolverType.RPC;
        }

        internal Uri GetUri()
        {
            Uri uri;
            if (_socketPath != "")
            {
                uri = new Uri("unix://" + _socketPath);
            }
            else
            {
                var protocol = "http";

                if (_useTLS)
                {
                    protocol = "https";
                }

                uri = new Uri(protocol + "://" + _host + ":" + _port);
            }
            return uri;
        }
    }

    /// <summary>
    ///     FlagdConfigBuilder is used to build a FlagdConfig object.
    /// </summary>
    public class FlagdConfigBuilder
    {
        private FlagdConfig _config = new FlagdConfig();

        /// <summary>
        ///     The host for the provider to connect to.
        /// </summary>
        public FlagdConfigBuilder WithHost(string host)
        {
            _config.Host = host;
            return this;
        }

        /// <summary>
        ///     The Port property of the config.
        /// </summary>
        public FlagdConfigBuilder WithPort(int port)
        {
            _config.Port = port;
            return this;
        }

        /// <summary>
        ///     Use TLS for communication between the provider and the host.
        /// </summary>
        public FlagdConfigBuilder WithTls(bool useTls)
        {
            _config.UseTls = useTls;
            return this;
        }

        /// <summary>
        ///     Path to the certificate file.
        /// </summary>
        public FlagdConfigBuilder WithCertificatePath(string certPath)
        {
            _config.CertificatePath = certPath;
            return this;
        }

        /// <summary>
        ///     Path to the socket.
        /// </summary>
        public FlagdConfigBuilder WithSocketPath(string socketPath)
        {
            _config.SocketPath = socketPath;
            return this;
        }

        /// <summary>
        ///     Enable/disable the local cache for static flag values.
        /// </summary>
        public FlagdConfigBuilder WithCache(bool cacheEnabled)
        {
            _config.CacheEnabled = cacheEnabled;
            return this;
        }

        /// <summary>
        ///     The maximum size of the cache.
        /// </summary>
        public FlagdConfigBuilder WithMaxCacheSize(int maxCacheSize)
        {
            _config.MaxCacheSize = maxCacheSize;
            return this;
        }

        /// <summary>
        ///     Maximum number of times the connection to the event stream should be re-attempted
        /// </summary>
        public FlagdConfigBuilder WithMaxEventStreamRetries(int maxEventStreamRetries)
        {
            _config.MaxEventStreamRetries = maxEventStreamRetries;
            return this;
        }

        /// <summary>
        ///     Which type of resolver to use.
        /// </summary>
        public FlagdConfigBuilder WithResolverType(ResolverType resolverType)
        {
            _config.ResolverType = resolverType;
            return this;
        }

        /// <summary>
        ///     Source selector for the in-process provider.
        /// </summary>
        public FlagdConfigBuilder WithSourceSelector(string sourceSelector)
        {
            _config.SourceSelector = sourceSelector;
            return this;
        }

        /// <summary>
        ///     Builds the FlagdConfig object.
        /// </summary>
        public FlagdConfig Build()
        {
            return _config;
        }
    }
}
