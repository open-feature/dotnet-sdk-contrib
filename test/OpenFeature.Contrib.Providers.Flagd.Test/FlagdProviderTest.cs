using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using OpenFeature.Constant;
using OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess;
using OpenFeature.Contrib.Providers.Flagd.Resolver.Rpc;
using OpenFeature.Error;
using OpenFeature.Flagd.Grpc.Evaluation;
using OpenFeature.Flagd.Grpc.Sync;
using OpenFeature.Model;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit;
using Metadata = Grpc.Core.Metadata;
using ProtoValue = Google.Protobuf.WellKnownTypes.Value;

namespace OpenFeature.Contrib.Providers.Flagd.Test
{
    public class UnitTestFlagdProvider
    {

        private Channel<object> MakeChannel()
        {
            return System.Threading.Channels.Channel.CreateBounded<object>(1);
        }

        private Model.Metadata MakeProviderMetadata()
        {
            return new Model.Metadata("test");
        }

        [Fact]
        public void BuildClientForPlatform_Should_Throw_Exception_When_FlagdCertPath_Not_Exists()
        {
            // Arrange
            Environment.SetEnvironmentVariable(FlagdConfig.EnvCertPart, "non-existing-path");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarHost, "localhost");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarPort, "5001");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new FlagdProvider());

            // Cleanup
            Environment.SetEnvironmentVariable(FlagdConfig.EnvCertPart, "");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarHost, "");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarPort, "");
        }

        [Fact]
        public void BuildClientForPlatform_Should_Return_Client_For_Non_Unix_Socket_Without_Certificate()
        {
            // Arrange
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarHost, "localhost");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarPort, "5001");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarResolverType, "RPC");

            // Act
            var flagdProvider = new FlagdProvider();
            var resolver = flagdProvider.GetResolver();

            // Assert
            Assert.NotNull(resolver);
            Assert.IsType<RpcResolver>(resolver);

            // Cleanup
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarHost, "");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarPort, "");
        }

#if NET462
        [Fact]
        public void BuildClientForPlatform_Should_Throw_Exception_For_Unsupported_DotNet_Version()
        {
            // Arrange
            var url = new Uri("unix:///var/run/flagd.sock");

            // Act & Assert
            Assert.Throws<Exception>(() => new FlagdProvider(url));
        }
