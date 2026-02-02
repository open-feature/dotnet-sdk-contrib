using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OpenFeature.Constant;
using OpenFeature.Contrib.Providers.Flagd.Resolver.Rpc;
using OpenFeature.Error;
using OpenFeature.Flagd.Grpc.Evaluation;
using OpenFeature.Model;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.Test.Resolver.Rpc;

public class RpcResolverTests
{
    private const int TestTimeoutMilliseconds = 10_000;

    [Fact]
    public async Task HandleEvents_CallsFlagdProviderEventHandler()
    {
        // Arrange
        var listFlag = new List<Google.Protobuf.WellKnownTypes.Value>()
        {
            Google.Protobuf.WellKnownTypes.Value.ForString("list_value1"),
            Google.Protobuf.WellKnownTypes.Value.ForBool(false),
        };
        var structFlag = new Google.Protobuf.WellKnownTypes.Struct()
        {
            Fields =
            {
                { "innerKey1", Google.Protobuf.WellKnownTypes.Value.ForString("innerValue1") },
                { "innerKey2", Google.Protobuf.WellKnownTypes.Value.ForNumber(42) },
            }
        };
        var flags = new Google.Protobuf.WellKnownTypes.Struct()
        {
            Fields =
            {
                { "key1", Google.Protobuf.WellKnownTypes.Value.ForString("value1") },
                { "key2", Google.Protobuf.WellKnownTypes.Value.ForBool(true) },
                { "key3", Google.Protobuf.WellKnownTypes.Value.ForNull() },
                { "key4", Google.Protobuf.WellKnownTypes.Value.ForList(listFlag.ToArray()) },
                { "key5", Google.Protobuf.WellKnownTypes.Value.ForNumber(1.0) },
                { "key6", Google.Protobuf.WellKnownTypes.Value.ForStruct(structFlag) }
            }
        };
        var responses = new List<EventStreamResponse>()
        {
            new EventStreamResponse()
            {
                Data = new Google.Protobuf.WellKnownTypes.Struct()
                {
                    Fields = { { "flags", Google.Protobuf.WellKnownTypes.Value.ForStruct(flags) } }
                },
                Type = "provider_ready"
            }
        };

        var autoResetEvent = new AutoResetEvent(false);
        var mockGrpcClient = SetupGrpcStream(responses);

        var config = new FlagdConfig();

        FlagdProviderEvent flagdProviderEvent = null;
        var resolver = new RpcResolver(mockGrpcClient, config, null);
        resolver.ProviderEvent += (sender, args) => { flagdProviderEvent = args; autoResetEvent.Set(); };

        // Act
        await resolver.Init();

        // Assert
        Assert.True(autoResetEvent.WaitOne(TestTimeoutMilliseconds));

        Assert.NotNull(flagdProviderEvent);
        Assert.Equal(ProviderEventTypes.ProviderReady, flagdProviderEvent.EventType);
        Assert.Contains("key1", flagdProviderEvent.FlagsChanged);
        Assert.Contains("key1", flagdProviderEvent.FlagsChanged);
        Assert.Contains("key2", flagdProviderEvent.FlagsChanged);
        Assert.Contains("key3", flagdProviderEvent.FlagsChanged);
        Assert.Contains("key4", flagdProviderEvent.FlagsChanged);
        Assert.Contains("key5", flagdProviderEvent.FlagsChanged);
        Assert.Contains("key6", flagdProviderEvent.FlagsChanged);
        Assert.Equal(Structure.Empty, flagdProviderEvent.SyncMetadata);
    }

    [Fact]
    public async Task HandleEvents_WithNoFlags_CallsFlagdProviderEventHandler()
    {
        // Arrange
        var responses = new List<EventStreamResponse>()
        {
            new EventStreamResponse()
            {
                Data = new Google.Protobuf.WellKnownTypes.Struct(),
                Type = "provider_ready"
            }
        };

        var autoResetEvent = new AutoResetEvent(false);
        var mockGrpcClient = SetupGrpcStream(responses);

        var config = new FlagdConfig();

        FlagdProviderEvent flagdProviderEvent = null;
        var resolver = new RpcResolver(mockGrpcClient, config, null);
        resolver.ProviderEvent += (sender, args) => { flagdProviderEvent = args; autoResetEvent.Set(); };

        // Act
        await resolver.Init();

        // Assert
        Assert.True(autoResetEvent.WaitOne(TestTimeoutMilliseconds));

        Assert.NotNull(flagdProviderEvent);
        Assert.Equal(ProviderEventTypes.ProviderReady, flagdProviderEvent.EventType);
        Assert.Empty(flagdProviderEvent.FlagsChanged);
        Assert.Equal(Structure.Empty, flagdProviderEvent.SyncMetadata);
    }

