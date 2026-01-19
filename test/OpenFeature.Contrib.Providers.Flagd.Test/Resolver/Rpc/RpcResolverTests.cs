using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
