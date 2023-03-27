using Xunit;
using Moq;
using OpenFeature.Flagd.Grpc;
using Grpc.Core;
using Google.Protobuf.WellKnownTypes;
using OpenFeature.Error;
using ProtoValue = Google.Protobuf.WellKnownTypes.Value;
using System.Collections.Generic;
using System.Linq;
using OpenFeature.Model;
using System.Threading;

namespace OpenFeature.Contrib.Providers.Flagd.Test
{
    public class UnitTestFlagdProvider
    {
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
        public void TestResolveBooleanValue()
        {
            var resp = new ResolveBooleanResponse();
            resp.Value = true;

            var grpcResp = new AsyncUnaryCall<ResolveBooleanResponse>(
                System.Threading.Tasks.Task.FromResult(resp),
                System.Threading.Tasks.Task.FromResult(new Grpc.Core.Metadata()),
                () => Status.DefaultSuccess,
                () => new Grpc.Core.Metadata(),
                () => { });

            var mockGrpcClient = new Mock<Service.ServiceClient>();
            mockGrpcClient
                .Setup(m => m.ResolveBooleanAsync(
                    It.IsAny<ResolveBooleanRequest>(), null, null, System.Threading.CancellationToken.None))
                .Returns(grpcResp);

            var flagdProvider = new FlagdProvider(mockGrpcClient.Object, new FlagdConfig());

            // resolve with default set to false to make sure we return what the grpc server gives us
            var val = flagdProvider.ResolveBooleanValue("my-key", false, null);

            Assert.True(val.Result.Value);
        }

        [Fact]
        public void TestResolveStringValue()
        {
            var resp = new ResolveStringResponse();
            resp.Value = "my-value";

            var grpcResp = new AsyncUnaryCall<ResolveStringResponse>(
                System.Threading.Tasks.Task.FromResult(resp),
                System.Threading.Tasks.Task.FromResult(new Grpc.Core.Metadata()),
                () => Status.DefaultSuccess,
                () => new Grpc.Core.Metadata(),
                () => { });

            var mockGrpcClient = new Mock<Service.ServiceClient>();
            mockGrpcClient
                .Setup(m => m.ResolveStringAsync(
                    It.IsAny<ResolveStringRequest>(), null, null, System.Threading.CancellationToken.None))
                .Returns(grpcResp);

            var flagdProvider = new FlagdProvider(mockGrpcClient.Object, new FlagdConfig());

            var val = flagdProvider.ResolveStringValue("my-key", "", null);

            Assert.Equal("my-value", val.Result.Value);
        }

        [Fact]
        public void TestResolveIntegerValue()
        {
            var resp = new ResolveIntResponse();
            resp.Value = 10;

            var grpcResp = new AsyncUnaryCall<ResolveIntResponse>(
                System.Threading.Tasks.Task.FromResult(resp),
                System.Threading.Tasks.Task.FromResult(new Grpc.Core.Metadata()),
                () => Status.DefaultSuccess,
                () => new Grpc.Core.Metadata(),
                () => { });

            var mockGrpcClient = new Mock<Service.ServiceClient>();
            mockGrpcClient
                .Setup(m => m.ResolveIntAsync(
                    It.IsAny<ResolveIntRequest>(), null, null, System.Threading.CancellationToken.None))
                .Returns(grpcResp);

            var flagdProvider = new FlagdProvider(mockGrpcClient.Object, new FlagdConfig());

            var val = flagdProvider.ResolveIntegerValue("my-key", 0, null);

            Assert.Equal(10, val.Result.Value);
        }

