using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using NSubstitute;
using OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess;
using OpenFeature.Flagd.Grpc.Sync;
using OpenFeature.Model;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.Test.Resolver.InProcess;

public class InProcessResolverTests
{
    [Fact]
    public async Task HandleEvents_CallsFlagdProviderEventHandler()
    {
        // Arrange
        var responses = new List<SyncFlagsResponse>()
        {
            new SyncFlagsResponse()
            {
                FlagConfiguration = Utils.validFlagConfig,
                SyncContext = new Google.Protobuf.WellKnownTypes.Struct()
                {
                    Fields =
                    {
                        { "source-selector", Google.Protobuf.WellKnownTypes.Value.ForString("source-selector-value") }
                    }
                }
            }
        };

        var mockJsonSchemaValidator = Substitute.For<IJsonSchemaValidator>();
        var (mockGrpcClient, asyncStreamReader) = SetupGrpcStream(responses);

        var config = FlagdConfig.Builder()
            .WithCache(true)
            .WithMaxEventStreamRetries(1)
            .WithSourceSelector("source-selector")
            .Build();

        FlagdProviderEvent flagdProviderEvent = null;
        var resolver = new InProcessResolver(mockGrpcClient, config, mockJsonSchemaValidator);
        resolver.ProviderEvent += (sender, evt) => flagdProviderEvent = evt;

        // Act
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        resolver.Init();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        // Assert
        await Utils.AssertUntilAsync((ct) => { Assert.NotNull(flagdProviderEvent); return Task.CompletedTask; });

        Assert.Equal(Constant.ProviderEventTypes.ProviderConfigurationChanged, flagdProviderEvent.EventType);
        Assert.Contains("validFlag", flagdProviderEvent.FlagsChanged);
        Assert.Contains("source-selector-value", flagdProviderEvent.SyncMetadata["source-selector"].AsString);

        await resolver.Shutdown();
    }

    [Fact]
    public async Task HandleEvents_WhenRpcErrors_CallsFlagdProviderEventHandler()
    {
        // Arrange
        var mockJsonSchemaValidator = Substitute.For<IJsonSchemaValidator>();
        var (mockGrpcClient, asyncStreamReader) = SetupGrpcStream(new List<SyncFlagsResponse>());

        asyncStreamReader.MoveNext(Arg.Any<CancellationToken>())
            .Returns(x => { throw new RpcException(new Status(StatusCode.Internal, "Unable to fetch flags")); }, x => Task.FromResult(false));

        var config = FlagdConfig.Builder()
            .WithCache(true)
            .WithMaxEventStreamRetries(1)
            .WithSourceSelector("source-selector")
            .Build();

        FlagdProviderEvent flagdProviderEvent = null;
        var resolver = new InProcessResolver(mockGrpcClient, config, mockJsonSchemaValidator);
        resolver.ProviderEvent += (sender, evt) => flagdProviderEvent = evt;

        // Act
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        resolver.Init();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        // Assert
        await Utils.AssertUntilAsync((ct) => { Assert.NotNull(flagdProviderEvent); return Task.CompletedTask; });

        Assert.Equal(Constant.ProviderEventTypes.ProviderError, flagdProviderEvent.EventType);
        Assert.Empty(flagdProviderEvent.FlagsChanged);
        Assert.Equal(Structure.Empty, flagdProviderEvent.SyncMetadata);

        await resolver.Shutdown();
    }

    [Fact]
    public async Task HandleEvents_WhenRpcCancelled_DoesNotCallFlagdProviderEventHandler()
    {
        // Arrange
        var mockJsonSchemaValidator = Substitute.For<IJsonSchemaValidator>();
        var (mockGrpcClient, asyncStreamReader) = SetupGrpcStream(new List<SyncFlagsResponse>());

        asyncStreamReader.MoveNext(Arg.Any<CancellationToken>())
            .Returns(x => { throw new RpcException(new Status(StatusCode.Cancelled, "Request cancelled")); }, x => Task.FromResult(false));

        var config = FlagdConfig.Builder()
            .WithCache(true)
            .WithMaxEventStreamRetries(1)
            .WithSourceSelector("source-selector")
            .Build();

        var counter = 0;
        var resolver = new InProcessResolver(mockGrpcClient, config, mockJsonSchemaValidator);
        resolver.ProviderEvent += (sender, evt) => { counter++; };

        // Act
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        resolver.Init();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        // Assert
        await Utils.AssertUntilAsync((ct) => { Assert.Equal(0, counter); return Task.CompletedTask; });

        await resolver.Shutdown();
    }

    private static (FlagSyncService.FlagSyncServiceClient, IAsyncStreamReader<SyncFlagsResponse>) SetupGrpcStream(List<SyncFlagsResponse> responses)
    {
        var mockGrpcClient = Substitute.For<FlagSyncService.FlagSyncServiceClient>();
        var asyncStreamReader = Substitute.For<IAsyncStreamReader<SyncFlagsResponse>>();

        var enumerator = responses.GetEnumerator();
        asyncStreamReader.MoveNext(Arg.Any<CancellationToken>()).Returns(enumerator.MoveNext());
        asyncStreamReader.Current.Returns(_ => enumerator.Current);

        var grpcEventStreamResp = new AsyncServerStreamingCall<SyncFlagsResponse>(asyncStreamReader, null, null, null, null, null);
        mockGrpcClient.SyncFlags(Arg.Any<SyncFlagsRequest>(), null, null, Arg.Any<CancellationToken>()).Returns(grpcEventStreamResp);

        return (mockGrpcClient, asyncStreamReader);
    }
}
