using System;

namespace OpenFeature.Contrib.Providers.Flagd

{
    /// <summary>
    ///     ResolverTpe represents the flag evaluator type.
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
        internal string Host
        {
            get { return _host; }
        }

        internal bool CacheEnabled
        {
            get { return _cache; }
            set { _cache = value; }
        }

        internal int MaxCacheSize
        {
            get { return _maxCacheSize; }
        }

        internal bool UseCertificate
        {
            get { return _cert.Length > 0; }
        }

        internal string CertificatePath
        {
            get { return _cert; }
            set { _cert = value; }
        }

        internal int MaxEventStreamRetries
        {
            get { return _maxEventStreamRetries; }
            set { _maxEventStreamRetries = value; }
        }

        internal ResolverType ResolverType
        {
            get => _resolverType;
            set => _resolverType = value;
        }

        internal string SourceSelector
        {
            get { return _sourceSelector; }
            set { _sourceSelector = value; }
        }

        private string _host;
        private string _port;
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
            _port = Environment.GetEnvironmentVariable(EnvVarPort) ?? "8013";
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
            _port = url.Port.ToString();
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
}
