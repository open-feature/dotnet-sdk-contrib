using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
#if NET462_OR_GREATER
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
#endif
using Grpc.Net.Client;
#if NET8_0_OR_GREATER
using System.Security.Cryptography.X509Certificates;
using System.Net.Sockets; // needed for unix sockets
#endif
using Grpc.Core;
using OpenFeature.Constant;
using OpenFeature.Contrib.Providers.Flagd.Utils;
using OpenFeature.Flagd.Grpc.Sync;
using OpenFeature.Model;
using Value = OpenFeature.Model.Value;

namespace OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess;

internal class InProcessResolver : Resolver
{
    static readonly int InitialEventStreamRetryBaseBackoff = 1;
    static readonly int MaxEventStreamRetryBackoff = 60;
    readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private readonly FlagSyncService.FlagSyncServiceClient _client;
    private readonly JsonEvaluator _evaluator;
    private int _eventStreamRetryBackoff = InitialEventStreamRetryBaseBackoff;
    private readonly FlagdConfig _config;
    private GrpcChannel _channel;
    private readonly IJsonSchemaValidator _jsonSchemaValidator;

    public event EventHandler<FlagdProviderEvent> ProviderEvent;

    internal InProcessResolver(FlagdConfig config, IJsonSchemaValidator jsonSchemaValidator)
    {
        this._jsonSchemaValidator = jsonSchemaValidator;
        this._config = config;
        this._client = this.BuildClient(config, channel => new FlagSyncService.FlagSyncServiceClient(channel));
        this._evaluator = new JsonEvaluator(config.SourceSelector, jsonSchemaValidator);
    }

    internal InProcessResolver(
        FlagSyncService.FlagSyncServiceClient client,
        FlagdConfig config,
        IJsonSchemaValidator jsonSchemaValidator)
            : this(config, jsonSchemaValidator)
    {
        this._client = client;
    }

    public async Task Init()
    {
        await _jsonSchemaValidator.InitializeAsync().ConfigureAwait(false);

        var latch = new CountdownEvent(1);
        var handleEventsThread = new Thread(async () => await HandleEvents(latch).ConfigureAwait(false))
        {
            IsBackground = true
        };
        handleEventsThread.Start();
        await Task.Run(() => latch.Wait()).ConfigureAwait(false);
    }

