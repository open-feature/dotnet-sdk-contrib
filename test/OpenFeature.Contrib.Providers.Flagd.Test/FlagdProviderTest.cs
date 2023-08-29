using Xunit;
using Moq;
using OpenFeature.Flagd.Grpc;
using Grpc.Core;
using Google.Protobuf.WellKnownTypes;
using OpenFeature.Error;
using ProtoValue = Google.Protobuf.WellKnownTypes.Value;
using System.Collections.Generic;
using OpenFeature.Model;
using System.Threading;
using System;
using NSubstitute;

namespace OpenFeature.Contrib.Providers.Flagd.Test
{
    public class UnitTestFlagdProvider
    {
        [Fact]
        public void BuildClientForPlatform_Should_Throw_Exception_When_FlagdCertPath_Not_Exists()
        {
            // Arrange
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvCertPart, "non-existing-path");
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvVarHost, "localhost");
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvVarPort, "5001");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new FlagdProvider());

            // Cleanup
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvCertPart, "");
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvVarHost, "");
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvVarPort, "");
        }

        [Fact]
        public void BuildClientForPlatform_Should_Return_Client_For_Non_Unix_Socket_Without_Certificate()
        {
            // Arrange
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvVarHost, "localhost");
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvVarPort, "5001");

            // Act
            var flagdProvider = new FlagdProvider();
            var client = flagdProvider.GetClient();

            // Assert
            Assert.NotNull(client);
            Assert.IsType<Service.ServiceClient>(client);

            // Cleanup
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvVarHost, "");
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvVarPort, "");
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
            Assert.Equal("No-op Provider", FlagdProvider.GetProviderName());
        }

        [Fact]
        public void TestGetProviderWithDefaultConfig()
        {
            var flagdProvider = new FlagdProvider();

            var client = flagdProvider.GetClient();

            Assert.NotNull(client);
        }

        [Fact]
        public void TestGetProviderWithConfig()
        {
            // Create a config with default values set
            var config = new FlagdConfig();

            // Set env variables (should be ignored by the constructor)
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvCertPart, "path");
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvVarHost, "localhost111");
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvVarPort, "5001");
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvVarTLS, "true");
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvVarCache, "LRU");
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvVarMaxCacheSize, "20");

            // Create provider, which ignores the env vars and uses the config
            var flagdProvider = new FlagdProvider(config);

            // Client should no be nil
            var client = flagdProvider.GetClient();
            Assert.NotNull(client);

            // Retrieve config for assertions
            config = flagdProvider.GetConfig();

            // Assert
            Assert.Equal("", config.CertificatePath);
            Assert.Equal(new Uri("http://localhost:8013"), config.GetUri());
            Assert.False(config.CacheEnabled);
            Assert.Equal(0, config.MaxCacheSize);

            // Cleanup
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvCertPart, "");
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvVarHost, "");
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvVarPort, "");
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvVarTLS, "");
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvVarCache, "");
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvVarMaxCacheSize, "");
        }

        [Fact]
        public void TestResolveBooleanValue()
        {
            var resp = new ResolveBooleanResponse
            {
                Value = true
            };

            var grpcResp = new AsyncUnaryCall<ResolveBooleanResponse>(
                System.Threading.Tasks.Task.FromResult(resp),
                System.Threading.Tasks.Task.FromResult(new Grpc.Core.Metadata()),
                () => Status.DefaultSuccess,
                () => new Grpc.Core.Metadata(),
                () => { });

            var substituteGrpcClient = Substitute.For<Service.ServiceClient>();
            substituteGrpcClient
                .ResolveBooleanAsync(
                    Arg.Any<ResolveBooleanRequest>(), null, null, System.Threading.CancellationToken.None)
                .Returns(grpcResp);

            var flagdProvider = new FlagdProvider(substituteGrpcClient, new FlagdConfig());

            // resolve with default set to false to make sure we return what the grpc server gives us
            var val = flagdProvider.ResolveBooleanValue("my-key", false, null);

            Assert.True(val.Result.Value);
        }

        [Fact]
        public void TestResolveStringValue()
        {
            var resp = new ResolveStringResponse { Value = "my-value" };

            var grpcResp = new AsyncUnaryCall<ResolveStringResponse>(
                System.Threading.Tasks.Task.FromResult(resp),
                System.Threading.Tasks.Task.FromResult(new Grpc.Core.Metadata()),
                () => Status.DefaultSuccess,
                () => new Grpc.Core.Metadata(),
                () => { });

            var subGrpcClient = Substitute.For<Service.ServiceClient>();

            subGrpcClient.ResolveStringAsync(
                    Arg.Any<ResolveStringRequest>(), null, null, System.Threading.CancellationToken.None)
                .Returns(grpcResp);

            var flagdProvider = new FlagdProvider(subGrpcClient, new FlagdConfig());

            var val = flagdProvider.ResolveStringValue("my-key", "", null);

            Assert.Equal("my-value", val.Result.Value);
        }

        [Fact]
        public void TestResolveIntegerValue()
        {
            var resp = new ResolveIntResponse
            {
                Value = 10
            };

            var grpcResp = new AsyncUnaryCall<ResolveIntResponse>(
                System.Threading.Tasks.Task.FromResult(resp),
                System.Threading.Tasks.Task.FromResult(new Grpc.Core.Metadata()),
                () => Status.DefaultSuccess,
                () => new Grpc.Core.Metadata(),
                () => { });

            var subGrpcClient = Substitute.For<Service.ServiceClient>();
            subGrpcClient.ResolveIntAsync(Arg.Any<ResolveIntRequest>(), null, null, System.Threading.CancellationToken.None)
                .Returns(grpcResp);

            var flagdProvider = new FlagdProvider(subGrpcClient, new FlagdConfig());

            var val = flagdProvider.ResolveIntegerValue("my-key", 0, null);

            Assert.Equal(10, val.Result.Value);
        }

        [Fact]
        public void TestResolveDoubleValue()
        {
            var resp = new ResolveFloatResponse
            {
                Value = 10.0
            };

            var grpcResp = new AsyncUnaryCall<ResolveFloatResponse>(
                System.Threading.Tasks.Task.FromResult(resp),
                System.Threading.Tasks.Task.FromResult(new Grpc.Core.Metadata()),
                () => Status.DefaultSuccess,
                () => new Grpc.Core.Metadata(),
                () => { });

            var mockGrpcClient = Substitute.For<Service.ServiceClient>();
            mockGrpcClient.ResolveFloatAsync(Arg.Any<ResolveFloatRequest>(), null, null, System.Threading.CancellationToken.None)
                .Returns(grpcResp);

            var flagdProvider = new FlagdProvider(mockGrpcClient, new FlagdConfig());

            var val = flagdProvider.ResolveDoubleValue("my-key", 0.0, null);

            Assert.Equal(10.0, val.Result.Value);
        }

        [Fact]
        public void TestResolveStructureValue()
        {
            var resp = new ResolveObjectResponse();

            var returnedValue = new Struct();
            returnedValue.Fields.Add("my-key", ProtoValue.ForString("my-value"));


            resp.Value = returnedValue;

            var grpcResp = new AsyncUnaryCall<ResolveObjectResponse>(
                System.Threading.Tasks.Task.FromResult(resp),
                System.Threading.Tasks.Task.FromResult(new Grpc.Core.Metadata()),
                () => Status.DefaultSuccess,
                () => new Grpc.Core.Metadata(),
                () => { });

            var mockGrpcClient = Substitute.For<Service.ServiceClient>();
            mockGrpcClient.ResolveObjectAsync(Arg.Any<ResolveObjectRequest>(), null, null, System.Threading.CancellationToken.None)
                .Returns(grpcResp);

            var flagdProvider = new FlagdProvider(mockGrpcClient, new FlagdConfig());

            var val = flagdProvider.ResolveStructureValue("my-key", null, null);

            Assert.True(val.Result.Value.AsStructure.ContainsKey("my-key"));
        }

        [Fact]
        public void TestResolveFlagNotFound()
        {
            var exc = new RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.NotFound, Constant.ErrorType.FlagNotFound.ToString()));

            var grpcResp = new AsyncUnaryCall<ResolveBooleanResponse>(
                System.Threading.Tasks.Task.FromException<ResolveBooleanResponse>(exc),
                System.Threading.Tasks.Task.FromResult(new Grpc.Core.Metadata()),
                () => Status.DefaultSuccess,
                () => new Grpc.Core.Metadata(),
                () => { });

            var mockGrpcClient = Substitute.For<Service.ServiceClient>();
            mockGrpcClient.ResolveBooleanAsync(
                    Arg.Any<ResolveBooleanRequest>(), null, null, System.Threading.CancellationToken.None).Returns(grpcResp);

            var flagdProvider = new FlagdProvider(mockGrpcClient, new FlagdConfig());

            // make sure the correct exception is thrown
            Assert.ThrowsAsync<FeatureProviderException>(async () =>
            {
                try
                {
                    await flagdProvider.ResolveBooleanValue("my-key", true, null);
                }
                catch (FeatureProviderException e)
                {
                    Assert.Equal(Constant.ErrorType.FlagNotFound, e.ErrorType);
                    Assert.Equal(Constant.ErrorType.FlagNotFound.ToString(), e.Message);
                    throw;
                }
            });
        }

        [Fact]
        public void TestResolveGrpcHostUnavailable()
        {
            var exc = new RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.Unavailable, Constant.ErrorType.ProviderNotReady.ToString()));

            var grpcResp = new AsyncUnaryCall<ResolveBooleanResponse>(
                System.Threading.Tasks.Task.FromException<ResolveBooleanResponse>(exc),
                System.Threading.Tasks.Task.FromResult(new Grpc.Core.Metadata()),
                () => Status.DefaultSuccess,
                () => new Grpc.Core.Metadata(),
                () => { });

            var mockGrpcClient = Substitute.For<Service.ServiceClient>();
            mockGrpcClient.ResolveBooleanAsync(
                    Arg.Any<ResolveBooleanRequest>(), null, null, System.Threading.CancellationToken.None)
                .Returns(grpcResp);

            var flagdProvider = new FlagdProvider(mockGrpcClient, new FlagdConfig());

            // make sure the correct exception is thrown
            Assert.ThrowsAsync<FeatureProviderException>(async () =>
            {
                try
                {
                    await flagdProvider.ResolveBooleanValue("my-key", true, null);
                }
                catch (FeatureProviderException e)
                {
                    Assert.Equal(Constant.ErrorType.ProviderNotReady, e.ErrorType);
                    Assert.Equal(Constant.ErrorType.ProviderNotReady.ToString(), e.Message);
                    throw;
                }
            });
        }

        [Fact]
        public void TestResolveTypeMismatch()
        {
            var exc = new RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.InvalidArgument, Constant.ErrorType.TypeMismatch.ToString()));

            var grpcResp = new AsyncUnaryCall<ResolveBooleanResponse>(
                System.Threading.Tasks.Task.FromException<ResolveBooleanResponse>(exc),
                System.Threading.Tasks.Task.FromResult(new Grpc.Core.Metadata()),
                () => Status.DefaultSuccess,
                () => new Grpc.Core.Metadata(),
                () => { });

            var mockGrpcClient = Substitute.For<Service.ServiceClient>();
            mockGrpcClient.ResolveBooleanAsync(
                    Arg.Any<ResolveBooleanRequest>(), null, null, System.Threading.CancellationToken.None)
                .Returns(grpcResp);

            var flagdProvider = new FlagdProvider(mockGrpcClient, new FlagdConfig());

            // make sure the correct exception is thrown
            Assert.ThrowsAsync<FeatureProviderException>(async () =>
            {
                try
                {
                    await flagdProvider.ResolveBooleanValue("my-key", true, null);
                }
                catch (FeatureProviderException e)
                {
                    Assert.Equal(Constant.ErrorType.TypeMismatch, e.ErrorType);
                    Assert.Equal(Constant.ErrorType.TypeMismatch.ToString(), e.Message);
                    throw;
                }
            });
        }

        [Fact]
        public void TestResolveUnknownError()
        {
            var exc = new RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.Internal, "unknown error"));

            var grpcResp = new AsyncUnaryCall<ResolveBooleanResponse>(
                System.Threading.Tasks.Task.FromException<ResolveBooleanResponse>(exc),
                System.Threading.Tasks.Task.FromResult(new Grpc.Core.Metadata()),
                () => Status.DefaultSuccess,
                () => new Grpc.Core.Metadata(),
                () => { });

            var mockGrpcClient = Substitute.For<Service.ServiceClient>();
            mockGrpcClient.ResolveBooleanAsync(
                    Arg.Any<ResolveBooleanRequest>(), null, null, System.Threading.CancellationToken.None)
                .Returns(grpcResp);

            var flagdProvider = new FlagdProvider(mockGrpcClient, new FlagdConfig());

            // make sure the correct exception is thrown
            Assert.ThrowsAsync<FeatureProviderException>(async () =>
            {
                try
                {
                    await flagdProvider.ResolveBooleanValue("my-key", true, null);
                }
                catch (FeatureProviderException e)
                {
                    Assert.Equal(Constant.ErrorType.General, e.ErrorType);
                    Assert.Equal(Constant.ErrorType.General.ToString(), e.Message);
                    throw;
                }
            });
        }

        [Fact]
        public void TestCache()
        {
            var resp = new ResolveBooleanResponse();
            resp.Value = true;
            resp.Reason = "STATIC";

            var grpcResp = new AsyncUnaryCall<ResolveBooleanResponse>(
                System.Threading.Tasks.Task.FromResult(resp),
                System.Threading.Tasks.Task.FromResult(new Grpc.Core.Metadata()),
                () => Status.DefaultSuccess,
                () => new Grpc.Core.Metadata(),
                () => { });

            var mockGrpcClient = Substitute.For<Service.ServiceClient>();
            mockGrpcClient.ResolveBooleanAsync(
                    Arg.Any<ResolveBooleanRequest>(), null, null, System.Threading.CancellationToken.None)
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

            asyncStreamReader.MoveNext(Arg.Any<System.Threading.CancellationToken>()).Returns(enumerator.MoveNext());
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
                    Arg.Any<Empty>(), null, null, System.Threading.CancellationToken.None)
                .Returns(grpcEventStreamResp);

            var mockCache = Substitute.For<ICache<string, object>>();
            mockCache.TryGet(Arg.Is<string>(s => s == "my-key")).Returns(null);
            mockCache.Add(Arg.Is<string>(s => s == "my-key"), Arg.Any<object>());


            var config = new FlagdConfig();
            config.CacheEnabled = true;
            config.MaxEventStreamRetries = 1;
            var flagdProvider = new FlagdProvider(mockGrpcClient, config, mockCache);

            // resolve with default set to false to make sure we return what the grpc server gives us
            var val = flagdProvider.ResolveBooleanValue("my-key", false, null);
            Assert.True(val.Result.Value);

            Assert.True(_autoResetEvent.WaitOne(10000));
            mockCache.Received(1).TryGet(Arg.Is<string>(s => s == "my-key"));
            mockCache.Received(1).Add(Arg.Is<string>(s => s == "my-key"), Arg.Any<object>());
            mockGrpcClient.Received(1).EventStream(Arg.Any<Empty>(), null, null, System.Threading.CancellationToken.None);
        }
        //
        // [Fact]
        // public void TestCacheHit()
        // {
        //
        //     var mockGrpcClient = Substitute.For<Service.ServiceClient>();
        //
        //     var asyncStreamReader = Substitute.For<IAsyncStreamReader<EventStreamResponse>>();
        //
        //     var l = new List<EventStreamResponse>
        //     {
        //         new EventStreamResponse{
        //             Type = "provider_ready"
        //         }
        //     };
        //
        //     var enumerator = l.GetEnumerator();
        //
        //     // create an autoResetEvent which we will wait for in our test verification
        //     AutoResetEvent _autoResetEvent = new AutoResetEvent(false);
        //
        //     asyncStreamReader.Setup(a => a.MoveNext(Arg.Any<System.Threading.CancellationToken>())).ReturnsAsync(() => enumerator.MoveNext());
        //     asyncStreamReader.Setup(a => a.Current).Returns(() =>
        //     {
        //         // set the autoResetEvent since this path should be the last one that's reached in the background task
        //         _autoResetEvent.Set();
        //         return enumerator.Current;
        //     });
        //
        //     var grpcEventStreamResp = new AsyncServerStreamingCall<EventStreamResponse>(
        //         asyncStreamReader.Object,
        //         null,
        //         null,
        //         null,
        //         null,
        //         null
        //     );
        //
        //     mockGrpcClient
        //         .Setup(m => m.EventStream(
        //             Arg.Any<Empty>(), null, null, System.Threading.CancellationToken.None))
        //         .Returns(grpcEventStreamResp);
        //
        //     var mockCache = Substitute.For<ICache<string, object>>();
        //     mockCache.Setup(c => c.TryGet(It.Is<string>(s => s == "my-key"))).Returns(
        //         () => new ResolutionDetails<bool>("my-key", true)
        //     );
        //
        //     var config = new FlagdConfig();
        //     config.CacheEnabled = true;
        //     config.MaxEventStreamRetries = 1;
        //     var flagdProvider = new FlagdProvider(mockGrpcClient.Object, config, mockCache.Object);
        //
        //     // resolve with default set to false to make sure we return what the grpc server gives us
        //     var val = flagdProvider.ResolveBooleanValue("my-key", false, null);
        //     Assert.True(val.Result.Value);
        //
        //     // wait for the autoReset event to be fired before verifying the invocation of the mocked functions
        //     Assert.True(_autoResetEvent.WaitOne(10000));
        //     mockCache.VerifyAll();
        //     mockGrpcClient.VerifyAll();
        // }
        //
        // [Fact]
        // public void TestCacheInvalidation()
        // {
        //     var resp = new ResolveBooleanResponse();
        //     resp.Value = true;
        //     resp.Reason = "STATIC";
        //
        //     var grpcResp = new AsyncUnaryCall<ResolveBooleanResponse>(
        //         System.Threading.Tasks.Task.FromResult(resp),
        //         System.Threading.Tasks.Task.FromResult(new Grpc.Core.Metadata()),
        //         () => Status.DefaultSuccess,
        //         () => new Grpc.Core.Metadata(),
        //         () => { });
        //
        //     var mockGrpcClient = Substitute.For<Service.ServiceClient>();
        //     mockGrpcClient
        //         .Setup(m => m.ResolveBooleanAsync(
        //             Arg.Any<ResolveBooleanRequest>(), null, null, System.Threading.CancellationToken.None))
        //         .Returns(grpcResp);
        //
        //     var asyncStreamReader = Substitute.For<IAsyncStreamReader<EventStreamResponse>>();
        //
        //     var configurationChangeData = new Struct();
        //     var changedFlag = new Struct();
        //     changedFlag.Fields.Add("my-key", new Google.Protobuf.WellKnownTypes.Value());
        //     configurationChangeData.Fields.Add("flags", ProtoValue.ForStruct(changedFlag));
        //
        //
        //     var firstCall = true;
        //
        //     asyncStreamReader.Setup(a => a.MoveNext(Arg.Any<System.Threading.CancellationToken>())).ReturnsAsync(() => true);
        //     // as long as we did not send our first request to the provider, we will not send a configuration_change event
        //     // after the value of the flag has been retrieved the first time, we will send a configuration_change to test if the
        //     // item is deleted from the cache
        //
        //     // create an autoResetEvent which we will wait for in our test verification
        //     AutoResetEvent _autoResetEvent = new AutoResetEvent(false);
        //
        //     asyncStreamReader.Setup(a => a.Current).Returns(
        //         () =>
        //         {
        //             if (firstCall)
        //             {
        //                 return new EventStreamResponse
        //                 {
        //                     Type = "provider_ready"
        //                 };
        //             }
        //             return new EventStreamResponse
        //             {
        //                 Type = "configuration_change",
        //                 Data = configurationChangeData
        //             };
        //         }
        //     );
        //
        //     var grpcEventStreamResp = new AsyncServerStreamingCall<EventStreamResponse>(
        //         asyncStreamReader.Object,
        //         null,
        //         null,
        //         null,
        //         null,
        //         null
        //     );
        //
        //     mockGrpcClient
        //         .Setup(m => m.EventStream(
        //             Arg.Any<Empty>(), null, null, System.Threading.CancellationToken.None))
        //         .Returns(() =>
        //         {
        //             return grpcEventStreamResp;
        //         });
        //
        //     var mockCache = Substitute.For<ICache<string, object>>();
        //     mockCache.Setup(c => c.TryGet(It.Is<string>(s => s == "my-key"))).Returns(() => null);
        //     mockCache.Setup(c => c.Add(It.Is<string>(s => s == "my-key"), Arg.Any<object>()));
        //     mockCache.Setup(c => c.Delete(It.Is<string>(s => s == "my-key"))).Callback(() =>
        //     {
        //         // set the autoResetEvent since this path should be the last one that's reached in the background task
        //         _autoResetEvent.Set();
        //     });
        //
        //
        //     var config = new FlagdConfig();
        //     config.CacheEnabled = true;
        //     config.MaxEventStreamRetries = 1;
        //     var flagdProvider = new FlagdProvider(mockGrpcClient.Object, config, mockCache.Object);
        //
        //     // resolve with default set to false to make sure we return what the grpc server gives us
        //     var val = flagdProvider.ResolveBooleanValue("my-key", false, null);
        //     Assert.True(val.Result.Value);
        //
        //     // set firstCall to true to make the mock EventStream return a configuration_change event
        //     firstCall = false;
        //
        //     val = flagdProvider.ResolveBooleanValue("my-key", false, null);
        //     Assert.True(val.Result.Value);
        //
        //     Assert.True(_autoResetEvent.WaitOne(10000));
        //
        //     mockCache.VerifyAll();
        //     mockGrpcClient.VerifyAll();
        // }
    }
}