        [Fact]
        public void TestResolveDoubleValue()
        {
            var resp = new ResolveFloatResponse();
            resp.Value = 10.0;

            var grpcResp = new AsyncUnaryCall<ResolveFloatResponse>(
                System.Threading.Tasks.Task.FromResult(resp),
                System.Threading.Tasks.Task.FromResult(new Grpc.Core.Metadata()),
                () => Status.DefaultSuccess,
                () => new Grpc.Core.Metadata(),
                () => { });

            var mockGrpcClient = new Mock<Service.ServiceClient>();
            mockGrpcClient
                .Setup(m => m.ResolveFloatAsync(
                    It.IsAny<ResolveFloatRequest>(), null, null, System.Threading.CancellationToken.None))
                .Returns(grpcResp);

            var flagdProvider = new FlagdProvider(mockGrpcClient.Object, new FlagdConfig());

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

            var mockGrpcClient = new Mock<Service.ServiceClient>();
            mockGrpcClient
                .Setup(m => m.ResolveObjectAsync(
                    It.IsAny<ResolveObjectRequest>(), null, null, System.Threading.CancellationToken.None))
                .Returns(grpcResp);

            var flagdProvider = new FlagdProvider(mockGrpcClient.Object, new FlagdConfig());

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

            var mockGrpcClient = new Mock<Service.ServiceClient>();
            mockGrpcClient
                .Setup(m => m.ResolveBooleanAsync(
                    It.IsAny<ResolveBooleanRequest>(), null, null, System.Threading.CancellationToken.None))
                .Returns(grpcResp);

            var flagdProvider = new FlagdProvider(mockGrpcClient.Object, new FlagdConfig());

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

            var mockGrpcClient = new Mock<Service.ServiceClient>();
            mockGrpcClient
                .Setup(m => m.ResolveBooleanAsync(
                    It.IsAny<ResolveBooleanRequest>(), null, null, System.Threading.CancellationToken.None))
                .Returns(grpcResp);

            var flagdProvider = new FlagdProvider(mockGrpcClient.Object, new FlagdConfig());

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

            var mockGrpcClient = new Mock<Service.ServiceClient>();
            mockGrpcClient
                .Setup(m => m.ResolveBooleanAsync(
                    It.IsAny<ResolveBooleanRequest>(), null, null, System.Threading.CancellationToken.None))
                .Returns(grpcResp);

            var flagdProvider = new FlagdProvider(mockGrpcClient.Object, new FlagdConfig());

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

            var mockGrpcClient = new Mock<Service.ServiceClient>();
            mockGrpcClient
                .Setup(m => m.ResolveBooleanAsync(
                    It.IsAny<ResolveBooleanRequest>(), null, null, System.Threading.CancellationToken.None))
                .Returns(grpcResp);

            var flagdProvider = new FlagdProvider(mockGrpcClient.Object, new FlagdConfig());

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

            var mockGrpcClient = new Mock<Service.ServiceClient>();
            mockGrpcClient
                .Setup(m => m.ResolveBooleanAsync(
                    It.IsAny<ResolveBooleanRequest>(), null, null, System.Threading.CancellationToken.None))
                .Returns(grpcResp);

            var asyncStreamReader = new Mock<IAsyncStreamReader<EventStreamResponse>>();

            var l = new List<EventStreamResponse>
            {
                new EventStreamResponse{
                    Type = "provider_ready"
                }
            };

            var enumerator = l.GetEnumerator();

            // create an autoResetEvent which we will wait for in our test verification
            var _autoResetEvent = new AutoResetEvent(false);

            asyncStreamReader.Setup(a => a.MoveNext(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(() => enumerator.MoveNext());
            asyncStreamReader.Setup(a => a.Current).Returns(() => {
                // set the autoResetEvent since this path should be the last one that's reached in the background task
                _autoResetEvent.Set();
                return enumerator.Current;
            });

            var grpcEventStreamResp = new AsyncServerStreamingCall<EventStreamResponse>(
                asyncStreamReader.Object,
                null,
                null,
                null,
                null,
                null
            );

            mockGrpcClient
                .Setup(m => m.EventStream(
                    It.IsAny<Empty>(), null, null, System.Threading.CancellationToken.None))
                .Returns(grpcEventStreamResp);

            var mockCache = new Mock<ICache<string, ResolutionDetails<Model.Value>>>();
            mockCache.Setup(c => c.TryGet(It.Is<string>(s => s == "my-key"))).Returns(() => null);
            mockCache.Setup(c => c.Add(It.Is<string>(s => s == "my-key"), It.IsAny<ResolutionDetails<Model.Value>>()));


            var config = new FlagdConfig();
            config.CacheEnabled = true;
            config.MaxEventStreamRetries = 1;
            var flagdProvider = new FlagdProvider(mockGrpcClient.Object, config, mockCache.Object);

            // resolve with default set to false to make sure we return what the grpc server gives us
            var val = flagdProvider.ResolveBooleanValue("my-key", false, null);
            Assert.True(val.Result.Value);

            Assert.True(_autoResetEvent.WaitOne());
            mockCache.VerifyAll();
            mockGrpcClient.VerifyAll();
        }

        [Fact]
        public void TestCacheHit()
        {

            var mockGrpcClient = new Mock<Service.ServiceClient>();

            var asyncStreamReader = new Mock<IAsyncStreamReader<EventStreamResponse>>();

            var l = new List<EventStreamResponse>
            {
                new EventStreamResponse{
                    Type = "provider_ready"
                }
            };

            var enumerator = l.GetEnumerator();

            // create an autoResetEvent which we will wait for in our test verification
            AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

            asyncStreamReader.Setup(a => a.MoveNext(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(() => enumerator.MoveNext());
            asyncStreamReader.Setup(a => a.Current).Returns(() => 
            {
                // set the autoResetEvent since this path should be the last one that's reached in the background task
                _autoResetEvent.Set();
                return enumerator.Current;
            });

            var grpcEventStreamResp = new AsyncServerStreamingCall<EventStreamResponse>(
                asyncStreamReader.Object,
                null,
                null,
                null,
                null,
                null
            );

            mockGrpcClient
                .Setup(m => m.EventStream(
                    It.IsAny<Empty>(), null, null, System.Threading.CancellationToken.None))
                .Returns(grpcEventStreamResp);

            var mockCache = new Mock<ICache<string, ResolutionDetails<Model.Value>>>();
            mockCache.Setup(c => c.TryGet(It.Is<string>(s => s == "my-key"))).Returns(
                () => new ResolutionDetails<Model.Value>("my-key", new Model.Value(true))
            );

            var config = new FlagdConfig();
            config.CacheEnabled = true;
            config.MaxEventStreamRetries = 1;
            var flagdProvider = new FlagdProvider(mockGrpcClient.Object, config, mockCache.Object);

            // resolve with default set to false to make sure we return what the grpc server gives us
            var val = flagdProvider.ResolveBooleanValue("my-key", false, null);
            Assert.True(val.Result.Value);

            // wait for the autoReset event to be fired before verifying the invocation of the mocked functions
            Assert.True(_autoResetEvent.WaitOne());
            mockCache.VerifyAll();
            mockGrpcClient.VerifyAll();
        }

        [Fact]
        public void TestCacheInvalidation()
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

            var mockGrpcClient = new Mock<Service.ServiceClient>();
            mockGrpcClient
                .Setup(m => m.ResolveBooleanAsync(
                    It.IsAny<ResolveBooleanRequest>(), null, null, System.Threading.CancellationToken.None))
                .Returns(grpcResp);

            var asyncStreamReader = new Mock<IAsyncStreamReader<EventStreamResponse>>();

            var configurationChangeData = new Struct();
            var changedFlag = new Struct();
            changedFlag.Fields.Add("my-key", new Google.Protobuf.WellKnownTypes.Value());
            configurationChangeData.Fields.Add("flags", ProtoValue.ForStruct(changedFlag));


            var firstCall = true;

            asyncStreamReader.Setup(a => a.MoveNext(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(() => true);
            // as long as we did not send our first request to the provider, we will not send a configuration_change event
            // after the value of the flag has been retrieved the first time, we will send a configuration_change to test if the
            // item is deleted from the cache

            // create an autoResetEvent which we will wait for in our test verification
            AutoResetEvent _autoResetEvent = new AutoResetEvent(false);
            
            asyncStreamReader.Setup(a => a.Current).Returns(
                () => {
                    if (firstCall)
                    {
                        return new EventStreamResponse
                        {
                            Type = "provider_ready"
                        };
                    }
                    // set the autoResetEvent since this path should be the last one that's reached in the background task
                    _autoResetEvent.Set();
                    return new EventStreamResponse
                    {
                        Type = "configuration_change",
                        Data = configurationChangeData
                    };
                }
            );

            var grpcEventStreamResp = new AsyncServerStreamingCall<EventStreamResponse>(
                asyncStreamReader.Object,
                null,
                null,
                null,
                null,
                null
            );

            mockGrpcClient
                .Setup(m => m.EventStream(
                    It.IsAny<Empty>(), null, null, System.Threading.CancellationToken.None))
                .Returns(() => 
                {
                    return grpcEventStreamResp;
                });

            var mockCache = new Mock<ICache<string, ResolutionDetails<Model.Value>>>();
            mockCache.Setup(c => c.TryGet(It.Is<string>(s => s == "my-key"))).Returns(() => null);
            mockCache.Setup(c => c.Add(It.Is<string>(s => s == "my-key"), It.IsAny<ResolutionDetails<Model.Value>>()));
            mockCache.Setup(c => c.Delete(It.Is<string>(s => s == "my-key")));


            var config = new FlagdConfig();
            config.CacheEnabled = true;
            config.MaxEventStreamRetries = 1;
            var flagdProvider = new FlagdProvider(mockGrpcClient.Object, config, mockCache.Object);

            // resolve with default set to false to make sure we return what the grpc server gives us
            var val = flagdProvider.ResolveBooleanValue("my-key", false, null);
            Assert.True(val.Result.Value);

            // set firstCall to true to make the mock EventStream return a configuration_change event
            firstCall = false;

            val = flagdProvider.ResolveBooleanValue("my-key", false, null);
            Assert.True(val.Result.Value);

            Assert.True(_autoResetEvent.WaitOne());

            mockCache.VerifyAll();
            mockGrpcClient.VerifyAll();
        }
    }
}