    public async Task Shutdown()
    {
        _cancellationTokenSource.Cancel();
        try
        {
            if (_channel != null)
            {
                await _channel.ShutdownAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            _channel?.Dispose();
        }
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

    private async Task HandleEvents(CountdownEvent latch)
    {
        CancellationToken token = _cancellationTokenSource.Token;
        while (!token.IsCancellationRequested)
        {
            var call = _client.SyncFlags(new SyncFlagsRequest
            {
                Selector = _config.SourceSelector
            });
            try
            {
                // Read the response stream asynchronously
                while (!token.IsCancellationRequested && await call.ResponseStream.MoveNext(token).ConfigureAwait(false))
                {
                    var response = call.ResponseStream.Current;
                    this._evaluator.Sync(FlagConfigurationUpdateType.ALL, response.FlagConfiguration);

                    if (!latch.IsSet)
                    {
                        latch.Signal();
                    }

                    // Reset delay backoff on successful response
                    this._eventStreamRetryBackoff = InitialEventStreamRetryBaseBackoff;

                    var metadata = Structure.Builder();
                    if (response.SyncContext != null)
                    {
                        foreach (var item in response.SyncContext.Fields)
                        {
                            metadata.Set(item.Key, ExtractValue(item.Value));
                        }
                    }

                    var flagdEvent = new FlagdProviderEvent(ProviderEventTypes.ProviderConfigurationChanged, new List<string>(this._evaluator.Flags.Keys), metadata.Build());
                    ProviderEvent?.Invoke(this, flagdEvent);
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
            {
                // do nothing, we've been shutdown
            }
            catch (RpcException)
            {
                // Signal latch on error so Init() completes - provider is "ready" but in error state
                if (!latch.IsSet)
                {
                    latch.Signal();
                }

                var flagdEvent = new FlagdProviderEvent(ProviderEventTypes.ProviderError, new List<string>(), Structure.Empty);
                ProviderEvent?.Invoke(this, flagdEvent);

                // Handle the dropped connection by reconnecting and retrying the stream
                this._eventStreamRetryBackoff = Math.Min(this._eventStreamRetryBackoff * 2, MaxEventStreamRetryBackoff);
                await Task.Delay(this._eventStreamRetryBackoff * 1000).ConfigureAwait(false);
            }
        }
    }

    private static Value ExtractValue(Google.Protobuf.WellKnownTypes.Value value)
    {
        switch (value.KindCase)
        {
            case Google.Protobuf.WellKnownTypes.Value.KindOneofCase.BoolValue:
                return new Value(value.BoolValue);
            case Google.Protobuf.WellKnownTypes.Value.KindOneofCase.NumberValue:
                return new Value(value.NumberValue);
            case Google.Protobuf.WellKnownTypes.Value.KindOneofCase.StringValue:
                return new Value(value.StringValue);
            case Google.Protobuf.WellKnownTypes.Value.KindOneofCase.StructValue:
                {
                    var val = Structure.Builder();
                    foreach (var item in value.StructValue.Fields)
                    {
                        val.Set(item.Key, ExtractValue(item.Value));
                    }
                    return new Value(val.Build());
                }
            case Google.Protobuf.WellKnownTypes.Value.KindOneofCase.ListValue:
                return new Value(value.ListValue.Values.Select(ExtractValue));
            case Google.Protobuf.WellKnownTypes.Value.KindOneofCase.NullValue:
            case Google.Protobuf.WellKnownTypes.Value.KindOneofCase.None:
                break;
        }

        return new Value();
    }

    private T BuildClient<T>(FlagdConfig config, Func<GrpcChannel, T> constructorFunc)
    {
        var useUnixSocket = config.GetUri().ToString().StartsWith("unix://");

        if (!useUnixSocket)
        {
#if NET462_OR_GREATER
            var handler = new WinHttpHandler();
#else
            var handler = new HttpClientHandler();
#endif
            if (config.UseCertificate)
            {
                if (File.Exists(config.CertificatePath))
                {
                    var certificate = CertificateLoader.LoadCertificate(config.CertificatePath);

#if NET8_0_OR_GREATER
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, _) =>
                    {
                        // the the custom cert to the chain, Build returns a bool if valid.
                        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                        chain.ChainPolicy.CustomTrustStore.Add(certificate);
                        return chain.Build(cert);
                    };
#elif NET462_OR_GREATER
                    handler.ServerCertificateValidationCallback = (message, cert, chain, errors) =>
                    {
                        if (errors == SslPolicyErrors.None) { return true; }

                        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;

                        chain.ChainPolicy.ExtraStore.Add(certificate);

                        var isChainValid = chain.Build(cert);

                        if (!isChainValid) { return false; }

                        var isValid = chain.ChainElements
                            .Cast<X509ChainElement>()
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
            return constructorFunc(_channel);

        }

#if NET5_0_OR_GREATER
        var udsEndPoint = new UnixDomainSocketEndPoint(config.GetUri().ToString().Substring("unix://".Length));
        var connectionFactory = new UnixDomainSocketConnectionFactory(udsEndPoint);
        var socketsHttpHandler = new SocketsHttpHandler
        {
            ConnectCallback = connectionFactory.ConnectAsync
        };

        // point to localhost and let the custom ConnectCallback handle the communication over the unix socket
        // see https://learn.microsoft.com/en-us/aspnet/core/grpc/interprocess-uds?view=aspnetcore-7.0 for more details
        _channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpHandler = socketsHttpHandler,
        });
        return constructorFunc(_channel);
#endif
        // unix socket support is not available in this dotnet version
        throw new Exception("unix sockets are not supported in this version.");
    }

    private FlagSyncService.FlagSyncServiceClient BuildClientForPlatform(FlagdConfig config)
    {
        var useUnixSocket = config.GetUri().ToString().StartsWith("unix://");

        if (!useUnixSocket)
        {
#if NET462_OR_GREATER
            var handler = new WinHttpHandler();
#else
            var handler = new HttpClientHandler();
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
                        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                        chain.ChainPolicy.CustomTrustStore.Add(certificate);
                        return chain.Build(cert);
                    };
#elif NET462_OR_GREATER
                    handler.ServerCertificateValidationCallback = (message, cert, chain, errors) =>
                    {
                        if (errors == SslPolicyErrors.None) { return true; }

                        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;

                        chain.ChainPolicy.ExtraStore.Add(certificate);

                        var isChainValid = chain.Build(cert);

                        if (!isChainValid) { return false; }

                        var isValid = chain.ChainElements
                            .Cast<X509ChainElement>()
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
            return new FlagSyncService.FlagSyncServiceClient(GrpcChannel.ForAddress(config.GetUri(), new GrpcChannelOptions
            {
                HttpHandler = handler
            }));
        }

#if NET5_0_OR_GREATER
        var udsEndPoint = new UnixDomainSocketEndPoint(config.GetUri().ToString().Substring("unix://".Length));
        var connectionFactory = new UnixDomainSocketConnectionFactory(udsEndPoint);
        var socketsHttpHandler = new SocketsHttpHandler
        {
            ConnectCallback = connectionFactory.ConnectAsync
        };

        // point to localhost and let the custom ConnectCallback handle the communication over the unix socket
        // see https://learn.microsoft.com/en-us/aspnet/core/grpc/interprocess-uds?view=aspnetcore-7.0 for more details
        return new FlagSyncService.FlagSyncServiceClient(GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpHandler = socketsHttpHandler,
        }));
#endif
        // unix socket support is not available in this dotnet version
        throw new Exception("unix sockets are not supported in this version.");
    }

}