    [Fact]
    public async Task HandleEvents_WhenConfigChanged_CallsFlagdProviderEventHandler()
    {
        // Arrange
        var flags = new Google.Protobuf.WellKnownTypes.Struct()
        {
            Fields = { { "key1", Google.Protobuf.WellKnownTypes.Value.ForString("value1") } }
        };
        var responses = new List<EventStreamResponse>()
        {
            new EventStreamResponse()
            {
                Data = new Google.Protobuf.WellKnownTypes.Struct()
                {
                    Fields = { { "flags", Google.Protobuf.WellKnownTypes.Value.ForStruct(flags) } }
                },
                Type = "configuration_change"
            }
        };

        var autoResetEvent = new AutoResetEvent(false);
        var mockGrpcClient = SetupGrpcStream(responses);

        var config = new FlagdConfig();

        FlagdProviderEvent flagdProviderEvent = null;
        var resolver = new RpcResolver(mockGrpcClient, config, null);
        resolver.ProviderEvent += (sender, args) => { flagdProviderEvent = args; autoResetEvent.Set(); };

        // Act
        await resolver.Init();

        // Assert
        Assert.True(autoResetEvent.WaitOne(TestTimeoutMilliseconds));

        Assert.NotNull(flagdProviderEvent);
        Assert.Equal(ProviderEventTypes.ProviderConfigurationChanged, flagdProviderEvent.EventType);
        Assert.Contains("key1", flagdProviderEvent.FlagsChanged);
        Assert.Equal(Structure.Empty, flagdProviderEvent.SyncMetadata);
    }

    [Fact]
    public async Task HandleEvents_PurgesCache()
    {
        // Arrange
        var flags = new Google.Protobuf.WellKnownTypes.Struct()
        {
            Fields = { { "key1", Google.Protobuf.WellKnownTypes.Value.ForString("value1") } }
        };
        var responses = new List<EventStreamResponse>()
        {
            new EventStreamResponse()
            {
                Data = new Google.Protobuf.WellKnownTypes.Struct()
                {
                    Fields = { { "flags", Google.Protobuf.WellKnownTypes.Value.ForStruct(flags) } }
                },
                Type = "provider_ready"
            }
        };

        var autoResetEvent = new AutoResetEvent(false);
        var mockGrpcClient = SetupGrpcStream(responses);

        var config = new FlagdConfig()
        {
            CacheEnabled = true
        };

        var mockCache = Substitute.For<ICache<string, object>>();
        mockCache.TryGet(Arg.Is<string>(s => s == "key1")).Returns(null);
        mockCache.Add(Arg.Is<string>(s => s == "key1"), Arg.Any<object>());
        mockCache.When(x => x.Purge()).Do(_ => { autoResetEvent.Set(); });

        var resolver = new RpcResolver(mockGrpcClient, config, mockCache);

        // Act
        await resolver.Init();

        // Assert
        Assert.True(autoResetEvent.WaitOne(TestTimeoutMilliseconds));

        mockCache.Received().Purge();
    }

    [Fact]
    public async Task HandleEvents_WhenConfigChanged_DeletesCacheItem()
    {
        // Arrange
        var flags = new Google.Protobuf.WellKnownTypes.Struct()
        {
            Fields = { { "key1", Google.Protobuf.WellKnownTypes.Value.ForString("value1") } }
        };
        var responses = new List<EventStreamResponse>()
        {
            new EventStreamResponse()
            {
                Data = new Google.Protobuf.WellKnownTypes.Struct()
                {
                    Fields = { { "flags", Google.Protobuf.WellKnownTypes.Value.ForStruct(flags) } }
                },
                Type = "configuration_change"
            }
        };

        var autoResetEvent = new AutoResetEvent(false);
        var mockGrpcClient = SetupGrpcStream(responses);

        var config = new FlagdConfig()
        {
            CacheEnabled = true
        };

        var mockCache = Substitute.For<ICache<string, object>>();
        mockCache.TryGet(Arg.Is<string>(s => s == "key1")).Returns(null);
        mockCache.Add(Arg.Is<string>(s => s == "key1"), Arg.Any<object>());
        mockCache.When(x => x.Delete("key1")).Do(_ => { autoResetEvent.Set(); });

        var resolver = new RpcResolver(mockGrpcClient, config, mockCache);

        // Act
        await resolver.Init();

        // Assert
        Assert.True(autoResetEvent.WaitOne(TestTimeoutMilliseconds));

        mockCache.Received().Delete("key1");
    }

