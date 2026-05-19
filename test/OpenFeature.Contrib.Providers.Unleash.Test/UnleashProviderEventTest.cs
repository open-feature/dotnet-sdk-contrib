using System;
using System.IO;
using System.Threading.Tasks;
using OpenFeature.Constant;
using OpenFeature.Model;
using Unleash;
using Xunit;

namespace OpenFeature.Contrib.Providers.Unleash.Test;

public class UnleashProviderEventTest : IAsyncLifetime
{
    private readonly UnleashProvider _provider;

    public UnleashProviderEventTest()
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
        UnleashProviderTest.ApplyFileSystemWorkaround(settings);

        this._provider = new UnleashProvider(settings);
    }

    public Task InitializeAsync() => this._provider.InitializeAsync(EvaluationContext.Empty);
    public Task DisposeAsync() => this._provider.ShutdownAsync();

    [Fact]
    public void EmitConfigurationChanged_PublishesEventToChannel()
    {
        // Act
        this._provider.EmitConfigurationChanged();

        // Assert
        var channel = this._provider.GetEventChannel();
        var eventPublished = channel.Reader.TryRead(out var item);
        Assert.True(eventPublished);

        var providerEvent = item as ProviderEventPayload;
        Assert.NotNull(providerEvent);
        Assert.Equal(ProviderEventTypes.ProviderConfigurationChanged, providerEvent.Type);
        Assert.Equal("Unleash Provider", providerEvent.ProviderName);
    }

    [Fact]
    public void EmitConfigurationChanged_CalledTwice_SecondEventDroppedWhenChannelFull()
    {
        // Act
        this._provider.EmitConfigurationChanged();
        this._provider.EmitConfigurationChanged();

        // Assert: channel is bounded(1), so only one event is available
        var channel = this._provider.GetEventChannel();
        var firstRead = channel.Reader.TryRead(out _);
        var secondRead = channel.Reader.TryRead(out _);
        Assert.True(firstRead);
        Assert.False(secondRead);
    }
}
