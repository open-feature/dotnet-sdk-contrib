using Grpc.Core;
using Grpc.Net.Client;
using OpenFeature.Constant;
using OpenFeature.Flagd.Grpc.Sync;
using OpenFeature.Model;
using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets; // needed for unix sockets
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Value = OpenFeature.Model.Value;

namespace OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess
{
    internal class InProcessResolver : Resolver
    {
        static readonly int InitialEventStreamRetryBaseBackoff = 1;
        static readonly int MaxEventStreamRetryBackoff = 60;
        readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly FlagSyncService.FlagSyncServiceClient _client;
        private readonly JsonEvaluator _evaluator;
        private readonly Mutex _mtx;
        private int _eventStreamRetryBackoff = InitialEventStreamRetryBaseBackoff;
        private readonly FlagdConfig _config;
        private Thread _handleEventsThread;
        private GrpcChannel _channel;
        private Channel<object> _eventChannel;
        private Model.Metadata _providerMetadata;
        private readonly IJsonSchemaValidator _jsonSchemaValidator;
        private bool connected = false;

        internal InProcessResolver(FlagdConfig config, Channel<object> eventChannel, Model.Metadata providerMetadata, IJsonSchemaValidator jsonSchemaValidator)
        {
            _eventChannel = eventChannel;
            _providerMetadata = providerMetadata;
            _jsonSchemaValidator = jsonSchemaValidator;
            _config = config;
            _client = BuildClient(config, channel => new FlagSyncService.FlagSyncServiceClient(channel));
            _mtx = new Mutex();
            _evaluator = new JsonEvaluator(config.SourceSelector, jsonSchemaValidator);
        }

        internal InProcessResolver(
            FlagSyncService.FlagSyncServiceClient client,
            FlagdConfig config,
            Channel<object> eventChannel,
            Model.Metadata providerMetadata,
            IJsonSchemaValidator jsonSchemaValidator)
                : this(config, eventChannel, providerMetadata, jsonSchemaValidator)
        {
            _client = client;
        }

        public async Task Init()
        {
            await _jsonSchemaValidator.InitializeAsync().ConfigureAwait(false);

            await Task.Run(() =>
            {
                var latch = new CountdownEvent(1);
                _handleEventsThread = new Thread(async () => await HandleEvents(latch))
                {
                    IsBackground = true
                };
                _handleEventsThread.Start();
                latch.Wait();
            }).ContinueWith((task) =>
            {
                if (task.IsFaulted) throw task.Exception;
            });
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
                    while (!token.IsCancellationRequested && await call.ResponseStream.MoveNext(token))
                    {
                        var response = call.ResponseStream.Current;
                        _evaluator.Sync(FlagConfigurationUpdateType.ALL, response.FlagConfiguration);
                        if (!latch.IsSet)
                        {
                            latch.Signal();
                        }
                        HandleProviderReadyEvent();
                        HandleProviderChangeEvent();
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

        private void HandleProviderReadyEvent()
        {
            _mtx.WaitOne();
            _eventStreamRetryBackoff = InitialEventStreamRetryBaseBackoff;
            if (!connected)
            {
                connected = true;
                _eventChannel.Writer.TryWrite(new ProviderEventPayload { Type = ProviderEventTypes.ProviderReady, ProviderName = _providerMetadata.Name });
            }
            _mtx.ReleaseMutex();
        }

        private async Task HandleErrorEvent()
        {
            _mtx.WaitOne();
            _eventStreamRetryBackoff = Math.Min(_eventStreamRetryBackoff * 2, MaxEventStreamRetryBackoff);
            if (connected)
            {
                connected = false;
                _eventChannel.Writer.TryWrite(new ProviderEventPayload { Type = ProviderEventTypes.ProviderError, ProviderName = _providerMetadata.Name });
            }
            _mtx.ReleaseMutex();
            await Task.Delay(_eventStreamRetryBackoff * 1000);
        }

        private void HandleProviderChangeEvent()
        {
            _mtx.WaitOne();
            if (connected)
            {
                _eventChannel.Writer.TryWrite(new ProviderEventPayload { Type = ProviderEventTypes.ProviderConfigurationChanged, ProviderName = _providerMetadata.Name });
            }
            _mtx.ReleaseMutex();
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
                        X509Certificate2 certificate = new X509Certificate2(config.CertificatePath);
#if NET5_0_OR_GREATER
                        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, _) => {
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
                        X509Certificate2 certificate = new X509Certificate2(config.CertificatePath);
#if NET5_0_OR_GREATER
                        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, _) => {
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
}
