using System;
using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Model;
using Unleash;
using Xunit;

namespace OpenFeature.Contrib.Providers.Unleash.Test;

public class UnleashProviderConstructorTest
{
    [Fact]
    public void Constructor_WithNullSettings_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new UnleashProvider(null));
    }

    [Fact]
    public async Task InitializeAsync_WithoutBootstrap_UnreachableServer_ThrowsOnError()
    {
        var settings = new UnleashSettings
        {
            AppName = "test-app",
            UnleashApi = new Uri("http://unleash.test/api/"),
            InstanceTag = "test",
            SendMetricsInterval = null
        };

        var provider = new UnleashProvider(settings);

        var ex = await Assert.ThrowsAnyAsync<Exception>(() =>
            provider.InitializeAsync(EvaluationContext.Empty, new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token));

        Assert.NotNull(ex);
        await provider.ShutdownAsync();
    }

    [Fact]
    public async Task InitializeAsync_CancellationToken_CancelsInitialization()
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

        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            provider.InitializeAsync(EvaluationContext.Empty, cts.Token));

        await provider.ShutdownAsync();
    }
}
