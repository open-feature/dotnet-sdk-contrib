using Microsoft.Extensions.DependencyInjection;
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
                    Host = "localhost"
                });
            })
            .BuildServiceProvider();

        // Assert
        var client = services.GetService<IFeatureClient>();
        Assert.NotNull(client);

        var provider = services.GetService<FeatureProvider>();
        Assert.NotNull(provider);
        Assert.Equal("flagd Provider", provider.GetMetadata().Name);
    }

    [Fact]
    public void AddFlagdProvider_WithConfigBuilderDelegate_ShouldReturnOpenFeatureBuilder()
    {
        // Arrange
        using var services = new ServiceCollection()
            .AddOpenFeature(builder =>
            {
                // Act
                builder.AddFlagdProvider(config => config
                    .WithHost("localhost"));
            })
            .BuildServiceProvider();

        // Assert
        var client = services.GetService<IFeatureClient>();
        Assert.NotNull(client);

        var provider = services.GetService<FeatureProvider>();
        Assert.NotNull(provider);
        Assert.Equal("flagd Provider", provider.GetMetadata().Name);
    }
}
