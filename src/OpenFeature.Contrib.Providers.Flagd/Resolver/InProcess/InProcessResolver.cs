using System.Threading.Tasks;
using OpenFeature.Model;
using OpenFeature.Flagd.Grpc.Sync;
using System;
using System.Threading;
using Value = OpenFeature.Model.Value;
using System.Threading.Channels;
using OpenFeature.Constant;
using OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess.Storage;

namespace OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess;

internal class InProcessResolver : Resolver
{
    readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private readonly Storage.Storage _storage;
    private readonly JsonEvaluator _evaluator;
    private Thread _handleEventsThread;
    private Channel<object> _eventChannel;
    private Model.Metadata _providerMetadata;

    internal InProcessResolver(FlagdConfig config, Channel<object> eventChannel, Model.Metadata providerMetadata)
    {
        _eventChannel = eventChannel;
        _providerMetadata = providerMetadata;
        _evaluator = new JsonEvaluator(config.SourceSelector);
        this._storage = config.ResolverType switch
        {
            ResolverType.FILE => new FileStorage(config),
            _ => new RpcStorage(config),
        };
    }

    internal InProcessResolver(FlagSyncService.FlagSyncServiceClient client, FlagdConfig config, Channel<object> eventChannel, Model.Metadata providerMetadata) : this(config, eventChannel, providerMetadata)
    {
        this._storage = new RpcStorage(client, config);
    }

    public Task Init()
    {
        Task.Run(() =>
        {
            var latch = new CountdownEvent(1);
            _handleEventsThread = new Thread(async () =>
            {
                await _storage.Init().ConfigureAwait(false);
                await HandleStorageEvents(latch).ConfigureAwait(false);
            })
            {
                IsBackground = true
            };
            _handleEventsThread.Start();
            latch.Wait();
        }).ContinueWith(task =>
        {
            if (task.IsFaulted) throw task.Exception;
        });
        return Task.CompletedTask;
    }

    public Task Shutdown()
    {
        _cancellationTokenSource.Cancel();
        return this._storage.Shutdown().ContinueWith(t =>
        {
            if (t.IsFaulted) throw t.Exception;
        });;
    }

    public Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(string flagKey, bool defaultValue, EvaluationContext context = null)
    {
        return Task.FromResult(_evaluator.ResolveBooleanValueAsync(flagKey, defaultValue, context));
    }

    public Task<ResolutionDetails<string>> ResolveStringValueAsync(string flagKey, string defaultValue, EvaluationContext context = null)
    {
        return Task.FromResult(_evaluator.ResolveStringValueAsync(flagKey, defaultValue, context));
    }

    public Task<ResolutionDetails<int>> ResolveIntegerValueAsync(string flagKey, int defaultValue, EvaluationContext context = null)
    {
        return Task.FromResult(_evaluator.ResolveIntegerValueAsync(flagKey, defaultValue, context));
    }

    public Task<ResolutionDetails<double>> ResolveDoubleValueAsync(string flagKey, double defaultValue, EvaluationContext context = null)
    {
        return Task.FromResult(_evaluator.ResolveDoubleValueAsync(flagKey, defaultValue, context));
    }

    public Task<ResolutionDetails<Value>> ResolveStructureValueAsync(string flagKey, Value defaultValue, EvaluationContext context = null)
    {
        return Task.FromResult(_evaluator.ResolveStructureValueAsync(flagKey, defaultValue, context));
    }

    private async Task HandleStorageEvents(CountdownEvent latch)
    {
        CancellationToken token = _cancellationTokenSource.Token;
        while (await this._storage.EventChannel().Reader.WaitToReadAsync(token).ConfigureAwait(false))
        {
            try
            {
                var storageEvent = await this._storage.EventChannel().Reader.ReadAsync().ConfigureAwait(false);
                switch (storageEvent.EventType)
                {
                    case StorageEvent.Type.READY:
                        _evaluator.Sync(FlagConfigurationUpdateType.ALL, storageEvent.FlagConfiguration);
                        if (!latch.IsSet)
                        {
                            _eventChannel.Writer.TryWrite(new ProviderEventPayload
                            {
                                Type = ProviderEventTypes.ProviderReady, ProviderName = _providerMetadata.Name
                            });
                            latch.Signal();
                        }

                        break;
                    case StorageEvent.Type.CHANGED:
                        _evaluator.Sync(FlagConfigurationUpdateType.ALL, storageEvent.FlagConfiguration);
                        _eventChannel.Writer.TryWrite(new ProviderEventPayload
                        {
                            Type = ProviderEventTypes.ProviderConfigurationChanged,
                            ProviderName = _providerMetadata.Name
                        });
                        break;
                    case StorageEvent.Type.ERROR:
                        _eventChannel.Writer.TryWrite(new ProviderEventPayload
                        {
                            Type = ProviderEventTypes.ProviderError, ProviderName = _providerMetadata.Name
                        });
                        break;
                }
            }
            catch (Exception)
            {
                _eventChannel.Writer.TryWrite(new ProviderEventPayload
                {
                    Type = ProviderEventTypes.ProviderError, ProviderName = _providerMetadata.Name
                });
            }
        }
    }
}
