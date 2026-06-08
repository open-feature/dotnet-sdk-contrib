using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Constant;
using OpenFeature.Error;
using OpenFeature.Model;
using Unleash;
using Xunit;

namespace OpenFeature.Providers.Unleash.Test;

public class UnleashProviderConstructorTest
{
    [Fact]
    public async Task InitializeAsync_WithoutBootstrap_UnreachableServer_Completes()
    {
        var settings = new UnleashSettings
        {
            AppName = "test-app",
            UnleashApi = new Uri("http://unleash.test/api/"),
            InstanceTag = "test",
            SendMetricsInterval = null
        };

        var provider = new UnleashProvider(settings);

        // Should complete without throwing (error resolves the TCS with the client)
        await provider.InitializeAsync(EvaluationContext.Empty, new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);

        // Evaluations return Unleash default behavior (false for unknown flags)
        var result = await provider.ResolveBooleanValueAsync("unknown-flag", true);
        Assert.True(result.Value); // Unleash returns the provided default when flag is unknown

        await provider.ShutdownAsync();
    }

    [Fact]
    public async Task InitializeAsync_CancellationToken_Completes()
    {
        var settings = new UnleashSettings
        {
            AppName = "test-app",
            UnleashApi = new Uri("http://unleash.test/api/"),
            InstanceTag = "test",
            SendMetricsInterval = null
        };

        var provider = new UnleashProvider(settings);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Should complete without throwing
        await provider.InitializeAsync(EvaluationContext.Empty, cts.Token);

        // Evaluations still work (client was created, just not ready)
        var result = await provider.ResolveBooleanValueAsync("flag", true);
        Assert.True(result.Value);

        await provider.ShutdownAsync();
    }

    [Fact]
    public async Task Evaluation_BeforeInitialization_ThrowsProviderNotReady()
    {
        var settings = new UnleashSettings
        {
            AppName = "test-app",
            UnleashApi = new Uri("http://unleash.test/api/"),
            InstanceTag = "test",
            SendMetricsInterval = null
        };

        var provider = new UnleashProvider(settings);

        // No InitializeAsync called — _unleash is null
        await Assert.ThrowsAsync<ProviderNotReadyException>(
            () => provider.ResolveBooleanValueAsync("flag", false));
        await Assert.ThrowsAsync<ProviderNotReadyException>(
            () => provider.ResolveStringValueAsync("flag", "default"));
        await Assert.ThrowsAsync<ProviderNotReadyException>(
            () => provider.ResolveIntegerValueAsync("flag", 7));
        await Assert.ThrowsAsync<ProviderNotReadyException>(
            () => provider.ResolveDoubleValueAsync("flag", 1.5));
        await Assert.ThrowsAsync<ProviderNotReadyException>(
            () => provider.ResolveStructureValueAsync("flag", new Value("x")));
    }
}