    [Theory]
    [MemberData(nameof(ResolveValueDataLossData))]
    internal async Task ResolveValue_WhenDataLossError_ReturnsParseError(Func<RpcResolver, Task> act, Action<Service.ServiceClient> setup)
    {
        // Arrange
        var mockGrpcClient = Substitute.For<Service.ServiceClient>();
        setup(mockGrpcClient);

        var config = new FlagdConfig();
        var resolver = new RpcResolver(mockGrpcClient, config, null);

        // Act
        var ex = await Assert.ThrowsAsync<FeatureProviderException>(() => act(resolver));

        // Assert
        Assert.Equal(ErrorType.ParseError, ex.ErrorType);
        Assert.Equal("Parse error", ex.Message);
    }

    public static IEnumerable<object[]> ResolveValueDataLossData()
    {
        const string flagKey = "key";
        const string errorMessage = "Parse error";
        var rpcException = new RpcException(new Status(StatusCode.DataLoss, errorMessage));

        yield return new object[]
        {
            new Func<RpcResolver, Task>(r => r.ResolveBooleanValueAsync(flagKey, false)),
            new Action<Service.ServiceClient>(client => client.ResolveBooleanAsync(Arg.Any<ResolveBooleanRequest>()).Throws(rpcException))
        };
        yield return new object[]
        {
            new Func<RpcResolver, Task>(r => r.ResolveStringValueAsync(flagKey, "def")),
            new Action<Service.ServiceClient>(client => client.ResolveStringAsync(Arg.Any<ResolveStringRequest>()).Throws(rpcException))
        };
        yield return new object[]
        {
            new Func<RpcResolver, Task>(r => r.ResolveIntegerValueAsync(flagKey, 3)),
            new Action<Service.ServiceClient>(client => client.ResolveIntAsync(Arg.Any<ResolveIntRequest>()).Throws(rpcException))
        };
        yield return new object[]
        {
            new Func<RpcResolver, Task>(r => r.ResolveDoubleValueAsync(flagKey, 3.5)),
            new Action<Service.ServiceClient>(client => client.ResolveFloatAsync(Arg.Any<ResolveFloatRequest>()).Throws(rpcException))
        };
        yield return new object[]
        {
            new Func<RpcResolver, Task>(r => r.ResolveStructureValueAsync(flagKey, new Value(Structure.Builder().Set("value1", true).Build()))),
            new Action<Service.ServiceClient>(client => client.ResolveObjectAsync(Arg.Any<ResolveObjectRequest>()).Throws(rpcException))
        };
    }

    [Theory]
    [MemberData(nameof(ResolveValueFlagdMetadata))]
    internal async Task ResolveValueAsync_AddsFlagMetadata<T>(Func<RpcResolver, Task<ResolutionDetails<T>>> act,
        Action<Service.ServiceClient, Google.Protobuf.WellKnownTypes.Struct> setup)
    {
        // Arrange
        var mockGrpcClient = Substitute.For<Service.ServiceClient>();

        var setupMetadata = new Google.Protobuf.WellKnownTypes.Struct()
        {
            Fields =
                {
                    { "key1", Google.Protobuf.WellKnownTypes.Value.ForString("value1") },
                    { "key2", Google.Protobuf.WellKnownTypes.Value.ForString(string.Empty) },
                    { "key3", Google.Protobuf.WellKnownTypes.Value.ForBool(true) },
                    { "key4", Google.Protobuf.WellKnownTypes.Value.ForBool(false) },
                    { "key5", Google.Protobuf.WellKnownTypes.Value.ForNumber(1) },
                    { "key6", Google.Protobuf.WellKnownTypes.Value.ForNumber(3.14) },
                    { "key7", Google.Protobuf.WellKnownTypes.Value.ForNumber(-0.531921) },
                    { "key8", Google.Protobuf.WellKnownTypes.Value.ForList(Google.Protobuf.WellKnownTypes.Value.ForString("1"), Google.Protobuf.WellKnownTypes.Value.ForString("2")) },
                    { "key9", Google.Protobuf.WellKnownTypes.Value.ForNull() },
                    { "key10", Google.Protobuf.WellKnownTypes.Value.ForStruct(new Google.Protobuf.WellKnownTypes.Struct()
                    {
                        Fields = { { "innerkey", Google.Protobuf.WellKnownTypes.Value.ForBool(true) } }
                    }) },
                    { "key11", Google.Protobuf.WellKnownTypes.Value.ForNumber(int.MaxValue) }
                }
        };

        setup(mockGrpcClient, setupMetadata);

        var config = new FlagdConfig();
        var resolver = new RpcResolver(mockGrpcClient, config, null);

        // Act
        var value = await act(resolver);

        // Assert
        var metadata = value.FlagMetadata;
        Assert.NotNull(metadata);
        Assert.Equal("value1", metadata.GetString("key1"));
        Assert.Equal(string.Empty, metadata.GetString("key2"));
        Assert.True(metadata.GetBool("key3"));
        Assert.False(metadata.GetBool("key4"));
        Assert.Equal(1, metadata.GetInt("key5"));
        Assert.Equal(3.14, metadata.GetDouble("key6"));
        Assert.Equal(-0.531921, metadata.GetDouble("key7"));
        Assert.Null(metadata.GetString("key8"));
        Assert.Null(metadata.GetString("key9"));
        Assert.Null(metadata.GetString("key10"));
        Assert.Equal(int.MaxValue, metadata.GetInt("key11"));
    }

