using Xunit;
using Moq;
using Schema.V1;
using Grpc.Core;
using Google.Protobuf.WellKnownTypes;
using ProtoValue = Google.Protobuf.WellKnownTypes.Value;

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

            var flagdProvider = new FlagdProvider(mockGrpcClient.Object);

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

            var flagdProvider = new FlagdProvider(mockGrpcClient.Object);

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

            var flagdProvider = new FlagdProvider(mockGrpcClient.Object);

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

            var flagdProvider = new FlagdProvider(mockGrpcClient.Object);

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

            var flagdProvider = new FlagdProvider(mockGrpcClient.Object);

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

            var flagdProvider = new FlagdProvider(mockGrpcClient.Object);

            // resolve with default set to true
            var val = flagdProvider.ResolveBooleanValue("my-key", true, null);

            // here we should get the default value regardless of the failed grpc call
            Assert.True(val.Result.Value);
            // check for error information to be included in the result
            Assert.Equal(Constant.Reason.Error, val.Result.Reason);
            Assert.Equal(Constant.ErrorType.FlagNotFound, val.Result.ErrorType);
            Assert.Equal(Constant.ErrorType.FlagNotFound.ToString(), val.Result.ErrorMessage);
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

            var flagdProvider = new FlagdProvider(mockGrpcClient.Object);

            // resolve with default set to true
            var val = flagdProvider.ResolveBooleanValue("my-key", true, null);

            // here we should get the default value regardless of the failed grpc call
            Assert.True(val.Result.Value);
            // check for error information to be included in the result
            Assert.Equal(Constant.Reason.Error, val.Result.Reason);
            Assert.Equal(Constant.ErrorType.ProviderNotReady, val.Result.ErrorType);
            Assert.Equal(Constant.ErrorType.ProviderNotReady.ToString(), val.Result.ErrorMessage);
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

            var flagdProvider = new FlagdProvider(mockGrpcClient.Object);

            // resolve with default set to true
            var val = flagdProvider.ResolveBooleanValue("my-key", true, null);

            // here we should get the default value regardless of the failed grpc call
            Assert.True(val.Result.Value);
            // check for error information to be included in the result
            Assert.Equal(Constant.Reason.Error, val.Result.Reason);
            Assert.Equal(Constant.ErrorType.TypeMismatch, val.Result.ErrorType);
            Assert.Equal(Constant.ErrorType.TypeMismatch.ToString(), val.Result.ErrorMessage);
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

            var flagdProvider = new FlagdProvider(mockGrpcClient.Object);

            // resolve with default set to true
            var val = flagdProvider.ResolveBooleanValue("my-key", true, null);

            // here we should get the default value regardless of the failed grpc call
            Assert.True(val.Result.Value);
            // check for error information to be included in the result
            Assert.Equal(Constant.Reason.Error, val.Result.Reason);
            Assert.Equal(Constant.ErrorType.General, val.Result.ErrorType);
            Assert.Equal("unknown error", val.Result.ErrorMessage);
        }
    }
}
