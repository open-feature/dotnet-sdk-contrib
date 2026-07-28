using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace OpenFeature.Providers.Flagd;


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
    IN_PROCESS,
    /// <summary>
    ///     This is the file-based resolving type, where flags are loaded from a local JSON file
    ///     and evaluated in-process without creating any gRPC streams.
    ///     Evaluations are preformed in-process.
    /// </summary>
    FILE
}

/// <summary>
///     FlagdConfig is the configuration object for flagd.
/// </summary>
public class FlagdConfig
{
    internal const string EnvVarHost = "FLAGD_HOST";
    internal const string EnvVarPort = "FLAGD_PORT";
    internal const string EnvVarSyncPort = "FLAGD_SYNC_PORT";
    internal const string EnvVarTLS = "FLAGD_TLS";
    internal const string EnvCertPart = "FLAGD_SERVER_CERT_PATH";
    internal const string EnvVarSocketPath = "FLAGD_SOCKET_PATH";
    internal const string EnvVarCache = "FLAGD_CACHE";
    internal const string EnvVarMaxCacheSize = "FLAGD_MAX_CACHE_SIZE";
    internal const string EnvVarMaxEventStreamRetries = "FLAGD_MAX_EVENT_STREAM_RETRIES";
    internal const string EnvVarResolverType = "FLAGD_RESOLVER";
    internal const string EnvVarSourceSelector = "FLAGD_SOURCE_SELECTOR";
    internal const string EnvVarOfflineFlagSourcePath = "FLAGD_OFFLINE_FLAG_SOURCE_PATH";
    internal const string EnvVarHashFileChange = "FLAGD_HASH_FILE_CHANGE";
    internal const string EnvVarOfflinePollMs = "FLAGD_OFFLINE_POLL_MS";
    internal const string EnvVarDeadlineMs = "FLAGD_DEADLINE_MS";
    internal const string EnvVarRetryBackoffMs = "FLAGD_RETRY_BACKOFF_MS";
    internal const string EnvVarRetryBackoffMaxMs = "FLAGD_RETRY_BACKOFF_MAX_MS";
    internal const string FlagdSelectorHeaderName = "flagd-selector";
    internal static int CacheSizeDefault = 10;
    internal static int RetryBackoffMsDefault = 1000;
    internal static int RetryBackoffMaxMsDefault = 12000;
    internal static string InProcessResolverValue = "in-process";
    internal static string RpcResolverValue = "rpc";
    internal static string FileResolverValue = "file";
    internal static string LruCacheValue = "lru";

    /// <summary>
    /// Get a FlagdConfigBuilder instance.
    /// </summary>
    /// <returns>A new FlagdConfigBuilder.</returns>
    public static FlagdConfigBuilder Builder()
    {
        return new FlagdConfigBuilder();
    }