#endif
        [Fact]
        public void TestGetProviderName()
        {
            Assert.Equal("flagd Provider", FlagdProvider.GetProviderName());
        }

        [Fact]
        public void TestGetProviderWithDefaultConfig()
        {
            Utils.CleanEnvVars();
            var flagdProvider = new FlagdProvider();

            var resolver = flagdProvider.GetResolver();

            Assert.NotNull(resolver);
            Assert.IsType<RpcResolver>(resolver);
        }

        [Fact]
        public void TestGetProviderWithConfig()
        {
            // Create a config with default values set
            var config = new FlagdConfig();

            // Set env variables (should be ignored by the constructor)
            Environment.SetEnvironmentVariable(FlagdConfig.EnvCertPart, "path");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarHost, "localhost111");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarPort, "5001");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarTLS, "true");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarCache, "LRU");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarMaxCacheSize, "20");

            // Create provider, which ignores the env vars and uses the config
            var flagdProvider = new FlagdProvider(config);

            // Resolver should no be nil
            var resolver = flagdProvider.GetResolver();
            Assert.NotNull(resolver);

            // Retrieve config for assertions
            config = flagdProvider.GetConfig();

            // Assert
            Assert.Equal("", config.CertificatePath);
            Assert.Equal(new Uri("http://localhost:8013"), config.GetUri());
            Assert.False(config.CacheEnabled);
            Assert.Equal(0, config.MaxCacheSize);

            // Cleanup
            Environment.SetEnvironmentVariable(FlagdConfig.EnvCertPart, "");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarHost, "");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarPort, "");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarTLS, "");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarCache, "");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarMaxCacheSize, "");
        }

        [Fact]
        public void TestGetProviderWithUri()
        {
            // Set env variables (should be used by the constructor)
            Environment.SetEnvironmentVariable(FlagdConfig.EnvCertPart, "");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarCache, "LRU");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarMaxCacheSize, "20");

            // Set env variables (should be ignored by the constructor)
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarHost, "localhost111");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarPort, "5001");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarTLS, "false");

            // Create provider, which ignores the env vars and uses the config
            var flagdProvider = new FlagdProvider(new Uri("https://localhost:8013"));

            // Resolver should no be nil
            var resolver = flagdProvider.GetResolver();
            Assert.NotNull(resolver);

            // Retrieve config for assertions
            var config = flagdProvider.GetConfig();

            // Assert
            Assert.Equal("", config.CertificatePath);
            Assert.Equal(new Uri("https://localhost:8013"), config.GetUri());
            Assert.True(config.CacheEnabled);
            Assert.Equal(20, config.MaxCacheSize);

            // Cleanup
            Environment.SetEnvironmentVariable(FlagdConfig.EnvCertPart, "");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarHost, "");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarPort, "");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarTLS, "");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarCache, "");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarMaxCacheSize, "");
        }

        [Fact]
        public async Task TestResolveBooleanValueAsync()
        {
            var resp = new ResolveBooleanResponse
            {
                Value = true
            };

            var grpcResp = new AsyncUnaryCall<ResolveBooleanResponse>(
                Task.FromResult(resp),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { });

            var substituteGrpcClient = Substitute.For<Service.ServiceClient>();
            substituteGrpcClient
                .ResolveBooleanAsync(
                    Arg.Any<ResolveBooleanRequest>(), null, null, CancellationToken.None)
                .Returns(grpcResp);

            var rpcResolver = new RpcResolver(substituteGrpcClient, new FlagdConfig(), null, MakeChannel(), MakeProviderMetadata());
            var flagdProvider = new FlagdProvider(rpcResolver);

            // resolve with default set to false to make sure we return what the grpc server gives us
            var val = await flagdProvider.ResolveBooleanValueAsync("my-key", false);

            Assert.True(val.Value);
        }

        [Fact]
        public async Task TestResolveStringValue()
        {
            var resp = new ResolveStringResponse { Value = "my-value" };

            var grpcResp = new AsyncUnaryCall<ResolveStringResponse>(
                Task.FromResult(resp),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { });

            var subGrpcClient = Substitute.For<Service.ServiceClient>();

            subGrpcClient.ResolveStringAsync(
                    Arg.Any<ResolveStringRequest>(), null, null, CancellationToken.None)
                .Returns(grpcResp);

            var rpcResolver = new RpcResolver(subGrpcClient, new FlagdConfig(), null, MakeChannel(), MakeProviderMetadata());
            var flagdProvider = new FlagdProvider(rpcResolver);

            var val = await flagdProvider.ResolveStringValueAsync("my-key", "");

            Assert.Equal("my-value", val.Value);
        }

        [Fact]
        public async Task TestResolveIntegerValue()
        {
            var resp = new ResolveIntResponse
            {
                Value = 10
            };

            var grpcResp = new AsyncUnaryCall<ResolveIntResponse>(
                Task.FromResult(resp),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { });

            var subGrpcClient = Substitute.For<Service.ServiceClient>();
            subGrpcClient.ResolveIntAsync(Arg.Any<ResolveIntRequest>(), null, null, CancellationToken.None)
                .Returns(grpcResp);

            var rpcResolver = new RpcResolver(subGrpcClient, new FlagdConfig(), null, MakeChannel(), MakeProviderMetadata());
            var flagdProvider = new FlagdProvider(rpcResolver);

            var val = await flagdProvider.ResolveIntegerValueAsync("my-key", 0);

            Assert.Equal(10, val.Value);
        }

        [Fact]
        public async Task TestResolveDoubleValue()
        {
            var resp = new ResolveFloatResponse
            {
                Value = 10.0
            };

            var grpcResp = new AsyncUnaryCall<ResolveFloatResponse>(
                Task.FromResult(resp),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { });

            var mockGrpcClient = Substitute.For<Service.ServiceClient>();
            mockGrpcClient.ResolveFloatAsync(Arg.Any<ResolveFloatRequest>(), null, null, CancellationToken.None)
                .Returns(grpcResp);

            var rpcResolver = new RpcResolver(mockGrpcClient, new FlagdConfig(), null, MakeChannel(), MakeProviderMetadata());
            var flagdProvider = new FlagdProvider(rpcResolver);

            var val = await flagdProvider.ResolveDoubleValueAsync("my-key", 0.0);

            Assert.Equal(10.0, val.Value);
        }

        [Fact]
        public async Task TestResolveStructureValue()
        {
            var resp = new ResolveObjectResponse();

            var returnedValue = new Struct();
            returnedValue.Fields.Add("my-key", ProtoValue.ForString("my-value"));


            resp.Value = returnedValue;

            var grpcResp = new AsyncUnaryCall<ResolveObjectResponse>(
                Task.FromResult(resp),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { });

            var mockGrpcClient = Substitute.For<Service.ServiceClient>();
            mockGrpcClient.ResolveObjectAsync(Arg.Any<ResolveObjectRequest>(), null, null, CancellationToken.None)
                .Returns(grpcResp);

            var rpcResolver = new RpcResolver(mockGrpcClient, new FlagdConfig(), null, MakeChannel(), MakeProviderMetadata());
            var flagdProvider = new FlagdProvider(rpcResolver);

            var val = await flagdProvider.ResolveStructureValueAsync("my-key", null);

            Assert.True(val.Value.AsStructure.ContainsKey("my-key"));
        }

        [Fact]
        public async Task TestResolveFlagNotFound()
        {
            var exc = new RpcException(new Status(StatusCode.NotFound, ErrorType.FlagNotFound.ToString()));

            var grpcResp = new AsyncUnaryCall<ResolveBooleanResponse>(
                Task.FromException<ResolveBooleanResponse>(exc),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { });

            var mockGrpcClient = Substitute.For<Service.ServiceClient>();
            mockGrpcClient.ResolveBooleanAsync(
                    Arg.Any<ResolveBooleanRequest>(), null, null, CancellationToken.None).Returns(grpcResp);

            var rpcResolver = new RpcResolver(mockGrpcClient, new FlagdConfig(), null, MakeChannel(), MakeProviderMetadata());
            var flagdProvider = new FlagdProvider(rpcResolver);

            // make sure the correct exception is thrown
            var ex = await Assert.ThrowsAsync<FeatureProviderException>(async () =>
            {
                await flagdProvider.ResolveBooleanValueAsync("my-key", true);
            });

            Assert.Multiple(() =>
            {
                Assert.Equal(ErrorType.FlagNotFound, ex.ErrorType);
                Assert.Equal(ErrorType.FlagNotFound.ToString(), ex.Message);
            });
        }

        [Fact]
        public async Task TestResolveGrpcHostUnavailable()
        {
            var exc = new RpcException(new Status(StatusCode.Unavailable, ErrorType.ProviderNotReady.ToString()));

            var grpcResp = new AsyncUnaryCall<ResolveBooleanResponse>(
                Task.FromException<ResolveBooleanResponse>(exc),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { });

            var mockGrpcClient = Substitute.For<Service.ServiceClient>();
            mockGrpcClient.ResolveBooleanAsync(
                    Arg.Any<ResolveBooleanRequest>(), null, null, CancellationToken.None)
                .Returns(grpcResp);

            var rpcResolver = new RpcResolver(mockGrpcClient, new FlagdConfig(), null, MakeChannel(), MakeProviderMetadata());
            var flagdProvider = new FlagdProvider(rpcResolver);

            // make sure the correct exception is thrown
            var ex = await Assert.ThrowsAsync<FeatureProviderException>(async () =>
            {
                await flagdProvider.ResolveBooleanValueAsync("my-key", true);
            });

            Assert.Multiple(() =>
            {
                Assert.Equal(ErrorType.ProviderNotReady, ex.ErrorType);
                Assert.Equal(ErrorType.ProviderNotReady.ToString(), ex.Message);
            });
        }

        [Fact]
        public async Task TestResolveTypeMismatch()
        {
            var exc = new RpcException(new Status(StatusCode.InvalidArgument, ErrorType.TypeMismatch.ToString()));

            var grpcResp = new AsyncUnaryCall<ResolveBooleanResponse>(
                Task.FromException<ResolveBooleanResponse>(exc),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { });

            var mockGrpcClient = Substitute.For<Service.ServiceClient>();
            mockGrpcClient.ResolveBooleanAsync(
                    Arg.Any<ResolveBooleanRequest>(), null, null, CancellationToken.None)
                .Returns(grpcResp);

            var rpcResolver = new RpcResolver(mockGrpcClient, new FlagdConfig(), null, MakeChannel(), MakeProviderMetadata());
            var flagdProvider = new FlagdProvider(rpcResolver);

            // make sure the correct exception is thrown
            var ex = await Assert.ThrowsAsync<FeatureProviderException>(() =>
            {
                return flagdProvider.ResolveBooleanValueAsync("my-key", true);
            });

            Assert.Multiple(() =>
            {
                Assert.Equal(ErrorType.TypeMismatch, ex.ErrorType);
                Assert.Equal(ErrorType.TypeMismatch.ToString(), ex.Message);
            });
        }

        [Fact]
        public async Task TestResolveUnknownError()
        {
            var exc = new RpcException(new Status(StatusCode.Internal, ErrorType.General.ToString()));

            var grpcResp = new AsyncUnaryCall<ResolveBooleanResponse>(
                Task.FromException<ResolveBooleanResponse>(exc),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { });

            var mockGrpcClient = Substitute.For<Service.ServiceClient>();
            mockGrpcClient.ResolveBooleanAsync(
                    Arg.Any<ResolveBooleanRequest>(), null, null, CancellationToken.None)
                .Returns(grpcResp);

            var rpcResolver = new RpcResolver(mockGrpcClient, new FlagdConfig(), null, MakeChannel(), MakeProviderMetadata());
            var flagdProvider = new FlagdProvider(rpcResolver);

            // make sure the correct exception is thrown
            var ex = await Assert.ThrowsAsync<FeatureProviderException>(() =>
            {
                return flagdProvider.ResolveBooleanValueAsync("my-key", true);
            });

            Assert.Multiple(() =>
            {
                Assert.Equal(ErrorType.General, ex.ErrorType);
                Assert.Equal(ErrorType.General.ToString(), ex.Message);
            });
        }

        [Fact]
        public async Task TestCacheAsync()
        {
            var resp = new ResolveBooleanResponse();
            resp.Value = true;
            resp.Reason = "STATIC";

            var grpcResp = new AsyncUnaryCall<ResolveBooleanResponse>(
                Task.FromResult(resp),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { });

            var mockGrpcClient = Substitute.For<Service.ServiceClient>();
            mockGrpcClient.ResolveBooleanAsync(
                    Arg.Any<ResolveBooleanRequest>(), null, null, CancellationToken.None)
                .Returns(grpcResp);

            var asyncStreamReader = Substitute.For<IAsyncStreamReader<EventStreamResponse>>();

            var l = new List<EventStreamResponse>
            {
                new EventStreamResponse{
                    Type = "provider_ready"
                }
            };

            var enumerator = l.GetEnumerator();

            // create an autoResetEvent which we will wait for in our test verification
            var _autoResetEvent = new AutoResetEvent(false);

            asyncStreamReader.MoveNext(Arg.Any<CancellationToken>()).Returns(enumerator.MoveNext());
            asyncStreamReader.Current.Returns(_ =>
            {
                // set the autoResetEvent since this path should be the last one that's reached in the background task
                _autoResetEvent.Set();
                return enumerator.Current;
            });

            var grpcEventStreamResp = new AsyncServerStreamingCall<EventStreamResponse>(
                asyncStreamReader,
                null,
                null,
                null,
                null,
                null
            );

            mockGrpcClient.EventStream(
                    Arg.Any<EventStreamRequest>(), null, null, CancellationToken.None)
                .Returns(grpcEventStreamResp);

            var mockCache = Substitute.For<ICache<string, object>>();
            mockCache.TryGet(Arg.Is<string>(s => s == "my-key")).Returns(null);
            mockCache.Add(Arg.Is<string>(s => s == "my-key"), Arg.Any<object>());


            var config = new FlagdConfig();
            config.CacheEnabled = true;
            config.MaxEventStreamRetries = 1;

            var rpcResolver = new RpcResolver(mockGrpcClient, config, mockCache, MakeChannel(), MakeProviderMetadata());
            var flagdProvider = new FlagdProvider(rpcResolver);
            await flagdProvider.InitializeAsync(EvaluationContext.Empty);

            // resolve with default set to false to make sure we return what the grpc server gives us
            var val = await flagdProvider.ResolveBooleanValueAsync("my-key", false);
            Assert.True(val.Value);

            Assert.True(_autoResetEvent.WaitOne(10000));
            mockCache.Received(1).TryGet(Arg.Is<string>(s => s == "my-key"));
            mockCache.Received(1).Add(Arg.Is<string>(s => s == "my-key"), Arg.Any<object>());
            mockGrpcClient.Received(Quantity.AtLeastOne()).EventStream(Arg.Any<EventStreamRequest>(), null, null, CancellationToken.None);
        }

        [Fact]
        public async Task TestCacheHitAsync()
        {

            var mockGrpcClient = Substitute.For<Service.ServiceClient>();

            var asyncStreamReader = Substitute.For<IAsyncStreamReader<EventStreamResponse>>();

            var l = new List<EventStreamResponse>
            {
                new EventStreamResponse{
                    Type = "provider_ready"
                }
            };

            var enumerator = l.GetEnumerator();

            // create an autoResetEvent which we will wait for in our test verification
            AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

            asyncStreamReader.MoveNext(Arg.Any<CancellationToken>()).Returns(enumerator.MoveNext());
            asyncStreamReader.Current.Returns(_ =>
            {
                // set the autoResetEvent since this path should be the last one that's reached in the background task
                _autoResetEvent.Set();
                return enumerator.Current;
            });

            var grpcEventStreamResp = new AsyncServerStreamingCall<EventStreamResponse>(
                asyncStreamReader,
                null,
                null,
                null,
                null,
                null
            );

            mockGrpcClient.EventStream(
                Arg.Any<EventStreamRequest>(), null, null, CancellationToken.None)
                .Returns(grpcEventStreamResp);

            var mockCache = Substitute.For<ICache<string, object>>();
            mockCache.TryGet("my-key").Returns(new ResolutionDetails<bool>("my-key", true));

            var config = new FlagdConfig
            {
                CacheEnabled = true,
                MaxEventStreamRetries = 1
            };

            var rpcResolver = new RpcResolver(mockGrpcClient, config, mockCache, MakeChannel(), MakeProviderMetadata());
            var flagdProvider = new FlagdProvider(rpcResolver);
            await flagdProvider.InitializeAsync(EvaluationContext.Empty);

            // resolve with default set to false to make sure we return what the grpc server gives us
            var val = await flagdProvider.ResolveBooleanValueAsync("my-key", false);
            Assert.True(val.Value);

            // wait for the autoReset event to be fired before verifying the invocation of the mocked functions
            Assert.True(_autoResetEvent.WaitOne(10000));
            mockCache.Received(1).TryGet("my-key");
            mockGrpcClient.Received(Quantity.AtLeastOne()).EventStream(Arg.Any<EventStreamRequest>(), null, null, CancellationToken.None);
        }

        [Fact]
        public async Task TestCacheInvalidationAsync()
        {
            var resp = new ResolveBooleanResponse();
            resp.Value = true;
            resp.Reason = "STATIC";

            var grpcResp = new AsyncUnaryCall<ResolveBooleanResponse>(
                Task.FromResult(resp),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { });

            var mockGrpcClient = Substitute.For<Service.ServiceClient>();
            mockGrpcClient.ResolveBooleanAsync(
                    Arg.Any<ResolveBooleanRequest>(), null, null, CancellationToken.None)
                .Returns(grpcResp);

            var asyncStreamReader = Substitute.For<IAsyncStreamReader<EventStreamResponse>>();

            var configurationChangeData = new Struct();
            var changedFlag = new Struct();
            changedFlag.Fields.Add("my-key", new ProtoValue());
            configurationChangeData.Fields.Add("flags", ProtoValue.ForStruct(changedFlag));


            var firstCall = true;

            asyncStreamReader.MoveNext(Arg.Any<CancellationToken>()).Returns(true);
            // as long as we did not send our first request to the provider, we will not send a configuration_change event
            // after the value of the flag has been retrieved the first time, we will send a configuration_change to test if the
            // item is deleted from the cache

            // create an autoResetEvent which we will wait for in our test verification
            AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

            asyncStreamReader.Current.Returns(
                _ =>
                {
                    if (firstCall)
                    {
                        return new EventStreamResponse
                        {
                            Type = "provider_ready"
                        };
                    }
                    return new EventStreamResponse
                    {
                        Type = "configuration_change",
                        Data = configurationChangeData
                    };
                }
            );

            var grpcEventStreamResp = new AsyncServerStreamingCall<EventStreamResponse>(
                asyncStreamReader,
                null,
                null,
                null,
                null,
                null
            );

            mockGrpcClient.EventStream(
                Arg.Any<EventStreamRequest>(), null, null, CancellationToken.None)
                .Returns(grpcEventStreamResp);

            var mockCache = Substitute.For<ICache<string, object>>();
            mockCache.TryGet(Arg.Is<string>(s => s == "my-key")).Returns(null);
            mockCache.Add(Arg.Is<string>(s => s == "my-key"), Arg.Any<object>());
            mockCache.When(x => x.Delete("my-key")).Do(_ =>
            {
                // set the autoResetEvent since this path should be the last one that's reached in the background task
                _autoResetEvent.Set();
            });


            var config = new FlagdConfig();
            config.CacheEnabled = true;
            config.MaxEventStreamRetries = 1;

            var rpcResolver = new RpcResolver(mockGrpcClient, config, mockCache, MakeChannel(), MakeProviderMetadata());
            var flagdProvider = new FlagdProvider(rpcResolver);
            await flagdProvider.InitializeAsync(EvaluationContext.Empty);

            // resolve with default set to false to make sure we return what the grpc server gives us
            var val = await flagdProvider.ResolveBooleanValueAsync("my-key", false);
            Assert.True(val.Value);

            // set firstCall to true to make the mock EventStream return a configuration_change event
            firstCall = false;

            val = await flagdProvider.ResolveBooleanValueAsync("my-key", false);
            Assert.True(val.Value);

            Assert.True(_autoResetEvent.WaitOne(10000));

            mockCache.Received(2).TryGet("my-key");
            mockCache.Received(2).Add("my-key", Arg.Any<object>());
            mockCache.Received().Delete("my-key");
            mockGrpcClient.Received(Quantity.AtLeastOne()).EventStream(Arg.Any<EventStreamRequest>(), null, null, CancellationToken.None);
        }

        [Fact]
        public async Task TestInProcessResolver()
        {
            var resp = new ResolveBooleanResponse();
            resp.Value = true;
            resp.Reason = "STATIC";

            var mockGrpcClient = Substitute.For<FlagSyncService.FlagSyncServiceClient>();
            var asyncStreamReader = Substitute.For<IAsyncStreamReader<SyncFlagsResponse>>();
            var mockJsonSchemaValidator = Substitute.For<IJsonSchemaValidator>();

            var l = new List<SyncFlagsResponse>
            {
                new SyncFlagsResponse{
                    FlagConfiguration = Utils.flags
                }
            };

            var enumerator = l.GetEnumerator();


            asyncStreamReader.MoveNext(Arg.Any<CancellationToken>()).Returns(enumerator.MoveNext());
            asyncStreamReader.Current.Returns(_ => enumerator.Current);

            var grpcEventStreamResp = new AsyncServerStreamingCall<SyncFlagsResponse>(
                asyncStreamReader,
                null,
                null,
                null,
                null,
                null
                );

            mockGrpcClient.SyncFlags(
                Arg.Any<SyncFlagsRequest>(), null, null, CancellationToken.None)
                .Returns(grpcEventStreamResp);


            var config = new FlagdConfig();
            config.CacheEnabled = true;
            config.MaxEventStreamRetries = 1;
            config.SourceSelector = "source-selector";

            var rpcResolver = new InProcessResolver(mockGrpcClient, config, MakeChannel(), MakeProviderMetadata(), mockJsonSchemaValidator);
            var flagdProvider = new FlagdProvider(rpcResolver);
            await flagdProvider.InitializeAsync(EvaluationContext.Empty);

            // resolve with default set to false to make sure we return what the grpc server gives us
            await Utils.AssertUntilAsync(
                async _ =>
                {
                    var val = await flagdProvider.ResolveBooleanValueAsync("staticBoolFlag", false, cancellationToken: CancellationToken.None);
                    Assert.True(val.Value);
                });

            mockGrpcClient.Received(Quantity.AtLeastOne()).SyncFlags(Arg.Is<SyncFlagsRequest>(req => req.Selector == "source-selector"), null, null, CancellationToken.None);
        }

        [Fact]
        public async Task TestInProcessResolverDefaultValueIfNotFound()
        {
            var resp = new ResolveBooleanResponse();
            resp.Value = true;
            resp.Reason = "STATIC";

            var mockGrpcClient = Substitute.For<FlagSyncService.FlagSyncServiceClient>();
            var asyncStreamReader = Substitute.For<IAsyncStreamReader<SyncFlagsResponse>>();
            var mockJsonSchemaValidator = Substitute.For<IJsonSchemaValidator>();

            var l = new List<SyncFlagsResponse>
            {
                new SyncFlagsResponse{
                    FlagConfiguration = Utils.flags
                }
            };

            var enumerator = l.GetEnumerator();


            asyncStreamReader.MoveNext(Arg.Any<CancellationToken>()).Returns(enumerator.MoveNext());
            asyncStreamReader.Current.Returns(_ => enumerator.Current);

            var grpcEventStreamResp = new AsyncServerStreamingCall<SyncFlagsResponse>(
                asyncStreamReader,
                null,
                null,
                null,
                null,
                null
                );

            mockGrpcClient.SyncFlags(
                Arg.Any<SyncFlagsRequest>(), null, null, CancellationToken.None)
                .Returns(grpcEventStreamResp);


            var config = new FlagdConfig();
            config.CacheEnabled = true;
            config.MaxEventStreamRetries = 1;
            config.SourceSelector = "source-selector";

            var inProcessResolver = new InProcessResolver(mockGrpcClient, config, MakeChannel(), MakeProviderMetadata(), mockJsonSchemaValidator);
            var flagdProvider = new FlagdProvider(inProcessResolver);
            await flagdProvider.InitializeAsync(EvaluationContext.Empty);

            // resolve with default set false to make sure we return what the grpc server gives us
            await Utils.AssertUntilAsync(
                async _ =>
                {
                    var exception = await Assert.ThrowsAsync<FeatureProviderException>(async () => await flagdProvider.ResolveStringValueAsync("unknown", "unknown"));
                    Assert.Equal(ErrorType.FlagNotFound, exception.ErrorType);
                });

            mockGrpcClient.Received(Quantity.AtLeastOne()).SyncFlags(Arg.Is<SyncFlagsRequest>(req => req.Selector == "source-selector"), null, null, CancellationToken.None);
        }
    }
}
