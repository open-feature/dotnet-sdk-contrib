using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Constant;
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
    public async Task Evaluation_BeforeInitialization_ReturnsProviderNotReady()
    {
        var settings = new UnleashSettings
        {
            AppName = "test-app",
            UnleashApi = new Uri("http://unleash.test/api/"),
            InstanceTag = "test",
            SendMetricsInterval = null
        };

        var provider = new UnleashProvider(settings);

        // No InitializeAsync called — _clientTcs is null
        var boolResult = await provider.ResolveBooleanValueAsync("flag", false);
        Assert.False(boolResult.Value);
        Assert.Equal(Reason.Error, boolResult.Reason);
        Assert.Equal(ErrorType.ProviderNotReady, boolResult.ErrorType);

        var stringResult = await provider.ResolveStringValueAsync("flag", "default");
        Assert.Equal("default", stringResult.Value);
        Assert.Equal(ErrorType.ProviderNotReady, stringResult.ErrorType);

        var intResult = await provider.ResolveIntegerValueAsync("flag", 7);
        Assert.Equal(7, intResult.Value);
        Assert.Equal(ErrorType.ProviderNotReady, intResult.ErrorType);

        var doubleResult = await provider.ResolveDoubleValueAsync("flag", 1.5);
        Assert.Equal(1.5, doubleResult.Value);
        Assert.Equal(ErrorType.ProviderNotReady, doubleResult.ErrorType);

        var structResult = await provider.ResolveStructureValueAsync("flag", new Value("x"));
        Assert.Equal("x", structResult.Value.AsString);
        Assert.Equal(ErrorType.ProviderNotReady, structResult.ErrorType);
    }

    [Fact]
    public async Task Evaluation_AfterShutdown_ReturnsProviderNotReady()
    {
        var bootstrapPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "bootstrap.json");
        var settings = new UnleashSettings
        {
            AppName = "test-app",
            UnleashApi = new Uri("http://unleash.test/api/"),
            InstanceTag = "test",
            SendMetricsInterval = null
        };
        settings.UseBootstrapFileProvider(bootstrapPath);

        var provider = new UnleashProvider(settings);
        await provider.InitializeAsync(EvaluationContext.Empty);

        // Verify the provider is working before shutdown
        var result = await provider.ResolveBooleanValueAsync("boolean-flag", false);
        Assert.True(result.Value);

        await provider.ShutdownAsync();

        // After shutdown, all evaluations should return ProviderNotReady
        var boolResult = await provider.ResolveBooleanValueAsync("boolean-flag", false);
        Assert.False(boolResult.Value);
        Assert.Equal(Reason.Error, boolResult.Reason);
        Assert.Equal(ErrorType.ProviderNotReady, boolResult.ErrorType);

        var stringResult = await provider.ResolveStringValueAsync("flag", "default");
        Assert.Equal("default", stringResult.Value);
        Assert.Equal(ErrorType.ProviderNotReady, stringResult.ErrorType);

        var intResult = await provider.ResolveIntegerValueAsync("flag", 7);
        Assert.Equal(7, intResult.Value);
        Assert.Equal(ErrorType.ProviderNotReady, intResult.ErrorType);

        var doubleResult = await provider.ResolveDoubleValueAsync("flag", 1.5);
        Assert.Equal(1.5, doubleResult.Value);
        Assert.Equal(ErrorType.ProviderNotReady, doubleResult.ErrorType);

        var structResult = await provider.ResolveStructureValueAsync("flag", new Value("x"));
        Assert.Equal("x", structResult.Value.AsString);
        Assert.Equal(ErrorType.ProviderNotReady, structResult.ErrorType);
    }

    [Fact]
    public async Task SetReady_BeforeInitialize_MakesClientAvailable()
    {
        var bootstrapPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "bootstrap.json");
        var recoverySettings = new UnleashSettings
        {
            AppName = "test-app",
            UnleashApi = new Uri("http://unleash.test/api/"),
            InstanceTag = "test",
            SendMetricsInterval = null
        };
        recoverySettings.UseBootstrapFileProvider(bootstrapPath);
        var client = new DefaultUnleash(recoverySettings);

        var settings = new UnleashSettings
        {
            AppName = "test-app",
            UnleashApi = new Uri("http://unleash.test/api/"),
            InstanceTag = "test",
            SendMetricsInterval = null
        };

        var provider = new UnleashProvider(settings);
        provider.SetReady(client);

        // Evaluation should use the provided client with bootstrap data
        var result = await provider.ResolveBooleanValueAsync("boolean-flag", false);
        Assert.True(result.Value);

        await provider.ShutdownAsync();
    }
}
