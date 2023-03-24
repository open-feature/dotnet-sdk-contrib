using System;

namespace OpenFeature.Contrib.Providers.Flagd

{
    internal class FlagdConfig
    {
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

        internal int MaxEventStreamRetries
        {
            get { return _maxEventStreamRetries; }
            set { _maxEventStreamRetries = value; }
        }

        private string _host;
        private string _port;
        private bool _useTLS;
        private string _socketPath;
        private bool _cache;
        private int _maxCacheSize;
        private int _maxEventStreamRetries;

        internal FlagdConfig()
        {
            _host = Environment.GetEnvironmentVariable("FLAGD_HOST") ?? "localhost";
            _port = Environment.GetEnvironmentVariable("FLAGD_PORT") ?? "8013";
            _useTLS = bool.Parse(Environment.GetEnvironmentVariable("FLAGD_TLS") ?? "false");
            _socketPath = Environment.GetEnvironmentVariable("FLAGD_SOCKET_PATH") ?? "";
            var cacheStr = Environment.GetEnvironmentVariable("FLAGD_CACHE") ?? "";

            if (cacheStr.ToUpper().Equals("LRU"))
            {
                _cache = true;
                _maxCacheSize = int.Parse(Environment.GetEnvironmentVariable("FLAGD_MAX_CACHE_SIZE") ?? $"{CacheSizeDefault}");
                _maxEventStreamRetries = int.Parse(Environment.GetEnvironmentVariable("FLAGD_MAX_EVENT_STREAM_RETRIES") ?? "3");
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