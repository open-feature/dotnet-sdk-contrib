using System;
using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess;
using OpenFeature.Contrib.Providers.Flagd.Resolver.Rpc;
using OpenFeature.Model;
using Metadata = OpenFeature.Model.Metadata;
using Value = OpenFeature.Model.Value;

namespace OpenFeature.Contrib.Providers.Flagd;

/// <summary>
///     FlagdProvider is the OpenFeature provider for flagD.
/// </summary>
public sealed class FlagdProvider : FeatureProvider
{
    const string ProviderName = "flagd Provider";
    private readonly FlagdConfig _config;
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
    ///     FLAGD_RESOLVER                 - The type of resolver (in-process or rpc) to be used for the provider
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
    ///     FLAGD_RESOLVER            - The type of resolver (in-process or rpc) to be used for the provider
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
            var jsonSchemaValidator = new JsonSchemaValidator(null, _config.Logger);
            _resolver = new InProcessResolver(_config, EventChannel, _providerMetadata, jsonSchemaValidator);
        }
        else
        {
            _resolver = new RpcResolver(config, EventChannel, _providerMetadata);
        }
    }

    // just for testing, internal but visible in tests
    internal FlagdProvider(Resolver.Resolver resolver)
    {
        _resolver = resolver;
        _resolver.Init();
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
    public override async Task InitializeAsync(EvaluationContext context, CancellationToken cancellationToken = default)
    {
        await _resolver.Init().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        await _resolver.Shutdown().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override async Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(string flagKey, bool defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
    {
        return await _resolver.ResolveBooleanValueAsync(flagKey, defaultValue, context).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override async Task<ResolutionDetails<string>> ResolveStringValueAsync(string flagKey, string defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
    {
        return await _resolver.ResolveStringValueAsync(flagKey, defaultValue, context).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override async Task<ResolutionDetails<int>> ResolveIntegerValueAsync(string flagKey, int defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
    {

        return await _resolver.ResolveIntegerValueAsync(flagKey, defaultValue, context).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override async Task<ResolutionDetails<double>> ResolveDoubleValueAsync(string flagKey, double defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
    {
        return await _resolver.ResolveDoubleValueAsync(flagKey, defaultValue, context).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override async Task<ResolutionDetails<Value>> ResolveStructureValueAsync(string flagKey, Value defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
    {
        return await _resolver.ResolveStructureValueAsync(flagKey, defaultValue, context).ConfigureAwait(false);
    }
}