    internal static FlagdConfigBuilder Builder(Uri uri)
    {
        if (uri == null)
        {
            throw new ArgumentNullException(nameof(uri));
        }

        return new FlagdConfigBuilder()
            .WithHost(uri.Host)
            .WithPort(uri.Port)
            .WithTls(string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
            .WithSocketPath(string.Equals(uri.Scheme, "unix", StringComparison.OrdinalIgnoreCase) ? uri.GetComponents(UriComponents.AbsoluteUri & ~UriComponents.Scheme, UriFormat.UriEscaped) : string.Empty);
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
    ///     Selects which flag set the provider sees from the flagd source.
    ///     Supported by both the in-process and RPC resolvers.
    /// </summary>
    public string SourceSelector
    {
        get => _sourceSelector;
        set => _sourceSelector = value;
    }

    /// <summary>
    ///     Logger for the provider. When not specified <see cref="NullLogger.Instance"/> is used.
    /// </summary>
    public ILogger Logger
    {
        get => _logger;
        set => _logger = value;
    }

    /// <summary>
    ///     Path to the flag definition JSON file. Used when ResolverType is FILE.
    /// </summary>
    public string OfflineFlagSourcePath
    {
        get => _offlineFlagSourcePath;
        set => _offlineFlagSourcePath = value;
    }

    /// <summary>
    ///     When true, the file watcher uses content hashing (MurmurHash) to detect changes.
    ///     When false (the default), the file watcher polls the file's modification time and size.
    ///     Modification-time polling is reliable in most environments; content hashing is an opt-in
    ///     for file systems that do not update modification times reliably, at a higher I/O cost.
    ///     Defaults to false. Used when ResolverType is FILE.
    /// </summary>
    public bool UseHashFileChangeDetection
    {
        get => _useHashFileChangeDetection;
        set => _useHashFileChangeDetection = value;
    }

    /// <summary>
    ///     The interval, in milliseconds, at which the file watcher polls the flag file for changes.
    ///     Applies to both the modification-time watcher (default) and the hash-based watcher.
    ///     When not set, a default of 5 seconds is used. Used when ResolverType is FILE.
    /// </summary>
    public int? OfflinePollIntervalMs
    {
        get => _offlinePollIntervalMs;
        set => _offlinePollIntervalMs = value;
    }

    /// <summary>
    ///     The maximum time, in milliseconds, to wait for the flag file to become available during
    ///     initialization before timing out. When not set, a default of 5 minutes is used.
    ///     Used when ResolverType is FILE.
    /// </summary>
    public int? DeadlineMs
    {
        get => _deadlineMs;
        set => _deadlineMs = value;
    }

    /// <summary>
    ///     The initial backoff time, in milliseconds, for stream reconnection attempts.
    ///     When not set, a default of 1000ms is used.
    ///     Used when ResolverType is RPC or IN_PROCESS.
    /// </summary>
    public int? RetryBackoffMs
    {
        get => _retryBackoffMs ?? RetryBackoffMsDefault;
        set => _retryBackoffMs = value;
    }

    /// <summary>
    ///     The maximum backoff time, in milliseconds, for stream reconnection attempts.
    ///     When not set, a default of 12000ms (12 seconds) is used.
    ///     Used when ResolverType is RPC or IN_PROCESS.
    /// </summary>
    public int? RetryBackoffMaxMs
    {
        get => _retryBackoffMaxMs ?? RetryBackoffMaxMsDefault;
        set => _retryBackoffMaxMs = value;
    }

    internal bool UseCertificate => _cert.Length > 0;

    private string _host;
    private int _port = 0;
    private bool _useTLS;
    private string _cert;
    private string _socketPath;
    private bool _cache;
    private int _maxCacheSize;
    private int _maxEventStreamRetries;
    private string _sourceSelector;
    private ILogger _logger;
    private ResolverType _resolverType;
    private string _offlineFlagSourcePath;
    private bool _useHashFileChangeDetection;
    private int? _offlinePollIntervalMs;
    private int? _deadlineMs;
    private int? _retryBackoffMs;
    private int? _retryBackoffMaxMs;

    internal FlagdConfig()
    {
        _host = string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EnvVarHost)) ? "localhost" : Environment.GetEnvironmentVariable(EnvVarHost);
        _useTLS = bool.TryParse(Environment.GetEnvironmentVariable(EnvVarTLS), out var useTLS) ? useTLS : false;
        _cert = Environment.GetEnvironmentVariable(EnvCertPart) ?? "";
        _socketPath = Environment.GetEnvironmentVariable(EnvVarSocketPath) ?? "";
        _sourceSelector = Environment.GetEnvironmentVariable(EnvVarSourceSelector) ?? "";
        _logger = NullLogger.Instance;
        var cacheStr = Environment.GetEnvironmentVariable(EnvVarCache) ?? "";

        if (string.Equals(cacheStr, LruCacheValue, StringComparison.OrdinalIgnoreCase))
        {
            _cache = true;
            _maxCacheSize = int.TryParse(Environment.GetEnvironmentVariable(EnvVarMaxCacheSize), out var maxCacheSize) ? maxCacheSize : CacheSizeDefault;
            _maxEventStreamRetries = int.TryParse(Environment.GetEnvironmentVariable(EnvVarMaxEventStreamRetries), out var maxEventStreamRetries) ? maxEventStreamRetries : 3;
        }

        _resolverType = GetResolverTypeFromEnvironment();
        _offlineFlagSourcePath = GetOfflineFlagSourcePathFromEnvironment();
        _useHashFileChangeDetection = GetUseHashFileChangeDetectionFromEnvironment();
        _offlinePollIntervalMs = GetMillisecondsFromEnvironment(EnvVarOfflinePollMs);
        _deadlineMs = GetMillisecondsFromEnvironment(EnvVarDeadlineMs);
        _retryBackoffMs = GetMillisecondsFromEnvironment(EnvVarRetryBackoffMs);
        _retryBackoffMaxMs = GetMillisecondsFromEnvironment(EnvVarRetryBackoffMaxMs);
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

    private static ResolverType GetResolverTypeFromEnvironment()
    {
        var resolverTypeStr = Environment.GetEnvironmentVariable(EnvVarResolverType);

        if (string.IsNullOrWhiteSpace(resolverTypeStr))
        {
            return ResolverType.RPC;
        }

        if (string.Equals(resolverTypeStr, InProcessResolverValue, StringComparison.OrdinalIgnoreCase))
            return ResolverType.IN_PROCESS;
        if (string.Equals(resolverTypeStr, FileResolverValue, StringComparison.OrdinalIgnoreCase))
            return ResolverType.FILE;

        return ResolverType.RPC;
    }

    private static string GetOfflineFlagSourcePathFromEnvironment()
    {
        var offlineFlagSourcePathStr = Environment.GetEnvironmentVariable(EnvVarOfflineFlagSourcePath);
        return string.IsNullOrWhiteSpace(offlineFlagSourcePathStr) ? string.Empty : offlineFlagSourcePathStr;
    }

    private static bool GetUseHashFileChangeDetectionFromEnvironment()
    {
        var value = Environment.GetEnvironmentVariable(EnvVarHashFileChange);
        return !string.IsNullOrEmpty(value) && bool.TryParse(value, out var parsed) && parsed;
    }

    private static int? GetMillisecondsFromEnvironment(string envVarName)
    {
        var value = Environment.GetEnvironmentVariable(envVarName);
        if (!string.IsNullOrWhiteSpace(value)
            && int.TryParse(value, out var milliseconds)
            && milliseconds > 0)
        {
            return milliseconds;
        }

        return null;
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
    ///     Selects which flag set the provider sees from the flagd source.
    ///     Supported by both the in-process and RPC resolvers.
    /// </summary>
    public FlagdConfigBuilder WithSourceSelector(string sourceSelector)
    {
        _config.SourceSelector = sourceSelector;
        return this;
    }

    /// <summary>
    ///     Path to the flag definition JSON file for file-based in-memory resolution.
    /// </summary>
    public FlagdConfigBuilder WithOfflineFlagSourcePath(string offlineFlagSourcePath)
    {
        _config.OfflineFlagSourcePath = offlineFlagSourcePath;
        return this;
    }

    /// <summary>
    ///     Enable or disable content hashing for file change detection.
    ///     When true, the file watcher uses content hashing (MurmurHash) to detect changes.
    ///     When false (the default), the file watcher polls the file's modification time and size.
    /// </summary>
    public FlagdConfigBuilder WithUseHashFileChangeDetection(bool useHash)
    {
        _config.UseHashFileChangeDetection = useHash;
        return this;
    }

    /// <summary>
    ///     The interval, in milliseconds, at which the file watcher polls the flag file for changes.
    ///     Applies to both the modification-time watcher (default) and the hash-based watcher.
    ///     Defaults to 5 seconds when not set.
    /// </summary>
    public FlagdConfigBuilder WithOfflinePollIntervalMs(int offlinePollIntervalMs)
    {
        _config.OfflinePollIntervalMs = offlinePollIntervalMs;
        return this;
    }

    /// <summary>
    ///     The maximum time, in milliseconds, to wait for the flag file to become available during
    ///     initialization before timing out. Defaults to 5 minutes when not set.
    /// </summary>
    public FlagdConfigBuilder WithDeadlineMs(int deadlineMs)
    {
        _config.DeadlineMs = deadlineMs;
        return this;
    }

    /// <summary>
    ///     The initial backoff time, in milliseconds, for stream reconnection attempts.
    ///     Defaults to 1000ms when not set.
    /// </summary>
    public FlagdConfigBuilder WithRetryBackoffMs(int retryBackoffMs)
    {
        _config.RetryBackoffMs = retryBackoffMs;
        return this;
    }

    /// <summary>
    ///     The maximum backoff time, in milliseconds, for stream reconnection attempts.
    ///     Defaults to 12000ms (12 seconds) when not set.
    /// </summary>
    public FlagdConfigBuilder WithRetryBackoffMaxMs(int retryBackoffMaxMs)
    {
        _config.RetryBackoffMaxMs = retryBackoffMaxMs;
        return this;
    }

    /// <summary>
    ///     Provide a <see cref="ILogger"/> to be used by the Flagd provider.
    /// </summary>
    /// <param name="logger"></param>
    /// <returns></returns>
    public FlagdConfigBuilder WithLogger(ILogger logger)
    {
        _config.Logger = logger;
        return this;
    }

    /// <summary>
    ///     Builds the FlagdConfig object.
    /// </summary>
    public FlagdConfig Build()
    {
        this.PreBuild();

        return this._config;
    }

    private void PreBuild()
    {
        if (this._config.ResolverType == ResolverType.FILE)
        {
            return;
        }

        if (this._config.Port == 0)
        {
            var defaultPort = this._config.ResolverType switch
            {
                ResolverType.RPC => 8013,
                ResolverType.IN_PROCESS => 8015,
                _ => throw new NotImplementedException($"No default port defined for resolver type '{this._config.ResolverType}'.")
            };

            var fromPortEnv = TryGetEnvironmentVariableOrDefault(FlagdConfig.EnvVarPort, defaultPort);

            this._config.Port = this._config.ResolverType == ResolverType.IN_PROCESS ?
                TryGetEnvironmentVariableOrDefault(FlagdConfig.EnvVarSyncPort, fromPortEnv) :
                fromPortEnv;
        }
    }

    private static int TryGetEnvironmentVariableOrDefault(string environmentVariable, int defaultPort)
    {
        if (int.TryParse(Environment.GetEnvironmentVariable(environmentVariable), out var p))
        {
            // Validate port is within valid TCP port range (1-65535)
            return p >= 1 && p <= 65535 ? p : defaultPort;
        }
        return defaultPort;
    }
}
