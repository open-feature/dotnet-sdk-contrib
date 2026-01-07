using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Constant;
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
    private readonly List<Hook> _hooks = new List<Hook>();

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
            _resolver = new InProcessResolver(_config, jsonSchemaValidator);
        }
        else
        {
            _resolver = new RpcResolver(config);
        }

        _hooks.Add(new SyncMetadataHook(() => this._enrichedContext));
        this._resolver.ProviderEvent += this.OnProviderEvent;
    }

    // just for testing, internal but visible in tests
    internal FlagdProvider(Resolver.Resolver resolver)
    {
        _resolver = resolver;
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
    public override IImmutableList<Hook> GetProviderHooks()
    {
        return this._hooks.ToImmutableList();
    }

    internal EvaluationContext _enrichedContext = EvaluationContext.Empty;

    private bool _connected;

    internal void OnProviderEvent(object _, FlagdProviderEvent payload)
    {
        switch (payload.EventType)
        {
            case ProviderEventTypes.ProviderConfigurationChanged:
                {
                    this.UpdateEnrichedContext(payload);

                    if (this._connected)
                    {
                        this.EventChannel.Writer.TryWrite(new ProviderEventPayload
                        {
                            Type = ProviderEventTypes.ProviderConfigurationChanged,
                            ProviderName = this._providerMetadata.Name
                        });

                        break;
                    }

                    this.EventChannel.Writer.TryWrite(new ProviderEventPayload
                    {
                        Type = ProviderEventTypes.ProviderReady,
                        ProviderName = this._providerMetadata.Name
                    });

                    this._connected = true;

                    break;
                }

            case ProviderEventTypes.ProviderReady:
                {
                    this.UpdateEnrichedContext(payload);

                    this.EventChannel.Writer.TryWrite(new ProviderEventPayload
                    {
                        Type = ProviderEventTypes.ProviderReady,
                        ProviderName = this._providerMetadata.Name
                    });

                    this._connected = true;

                    break;
                }

            case ProviderEventTypes.ProviderError:
                {
                    this.EventChannel.Writer.TryWrite(new ProviderEventPayload
                    {
                        Type = ProviderEventTypes.ProviderError,
                        ProviderName = this._providerMetadata.Name
                    });

                    break;
                }

            default:
                break;
        }
    }

    private void UpdateEnrichedContext(FlagdProviderEvent payload)
    {
        var context = EvaluationContext.Builder();
        foreach (var item in payload.SyncMetadata.AsDictionary())
        {
            context.Set(item.Key, item.Value);
        }
        this._enrichedContext = context.Build();
    }

    /// <inheritdoc/>
    public override async Task InitializeAsync(EvaluationContext context, CancellationToken cancellationToken = default)
    {
        await _resolver.Init().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        await _resolver.Shutdown().ConfigureAwait(false);

        this._resolver.ProviderEvent -= this.OnProviderEvent;
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