    public static IEnumerable<object[]> ResolveValueFlagdMetadata()
    {
        const string flagKey = "test-key";

        yield return new object[]
        {
            new Func<RpcResolver, Task<ResolutionDetails<bool>>>(r => r.ResolveBooleanValueAsync(flagKey, false)),
            new Action<Service.ServiceClient, Google.Protobuf.WellKnownTypes.Struct>((client, metadata) => client.ResolveBooleanAsync(Arg.Any<ResolveBooleanRequest>())
                .Returns(CreateRpcResponse(new ResolveBooleanResponse() { Value = true, Variant = "true", Reason = "TARGETING_MATCH", Metadata = metadata })))
        };
        yield return new object[]
        {
            new Func<RpcResolver, Task<ResolutionDetails<string>>>(r => r.ResolveStringValueAsync(flagKey, "def")),
            new Action<Service.ServiceClient, Google.Protobuf.WellKnownTypes.Struct>((client, metadata) => client.ResolveStringAsync(Arg.Any<ResolveStringRequest>())
                .Returns(CreateRpcResponse(new ResolveStringResponse() { Value = "one", Variant = "default", Reason = "TARGETING_MATCH", Metadata = metadata })))
        };
        yield return new object[]
        {
            new Func<RpcResolver, Task<ResolutionDetails<int>>>(r => r.ResolveIntegerValueAsync(flagKey, 3)),
            new Action<Service.ServiceClient, Google.Protobuf.WellKnownTypes.Struct>((client, metadata) => client.ResolveIntAsync(Arg.Any<ResolveIntRequest>())
                .Returns(CreateRpcResponse(new ResolveIntResponse() { Value = 1, Variant = "one", Reason = "TARGETING_MATCH", Metadata = metadata })))
        };
        yield return new object[]
        {
            new Func<RpcResolver, Task<ResolutionDetails<double>>>(r => r.ResolveDoubleValueAsync(flagKey, 3.5)),
            new Action<Service.ServiceClient, Google.Protobuf.WellKnownTypes.Struct>((client, metadata) => client.ResolveFloatAsync(Arg.Any<ResolveFloatRequest>())
                .Returns(CreateRpcResponse(new ResolveFloatResponse() { Value = 1.61, Variant = "one", Reason = "TARGETING_MATCH", Metadata = metadata })))
        };
        yield return new object[]
        {
            new Func<RpcResolver, Task<ResolutionDetails<Value>>>(r => r.ResolveStructureValueAsync(flagKey, new Value(Structure.Builder().Set("value1", true).Build()))),
            new Action<Service.ServiceClient, Google.Protobuf.WellKnownTypes.Struct>((client, metadata) => client.ResolveObjectAsync(Arg.Any<ResolveObjectRequest>())
                .Returns(CreateRpcResponse(new ResolveObjectResponse()
                {
                    Value = new Google.Protobuf.WellKnownTypes.Struct(),
                    Variant = "one",
                    Reason = "TARGETING_MATCH",
                    Metadata = metadata
                })))
        };
    }

    private static AsyncUnaryCall<T> CreateRpcResponse<T>(T resp)
        where T : IMessage<T>, IBufferMessage
    {
        return new AsyncUnaryCall<T>(Task.FromResult(resp), Task.FromResult(Grpc.Core.Metadata.Empty), () => Status.DefaultSuccess, () => Grpc.Core.Metadata.Empty, () => { });
    }

    private static Service.ServiceClient SetupGrpcStream(List<EventStreamResponse> responses)
    {
        var mockGrpcClient = Substitute.For<Service.ServiceClient>();
        var asyncStreamReader = Substitute.For<IAsyncStreamReader<EventStreamResponse>>();

        var enumerator = responses.GetEnumerator();
        asyncStreamReader.MoveNext(Arg.Any<CancellationToken>()).Returns(enumerator.MoveNext());
        asyncStreamReader.Current.Returns(_ => enumerator.Current);

        var grpcEventStreamResp = new AsyncServerStreamingCall<EventStreamResponse>(asyncStreamReader, null, null, null, null, null);
        mockGrpcClient.EventStream(Arg.Any<EventStreamRequest>(), null, null, CancellationToken.None)
            .Returns(grpcEventStreamResp);

        return mockGrpcClient;
    }
}
