using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using OpenFeature.Constant;
using OpenFeature.Contrib.Providers.Flagd.Utils;
using OpenFeature.Error;
using OpenFeature.Flagd.Grpc.Evaluation;
using OpenFeature.Model;
using ProtoValue = Google.Protobuf.WellKnownTypes.Value;
using Value = OpenFeature.Model.Value;

namespace OpenFeature.Contrib.Providers.Flagd.Resolver.Rpc;

internal class RpcResolver : Resolver
{
    private const string FLAGS_RPC_FIELD_NAME = "flags";

    static int EventStreamRetryBaseBackoff = 1;
    readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private readonly FlagdConfig _config;
    private readonly ICache<string, object> _cache;
    private readonly Service.ServiceClient _client;
    private int _eventStreamRetries;
    private int _eventStreamRetryBackoff = EventStreamRetryBaseBackoff;
    private GrpcChannel _channel;

    public event EventHandler<FlagdProviderEvent> ProviderEvent;

    internal RpcResolver(FlagdConfig config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        this._config = config;
        this._client = this.BuildClientForPlatform(_config);

        if (this._config.CacheEnabled)
        {
            this._cache = new LRUCache<string, object>(this._config.MaxCacheSize);
        }
    }

    internal RpcResolver(
        Service.ServiceClient client,
        FlagdConfig config,
        ICache<string, object> cache)
        : this(config)
    {
        this._client = client;
        this._cache = cache;
    }

    public Task Init()
    {
        _ = Task.Run(this.HandleEvents);
        return Task.CompletedTask;
    }

