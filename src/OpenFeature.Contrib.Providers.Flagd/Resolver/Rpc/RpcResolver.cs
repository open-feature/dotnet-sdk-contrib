using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using OpenFeature.Error;
using OpenFeature.Flagd.Grpc.Evaluation;
using OpenFeature.Model;
using ProtoValue = Google.Protobuf.WellKnownTypes.Value;
using System.Threading;
using Value = OpenFeature.Model.Value;
using System.Threading.Channels;
using OpenFeature.Constant;

namespace OpenFeature.Contrib.Providers.Flagd.Resolver.Rpc;

internal class RpcResolver : Resolver
{
    static int EventStreamRetryBaseBackoff = 1;
    readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private readonly FlagdConfig _config;
    private readonly ICache<string, object> _cache;
    private readonly Service.ServiceClient _client;
    private readonly Mutex _mtx;
    private int _eventStreamRetries;
    private int _eventStreamRetryBackoff = EventStreamRetryBaseBackoff;
    private GrpcChannel _channel;
    private Channel<object> _eventChannel;
    private Model.Metadata _providerMetadata;
    private Thread _handleEventsThread;

    internal RpcResolver(FlagdConfig config, Channel<object> eventChannel, Model.Metadata providerMetadata)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        _config = config;
        _eventChannel = eventChannel;
        _providerMetadata = providerMetadata;
        _client = BuildClientForPlatform(_config);
        _mtx = new Mutex();

