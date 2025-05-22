using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenFeature.DependencyInjection.Providers.Flagd;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.Test;

public class FeatureBuilderExtensionsTests
{
    [Fact]
    public void AddFlagdProvider_WithNoConfiguration_ShouldReturnOpenFeatureBuilder()
    {
        // Arrange
        using var services = new ServiceCollection()
            .AddOpenFeature(builder =>
            {
                // Act
                builder.AddFlagdProvider();
            })
            .BuildServiceProvider();

        // Assert
        var client = services.GetService<IFeatureClient>();
        Assert.NotNull(client);

        var provider = services.GetService<FeatureProvider>();
        Assert.NotNull(provider);
        Assert.Equal("flagd Provider", provider.GetMetadata().Name);
        Assert.IsType<FlagdProvider>(provider);

        var flagdProvider = (FlagdProvider)provider;
        var config = flagdProvider.GetConfig();
        Assert.NotNull(config);
        Assert.Multiple(
            () => Assert.Equal("localhost", config.Host),
            () => Assert.Equal(8013, config.Port),
            () => Assert.False(config.UseTls, "UseTls is disabled by default"),
            () => Assert.False(config.CacheEnabled, "CacheEnabled is disabled by default"),
            () => Assert.Equal(10, config.MaxCacheSize),
            () => Assert.Equal(string.Empty, config.CertificatePath),
            () => Assert.Equal(string.Empty, config.SocketPath),
            () => Assert.Equal(3, config.MaxEventStreamRetries),
            () => Assert.Equal(ResolverType.RPC, config.ResolverType),
            () => Assert.Equal(string.Empty, config.SourceSelector)
        );
    }

    [Fact]
    public void AddFlagdProvider_WithDomain_And_NoConfiguration_ShouldReturnOpenFeatureBuilder()
    {
        // Arrange
        using var services = new ServiceCollection()
            .AddOpenFeature(builder =>
            {
                // Act
                builder.AddFlagdProvider("test-domain");
            })
            .BuildServiceProvider();

        // Assert
        var client = services.GetService<IFeatureClient>();
        Assert.NotNull(client);

        var provider = services.GetKeyedService<FeatureProvider>("test-domain");
        Assert.NotNull(provider);
    }

    [Fact]
    public void AddFlagdProvider_ShouldReturnOpenFeatureBuilder()
    {
        // Arrange
        using var services = new ServiceCollection()
            .AddOpenFeature(builder =>
            {
                // Act
                builder.AddFlagdProvider(new FlagdProviderOptions
                {
                    Host = "flagdtest",
                    Port = 1234,
                    UseTls = true,
                    CacheEnabled = true,
                    MaxCacheSize = 500,
                    CertificatePath = "mycert.pem",
#if NET8_0_OR_GREATER
                    SocketPath = "tmp.sock",
#endif
                    MaxEventStreamRetries = -1,
                    ResolverType = ResolverType.IN_PROCESS,
                    SourceSelector = "source-selector"
                });
            })
            .BuildServiceProvider();

        // Assert
        var client = services.GetService<IFeatureClient>();
        Assert.NotNull(client);

        var provider = services.GetService<FeatureProvider>();
        Assert.NotNull(provider);
        Assert.Equal("flagd Provider", provider.GetMetadata().Name);
        Assert.IsType<FlagdProvider>(provider);

        var flagdProvider = (FlagdProvider)provider;
        var config = flagdProvider.GetConfig();
        Assert.NotNull(config);
        Assert.Multiple(
            () => Assert.Equal("flagdtest", config.Host),
            () => Assert.Equal(1234, config.Port),
            () => Assert.True(config.UseTls),
            () => Assert.True(config.CacheEnabled),
            () => Assert.Equal(500, config.MaxCacheSize),
            () => Assert.Equal("mycert.pem", config.CertificatePath),
#if NET8_0_OR_GREATER
            () => Assert.Equal("tmp.sock", config.SocketPath),
#endif
            () => Assert.Equal(-1, config.MaxEventStreamRetries),
            () => Assert.Equal(ResolverType.IN_PROCESS, config.ResolverType),
            () => Assert.Equal("source-selector", config.SourceSelector)
        );
    }

    [Fact]
    public void AddFlagdProvider_WithDomain_ShouldReturnOpenFeatureBuilder()
    {
        // Arrange
        using var services = new ServiceCollection()
            .AddOpenFeature(builder =>
            {
                // Act
                builder.AddFlagdProvider("test-domain", new FlagdProviderOptions
                {
                    Host = "flagdtest",
                    Port = 1234,
                    UseTls = true,
                    CacheEnabled = true,
                    MaxCacheSize = 500,
                    CertificatePath = "mycert.pem",
#if NET8_0_OR_GREATER
                    SocketPath = "tmp.sock",
#endif
                    MaxEventStreamRetries = -1,
                    ResolverType = ResolverType.IN_PROCESS,
                    SourceSelector = "source-selector"
                });
            })
            .BuildServiceProvider();

        // Assert
        var provider = services.GetKeyedService<FeatureProvider>("test-domain");
        Assert.NotNull(provider);
    }

    [Fact]
    public void AddFlagdProvider_WithNoLoggingServicesRegistered_AddsNullLoggerToConfig()
    {
        // Arrange
        using var services = new ServiceCollection()
            .AddOpenFeature(builder =>
            {
                // Act
                builder.AddFlagdProvider();
            })
            .BuildServiceProvider();

        // Assert
        var config = GetFlagdProviderConfig(services);
        Assert.IsType<NullLogger<FlagdProvider>>(config.Logger);
    }

    [Fact]
    public void AddFlagdProvider_WithLoggingServicesRegistered_AddsFlagdLoggerToConfig()
    {
        // Arrange
        using var services = new ServiceCollection()
            .AddLogging()
            .AddOpenFeature(builder =>
            {
                // Act
                builder.AddFlagdProvider();
            })
            .BuildServiceProvider();

        // Assert
        var config = GetFlagdProviderConfig(services);
        Assert.IsType<ILogger<FlagdProvider>>(config.Logger, exactMatch: false);
    }

    private static FlagdConfig GetFlagdProviderConfig(ServiceProvider services)
    {
        var provider = services.GetService<FeatureProvider>();
        var flagdProvider = (FlagdProvider)provider;
        return flagdProvider.GetConfig();
    }
}