    public async Task Shutdown()
    {
        _cancellationTokenSource.Cancel();
        try
        {
            await _channel.ShutdownAsync().ConfigureAwait(false);
        }
        finally
        {
            _channel?.Dispose();
        }
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
                value: resolveBooleanResponse.Value,
                reason: resolveBooleanResponse.Reason,
                variant: resolveBooleanResponse.Variant,
                flagMetadata: BuildFlagMetadata(resolveBooleanResponse.Metadata)
            );
        }, context).ConfigureAwait(false);
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
                variant: resolveStringResponse.Variant,
                flagMetadata: BuildFlagMetadata(resolveStringResponse.Metadata)
            );
        }, context).ConfigureAwait(false);
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
                variant: resolveIntResponse.Variant,
                flagMetadata: BuildFlagMetadata(resolveIntResponse.Metadata)
            );
        }, context).ConfigureAwait(false);
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
                variant: resolveDoubleResponse.Variant,
                flagMetadata: BuildFlagMetadata(resolveDoubleResponse.Metadata)
            );
        }, context).ConfigureAwait(false);
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
                variant: resolveObjectResponse.Variant,
                flagMetadata: BuildFlagMetadata(resolveObjectResponse.Metadata)
            );
        }, context).ConfigureAwait(false);
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
            var result = await resolveDelegate.Invoke(ConvertToContext(context)).ConfigureAwait(false);

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

    private async Task HandleEvents()
    {
        CancellationToken token = _cancellationTokenSource.Token;
        while (!token.IsCancellationRequested && _eventStreamRetries < _config.MaxEventStreamRetries)
        {
            var call = _client.EventStream(new EventStreamRequest());
            try
            {
                // Read the response stream asynchronously
                while (!token.IsCancellationRequested && call != null && await call.ResponseStream.MoveNext().ConfigureAwait(false))
                {
                    var response = call.ResponseStream.Current;

                    var flagsChanged = new List<string>();
                    if (response.Data != null && response.Data.Fields.ContainsKey(FLAGS_RPC_FIELD_NAME))
                    {
                        var flagsExist = response.Data.Fields.TryGetValue(FLAGS_RPC_FIELD_NAME, out ProtoValue val);
                        if (flagsExist && val.KindCase == ProtoValue.KindOneofCase.StructValue)
                        {
                            foreach (var item in val.StructValue.Fields)
                            {
                                flagsChanged.Add(item.Key);
                            }
                        }
                    }

                    switch (response.Type.ToLower())
                    {
                        case "configuration_change":
                            {
                                this.HandleConfigurationChangedEvent(flagsChanged);
                                break;
                            }

                        case "provider_ready":
                            {
                                this.HandleProviderReadyEvent(flagsChanged);
                                break;
                            }
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
                await this.HandleErrorEvent().ConfigureAwait(false);
            }
        }
    }

    private void HandleConfigurationChangedEvent(List<string> flagsChanged)
    {
        var flagdEvent = new FlagdProviderEvent(ProviderEventTypes.ProviderConfigurationChanged, flagsChanged, Structure.Empty);
        ProviderEvent?.Invoke(this, flagdEvent);

        if (!this._config.CacheEnabled)
        {
            return;
        }

        // if we have a cache, remove the changed flags from the cache
        try
        {
            foreach (var flag in flagsChanged)
            {
                this._cache.Delete(flag);
            }
        }
        catch (Exception)
        {
            this._cache.Purge();
        }
    }

    private void HandleProviderReadyEvent(List<string> flagsChanged)
    {
        _eventStreamRetries = 0;
        _eventStreamRetryBackoff = EventStreamRetryBaseBackoff;

        var flagdEvent = new FlagdProviderEvent(ProviderEventTypes.ProviderReady, flagsChanged, Structure.Empty);
        ProviderEvent?.Invoke(this, flagdEvent);

        if (this._config.CacheEnabled)
        {
            this._cache.Purge();
        }
    }

    private async Task HandleErrorEvent()
    {
        this._eventStreamRetries++;

        if (this._eventStreamRetries > this._config.MaxEventStreamRetries)
        {
            return;
        }

        var flagdEvent = new FlagdProviderEvent(ProviderEventTypes.ProviderError, new List<string>(), Structure.Empty);
        ProviderEvent?.Invoke(this, flagdEvent);

        // Handle the dropped connection by reconnecting and retrying the stream
        this._eventStreamRetryBackoff = this._eventStreamRetryBackoff * 2;
        await Task.Delay(this._eventStreamRetryBackoff * 1000).ConfigureAwait(false);
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
    private FeatureProviderException GetOFException(RpcException e)
    {
        switch (e.Status.StatusCode)
        {
            case StatusCode.NotFound:
                return new FeatureProviderException(ErrorType.FlagNotFound, e.Status.Detail, e);
            case StatusCode.Unavailable:
                return new FeatureProviderException(ErrorType.ProviderNotReady, e.Status.Detail, e);
            case StatusCode.InvalidArgument:
                return new FeatureProviderException(ErrorType.TypeMismatch, e.Status.Detail, e);
            case StatusCode.DataLoss:
                return new FeatureProviderException(ErrorType.ParseError, e.Status.Detail, e);
            default:
                return new FeatureProviderException(ErrorType.General, e.Status.Detail, e);
        }
    }

#nullable enable
    private static ImmutableMetadata? BuildFlagMetadata(Struct? metadata)
    {
        var items = new Dictionary<string, object>();

        foreach (var entry in metadata?.Fields ?? [])
        {
            switch (entry.Value.KindCase)
            {
                case ProtoValue.KindOneofCase.NumberValue:
                    items.Add(entry.Key, entry.Value.NumberValue);
                    break;
                case ProtoValue.KindOneofCase.StringValue:
                    items.Add(entry.Key, entry.Value.StringValue);
                    break;
                case ProtoValue.KindOneofCase.BoolValue:
                    items.Add(entry.Key, entry.Value.BoolValue);
                    break;

                // Unsupported types for metadata
                case ProtoValue.KindOneofCase.None:
                case ProtoValue.KindOneofCase.NullValue:
                case ProtoValue.KindOneofCase.StructValue:
                case ProtoValue.KindOneofCase.ListValue:
                default:
                    break;
            }
        }

        return items.Count > 0 ? new ImmutableMetadata(items) : null;
    }
#nullable disable

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
                    var certificate = CertificateLoader.LoadCertificate(config.CertificatePath);
#if NET5_0_OR_GREATER
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, _) =>
                    {
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