        if (_config.CacheEnabled)
        {
            _cache = new LRUCache<string, object>(_config.MaxCacheSize);
        }
    }

    internal RpcResolver(Service.ServiceClient client, FlagdConfig config, ICache<string, object> cache, Channel<object> eventChannel, Model.Metadata providerMetadata) : this(config, eventChannel, providerMetadata)
    {
        _client = client;
        _cache = cache;
    }

    public Task Init()
    {
        _handleEventsThread = new Thread(HandleEvents);
        _handleEventsThread.Start();
        return Task.CompletedTask; // TODO: an elegant way of testing the connection status before completing this task
    }

    public Task Shutdown()
    {
        _cancellationTokenSource.Cancel();
        return _channel?.ShutdownAsync().ContinueWith((t) =>
        {
            _channel.Dispose();
            if (t.IsFaulted) throw t.Exception;
        });
    }

    public async Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(string flagKey, bool defaultValue, EvaluationContext context = null)
    {
        return await ResolveValue(flagKey, async contextStruct =>
        {
            var resolveBooleanResponse = await _client.ResolveBooleanAsync(new ResolveBooleanRequest
            {
                Context = contextStruct,
                FlagKey = flagKey
            }).ConfigureAwait(false);

            return new ResolutionDetails<bool>(
                flagKey: flagKey,
                value: (bool)resolveBooleanResponse.Value,
                reason: resolveBooleanResponse.Reason,
                variant: resolveBooleanResponse.Variant
                );
        }, context);
    }

    public async Task<ResolutionDetails<string>> ResolveStringValueAsync(string flagKey, string defaultValue, EvaluationContext context = null)
    {
        return await ResolveValue(flagKey, async contextStruct =>
        {
            var resolveStringResponse = await _client.ResolveStringAsync(new ResolveStringRequest
            {
                Context = contextStruct,
                FlagKey = flagKey
            }).ConfigureAwait(false);

            return new ResolutionDetails<string>(
                flagKey: flagKey,
                value: resolveStringResponse.Value,
                reason: resolveStringResponse.Reason,
                variant: resolveStringResponse.Variant
                );
        }, context);
    }

    public async Task<ResolutionDetails<int>> ResolveIntegerValueAsync(string flagKey, int defaultValue, EvaluationContext context = null)
    {
        return await ResolveValue(flagKey, async contextStruct =>
        {
            var resolveIntResponse = await _client.ResolveIntAsync(new ResolveIntRequest
            {
                Context = contextStruct,
                FlagKey = flagKey
            }).ConfigureAwait(false);

            return new ResolutionDetails<int>(
                flagKey: flagKey,
                value: (int)resolveIntResponse.Value,
                reason: resolveIntResponse.Reason,
                variant: resolveIntResponse.Variant
                );
        }, context);
    }

    public async Task<ResolutionDetails<double>> ResolveDoubleValueAsync(string flagKey, double defaultValue, EvaluationContext context = null)
    {
        return await ResolveValue(flagKey, async contextStruct =>
        {
            var resolveDoubleResponse = await _client.ResolveFloatAsync(new ResolveFloatRequest
            {
                Context = contextStruct,
                FlagKey = flagKey
            }).ConfigureAwait(false);

            return new ResolutionDetails<double>(
                flagKey: flagKey,
                value: resolveDoubleResponse.Value,
                reason: resolveDoubleResponse.Reason,
                variant: resolveDoubleResponse.Variant
                );
        }, context);
    }

    public async Task<ResolutionDetails<Value>> ResolveStructureValueAsync(string flagKey, Value defaultValue, EvaluationContext context = null)
    {
        return await ResolveValue(flagKey, async contextStruct =>
        {
            var resolveObjectResponse = await _client.ResolveObjectAsync(new ResolveObjectRequest
            {
                Context = contextStruct,
                FlagKey = flagKey
            }).ConfigureAwait(false);

            return new ResolutionDetails<Value>(
                flagKey: flagKey,
                value: ConvertObjectToValue(resolveObjectResponse.Value),
                reason: resolveObjectResponse.Reason,
                variant: resolveObjectResponse.Variant
                );
        }, context);
    }

    private async Task<ResolutionDetails<T>> ResolveValue<T>(string flagKey, Func<Struct, Task<ResolutionDetails<T>>> resolveDelegate, EvaluationContext context = null)
    {
        try
        {
            if (_config.CacheEnabled)
            {
                var value = _cache.TryGet(flagKey);

                if (value != null)
                {
                    return (ResolutionDetails<T>)value;
                }
            }
            var result = await resolveDelegate.Invoke(ConvertToContext(context));

            if (result.Reason.Equals("STATIC") && _config.CacheEnabled)
            {
                _cache.Add(flagKey, result);
            }

            return result;
        }
        catch (RpcException e)
        {
            throw GetOFException(e);
        }
    }

    private async void HandleEvents()
    {
        CancellationToken token = _cancellationTokenSource.Token;
        while (!token.IsCancellationRequested && _eventStreamRetries < _config.MaxEventStreamRetries)
        {
            var call = _client.EventStream(new EventStreamRequest());
            try
            {
                // Read the response stream asynchronously
                while (!token.IsCancellationRequested && call != null && await call.ResponseStream.MoveNext())
                {
                    var response = call.ResponseStream.Current;

                    switch (response.Type.ToLower())
                    {
                        case "configuration_change":
                            HandleConfigurationChangeEvent(response.Data);
                            break;
                        case "provider_ready":
                            HandleProviderReadyEvent();
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
            {
                // do nothing, we've been shutdown
            }
            catch (RpcException)
            {
                // Handle the dropped connection by reconnecting and retrying the stream
                await HandleErrorEvent();
            }
        }
    }

    private void HandleConfigurationChangeEvent(Struct data)
    {
        _eventChannel.Writer.TryWrite(new ProviderEventPayload { Type = ProviderEventTypes.ProviderConfigurationChanged, ProviderName = _providerMetadata.Name });
        // if we don't have a cache, we don't need to remove anything
        if (!_config.CacheEnabled || !data.Fields.ContainsKey("flags"))
        {
            return;
        }

        try
        {
            if (data.Fields.TryGetValue("flags", out ProtoValue val))
            {
                if (val.KindCase == ProtoValue.KindOneofCase.StructValue)
                {
                    val.StructValue.Fields.ToList().ForEach(flag =>
                    {
                        _cache.Delete(flag.Key);
                    });
                }
                var structVal = val.StructValue;
            }
        }
        catch (Exception)
        {
            if (_config.CacheEnabled)
            {
                // purge the cache if we could not handle the configuration change event
                _cache.Purge();
            }
        }
    }

    private void HandleProviderReadyEvent()
    {
        _mtx.WaitOne();
        _eventStreamRetries = 0;
        _eventStreamRetryBackoff = EventStreamRetryBaseBackoff;
        _eventChannel.Writer.TryWrite(new ProviderEventPayload { Type = ProviderEventTypes.ProviderReady, ProviderName = _providerMetadata.Name });
        _mtx.ReleaseMutex();
        if (_config.CacheEnabled)
        {
            _cache.Purge();
        }
    }

    private async Task HandleErrorEvent()
    {
        _mtx.WaitOne();
        _eventStreamRetries++;

        if (_eventStreamRetries > _config.MaxEventStreamRetries)
        {
            return;
        }
        _eventStreamRetryBackoff = _eventStreamRetryBackoff * 2;
        _eventChannel.Writer.TryWrite(new ProviderEventPayload { Type = ProviderEventTypes.ProviderError, ProviderName = _providerMetadata.Name });
        _mtx.ReleaseMutex();
        await Task.Delay(_eventStreamRetryBackoff * 1000);
    }

    /// <summary>
    ///     ConvertToContext converts the given EvaluationContext to a Struct.
    /// </summary>
    /// <param name="ctx">The evaluation context</param>
    /// <returns>A Struct object containing the evaluation context</returns>
    private static Struct ConvertToContext(EvaluationContext ctx)
    {
        if (ctx == null)
        {
            return new Struct();
        }

        var values = new Struct();
        foreach (var entry in ctx)
        {
            values.Fields.Add(entry.Key, ConvertToProtoValue(entry.Value));
        }

        return values;
    }

    /// <summary>
    ///     ConvertToProtoValue converts the given Value to a ProtoValue.
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>A ProtoValue object representing the given value</returns>
    private static ProtoValue ConvertToProtoValue(Value value)
    {
        if (value.IsList)
        {
            return ProtoValue.ForList(value.AsList.Select(ConvertToProtoValue).ToArray());
        }

        if (value.IsStructure)
        {
            var values = new Struct();

            foreach (var entry in value.AsStructure)
            {
                values.Fields.Add(entry.Key, ConvertToProtoValue(entry.Value));
            }

            return ProtoValue.ForStruct(values);
        }

        if (value.IsBoolean)
        {
            return ProtoValue.ForBool(value.AsBoolean ?? false);
        }

        if (value.IsString)
        {
            return ProtoValue.ForString(value.AsString);
        }

        if (value.IsNumber)
        {
            return ProtoValue.ForNumber(value.AsDouble ?? 0.0);
        }

        return ProtoValue.ForNull();
    }

    /// <summary>
    ///     ConvertObjectToValue converts the given Struct to a Value.
    /// </summary>
    /// <param name="src">The struct</param>
    /// <returns>A Value object representing the given struct</returns>
    private static Value ConvertObjectToValue(Struct src) =>
        new Value(new Structure(src.Fields
            .ToDictionary(entry => entry.Key, entry => ConvertToValue(entry.Value))));

    /// <summary>
    ///     ConvertToValue converts the given ProtoValue to a Value.
    /// </summary>
    /// <param name="src">The value, represented as ProtoValue</param>
    /// <returns>A Value object representing the given value</returns>
    private static Value ConvertToValue(ProtoValue src)
    {
        switch (src.KindCase)
        {
            case ProtoValue.KindOneofCase.ListValue:
                return new Value(src.ListValue.Values.Select(ConvertToValue).ToList());
            case ProtoValue.KindOneofCase.StructValue:
                return new Value(ConvertObjectToValue(src.StructValue));
            case ProtoValue.KindOneofCase.None:
            case ProtoValue.KindOneofCase.NullValue:
            case ProtoValue.KindOneofCase.NumberValue:
            case ProtoValue.KindOneofCase.StringValue:
            case ProtoValue.KindOneofCase.BoolValue:
            default:
                return ConvertToPrimitiveValue(src);
        }
    }

    /// <summary>
    ///     ConvertToPrimitiveValue converts the given ProtoValue to a Value.
    /// </summary>
    /// <param name="value">The value, represented as ProtoValue</param>
    /// <returns>A Value object representing the given value as a primitive data type</returns>
    private static Value ConvertToPrimitiveValue(ProtoValue value)
    {
        switch (value.KindCase)
        {
            case ProtoValue.KindOneofCase.BoolValue:
                return new Value(value.BoolValue);
            case ProtoValue.KindOneofCase.StringValue:
                return new Value(value.StringValue);
            case ProtoValue.KindOneofCase.NumberValue:
                return new Value(value.NumberValue);
            case ProtoValue.KindOneofCase.NullValue:
            case ProtoValue.KindOneofCase.StructValue:
            case ProtoValue.KindOneofCase.ListValue:
            case ProtoValue.KindOneofCase.None:
            default:
                return new Value();
        }
    }

    /// <summary>
    ///     GetOFException returns a OpenFeature Exception containing an error code to describe the encountered error.
    /// </summary>
    /// <param name="e">The exception thrown by the Grpc client</param>
    /// <returns>A ResolutionDetails object containing the value of your flag</returns>
    private FeatureProviderException GetOFException(Grpc.Core.RpcException e)
    {
        switch (e.Status.StatusCode)
        {
            case Grpc.Core.StatusCode.NotFound:
                return new FeatureProviderException(Constant.ErrorType.FlagNotFound, e.Status.Detail, e);
            case Grpc.Core.StatusCode.Unavailable:
                return new FeatureProviderException(Constant.ErrorType.ProviderNotReady, e.Status.Detail, e);
            case Grpc.Core.StatusCode.InvalidArgument:
                return new FeatureProviderException(Constant.ErrorType.TypeMismatch, e.Status.Detail, e);
            default:
                return new FeatureProviderException(Constant.ErrorType.General, e.Status.Detail, e);
        }
    }

    private Service.ServiceClient BuildClientForPlatform(FlagdConfig config)
    {
        var useUnixSocket = config.GetUri().ToString().StartsWith("unix://");

        if (!useUnixSocket)
        {
#if NET462_OR_GREATER
            var handler = new System.Net.Http.WinHttpHandler();
#else
            var handler = new System.Net.Http.HttpClientHandler();
#endif
            if (config.UseCertificate)
            {
                if (File.Exists(config.CertificatePath))
                {
                    System.Security.Cryptography.X509Certificates.X509Certificate2 certificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(config.CertificatePath);
#if NET5_0_OR_GREATER
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, _) => {
                        // the the custom cert to the chain, Build returns a bool if valid.
                        chain.ChainPolicy.TrustMode = System.Security.Cryptography.X509Certificates.X509ChainTrustMode.CustomRootTrust;
                        chain.ChainPolicy.CustomTrustStore.Add(certificate);
                        return chain.Build(cert);
                    };
#elif NET462_OR_GREATER
                    handler.ServerCertificateValidationCallback = (message, cert, chain, errors) => {
                        if (errors == System.Net.Security.SslPolicyErrors.None) { return true; }

                        chain.ChainPolicy.VerificationFlags = System.Security.Cryptography.X509Certificates.X509VerificationFlags.AllowUnknownCertificateAuthority;

                        chain.ChainPolicy.ExtraStore.Add(certificate);

                        var isChainValid = chain.Build(cert);

                        if (!isChainValid) { return false; }

                        var isValid = chain.ChainElements
                            .Cast<System.Security.Cryptography.X509Certificates.X509ChainElement>()
                            .Any(x => x.Certificate.RawData.SequenceEqual(certificate.GetRawCertData()));

                        return isValid;
                    };
#else
                    throw new ArgumentException("Custom Certificates are not supported on your platform");
#endif
                }
                else
                {
                    throw new ArgumentException("Specified certificate cannot be found.");
                }
            }
            _channel = GrpcChannel.ForAddress(config.GetUri(), new GrpcChannelOptions
            {
                HttpHandler = handler
            });
            return new Service.ServiceClient(_channel);
        }

#if NET5_0_OR_GREATER
        var udsEndPoint = new System.Net.Sockets.UnixDomainSocketEndPoint(config.GetUri().ToString().Substring("unix://".Length));
        var connectionFactory = new UnixDomainSocketConnectionFactory(udsEndPoint);
        var socketsHttpHandler = new System.Net.Http.SocketsHttpHandler
        {
            ConnectCallback = connectionFactory.ConnectAsync
        };

        // point to localhost and let the custom ConnectCallback handle the communication over the unix socket
        // see https://learn.microsoft.com/en-us/aspnet/core/grpc/interprocess-uds?view=aspnetcore-7.0 for more details
        _channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpHandler = socketsHttpHandler,
        });
        return new Service.ServiceClient(_channel);
#endif
        // unix socket support is not available in this dotnet version
        throw new Exception("unix sockets are not supported in this version.");
    }
}
