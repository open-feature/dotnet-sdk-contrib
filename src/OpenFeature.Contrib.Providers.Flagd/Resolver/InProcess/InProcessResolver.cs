using System.Net.Http;
using System.Threading.Tasks;
using OpenFeature.Model;
using OpenFeature.Flagd.Grpc.Sync;
using System;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Grpc.Net.Client;
using System.Net.Sockets;
using System.Threading;
using Grpc.Core;
using Value = OpenFeature.Model.Value;

namespace OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess
{
    internal class InProcessResolver : Resolver
    {
        static readonly int EventStreamRetryBaseBackoff = 1;
        static readonly int MaxEventStreamRetryBackoff = 60;

        static readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly FlagSyncService.FlagSyncServiceClient _client;
        private readonly JsonEvaluator _evaluator;

        private readonly Mutex _mtx;
        private int _eventStreamRetryBackoff = EventStreamRetryBaseBackoff;

        private readonly FlagdConfig _config;
        private Thread _handleEvents;

        internal InProcessResolver(FlagdConfig config)
        {
            _config = config;
            _client = BuildClient(config, channel => new FlagSyncService.FlagSyncServiceClient(channel));
            _mtx = new Mutex();
            _evaluator = new JsonEvaluator(config.SourceSelector);
        }

        internal InProcessResolver(FlagSyncService.FlagSyncServiceClient client, FlagdConfig config) : this(config)
        {
            _client = client;
        }


        public void Init()
        {
            var latch = new CountdownEvent(1);
            _handleEvents = new Thread(() => HandleEvents(latch));
            _handleEvents.Start();
            latch.Wait();
        }

        public void Shutdown()
        {
            _cancellationTokenSource.Cancel();
        }

        public Task<ResolutionDetails<bool>> ResolveBooleanValue(string flagKey, bool defaultValue, EvaluationContext context = null)
        {
            return Task.FromResult(_evaluator.ResolveBooleanValue(flagKey, defaultValue, context));
        }

        public Task<ResolutionDetails<string>> ResolveStringValue(string flagKey, string defaultValue, EvaluationContext context = null)
        {
            return Task.FromResult(_evaluator.ResolveStringValue(flagKey, defaultValue, context));
        }

        public Task<ResolutionDetails<int>> ResolveIntegerValue(string flagKey, int defaultValue, EvaluationContext context = null)
        {
            return Task.FromResult(_evaluator.ResolveIntegerValue(flagKey, defaultValue, context));
        }

        public Task<ResolutionDetails<double>> ResolveDoubleValue(string flagKey, double defaultValue, EvaluationContext context = null)
        {
            return Task.FromResult(_evaluator.ResolveDoubleValue(flagKey, defaultValue, context));
        }

        public Task<ResolutionDetails<Value>> ResolveStructureValue(string flagKey, Value defaultValue, EvaluationContext context = null)
        {
            return Task.FromResult(_evaluator.ResolveStructureValue(flagKey, defaultValue, context));
        }

        private async void HandleEvents(CountdownEvent latch)
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
                    while (await call.ResponseStream.MoveNext(token))
                    {
                        var response = call.ResponseStream.Current;
                        _evaluator.Sync(FlagConfigurationUpdateType.ALL, response.FlagConfiguration);
                        if (!latch.IsSet)
                        {
                            latch.Signal();
                        }
                        HandleProviderReadyEvent();
                    }
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
                {
                    // Handle the dropped connection by reconnecting and retrying the stream
                    await HandleErrorEvent();
                }
            }
        }

        private void HandleProviderReadyEvent()
        {
            _mtx.WaitOne();
            _eventStreamRetryBackoff = EventStreamRetryBaseBackoff;
            _mtx.ReleaseMutex();
        }

        private async Task HandleErrorEvent()
        {
            _mtx.WaitOne();
            _eventStreamRetryBackoff = Math.Min(_eventStreamRetryBackoff * 2, MaxEventStreamRetryBackoff);
            _mtx.ReleaseMutex();
            await Task.Delay(_eventStreamRetryBackoff * 1000);
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
                return constructorFunc(GrpcChannel.ForAddress(config.GetUri(), new GrpcChannelOptions
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
            return constructorFunc(GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
            {
                HttpHandler = socketsHttpHandler,
            }));
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
