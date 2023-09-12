using System;

namespace OpenFeature.Contrib.Providers.Flagd

{
    /// <summary>
    ///     FlagdConfig is the configuration object for flagD.
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

        private string _host;
        private string _port;
        private bool _useTLS;
        private string _cert;
        private string _socketPath;
        private bool _cache;
        private int _maxCacheSize;
        private int _maxEventStreamRetries;

        internal FlagdConfig()
        {
            _host = Environment.GetEnvironmentVariable(EnvVarHost) ?? "localhost";
            _port = Environment.GetEnvironmentVariable(EnvVarPort) ?? "8013";
            _useTLS = bool.Parse(Environment.GetEnvironmentVariable(EnvVarTLS) ?? "false");
            _cert = Environment.GetEnvironmentVariable(EnvCertPart) ?? "";
            _socketPath = Environment.GetEnvironmentVariable(EnvVarSocketPath) ?? "";
            var cacheStr = Environment.GetEnvironmentVariable(EnvVarCache) ?? "";

            if (cacheStr.ToUpper().Equals("LRU"))
            {
                _cache = true;
                _maxCacheSize = int.Parse(Environment.GetEnvironmentVariable(EnvVarMaxCacheSize) ?? $"{CacheSizeDefault}");
                _maxEventStreamRetries = int.Parse(Environment.GetEnvironmentVariable(EnvVarMaxEventStreamRetries) ?? "3");
            }
        }
        
        internal FlagdConfig(Uri url)
        {
            _host = url.Host;
            _port = url.Port.ToString();
            _useTLS = url.Scheme.ToLower().Equals("https");
            _cert = Environment.GetEnvironmentVariable(EnvCertPart) ?? "";
            _socketPath = url.Scheme.ToLower().Equals("unix") ? url.GetComponents(UriComponents.AbsoluteUri &~ UriComponents.Scheme, UriFormat.UriEscaped) : "";
            var cacheStr = Environment.GetEnvironmentVariable(EnvVarCache) ?? "";

            if (cacheStr.ToUpper().Equals("LRU"))
            {
                _cache = true;
                _maxCacheSize = int.Parse(Environment.GetEnvironmentVariable(EnvVarMaxCacheSize) ?? $"{CacheSizeDefault}");
                _maxEventStreamRetries = int.Parse(Environment.GetEnvironmentVariable(EnvVarMaxEventStreamRetries) ?? "3");
            }
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
