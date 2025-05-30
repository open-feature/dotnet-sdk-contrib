using System;
using OpenFeature.Contrib.Providers.Flagd.DependencyInjection;
using OpenFeature.DependencyInjection.Providers.Flagd;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.Test;

public class FlagdProviderOptionsExtensionsTests
{
    [Fact]
    public void Given_Null_FlagdProviderOptions_When_ToFlagdConfig_Then_ThrowsArgumentNullException()
    {
        // Arrange
        FlagdProviderOptions options = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(options.ToFlagdConfig);
    }

    [Fact]
    public void Given_Host_When_ToFlagdConfig_Then_ReturnsCorrectConfig()
    {
        // Arrange
        var options = new FlagdProviderOptions
        {
            Host = "test-host"
        };

        // Act
        var config = options.ToFlagdConfig();

        // Assert
        Assert.Equal("test-host", config.Host);
    }

    [Fact]
    public void Given_Port_When_ToFlagdConfig_Then_ReturnsCorrectConfig()
    {
        // Arrange
        var options = new FlagdProviderOptions
        {
            Port = 1234
        };

        // Act
        var config = options.ToFlagdConfig();

        // Assert
        Assert.Equal(1234, config.Port);
    }

    [Fact]
    public void Given_UseTls_When_ToFlagdConfig_Then_ReturnsCorrectConfig()
    {
        // Arrange
        var options = new FlagdProviderOptions
        {
            UseTls = true
        };

        // Act
        var config = options.ToFlagdConfig();

        // Assert
        Assert.True(config.UseTls);
    }

    [Fact]
    public void Given_CacheEnabled_When_ToFlagdConfig_Then_ReturnsCorrectConfig()
    {
        // Arrange
        var options = new FlagdProviderOptions
        {
            CacheEnabled = true
        };

        // Act
        var config = options.ToFlagdConfig();

        // Assert
        Assert.True(config.CacheEnabled);
    }

    [Fact]
    public void Given_MaxCacheSize_When_ToFlagdConfig_Then_ReturnsCorrectConfig()
    {
        // Arrange
        var options = new FlagdProviderOptions
        {
            MaxCacheSize = 42
        };

        // Act
        var config = options.ToFlagdConfig();

        // Assert
        Assert.Equal(42, config.MaxCacheSize);
    }

    [Fact]
    public void Given_CertificatePath_When_ToFlagdConfig_Then_ReturnsCorrectConfig()
    {
        // Arrange
        var options = new FlagdProviderOptions
        {
            CertificatePath = "mycert.pem"
        };

        // Act
        var config = options.ToFlagdConfig();

        // Assert
        Assert.Equal("mycert.pem", config.CertificatePath);
    }

    [Fact]
    public void Given_SocketPath_When_ToFlagdConfig_Then_ReturnsCorrectConfig()
    {
        // Arrange
        var options = new FlagdProviderOptions
        {
            SocketPath = "/tmp/socket"
        };

        // Act
        var config = options.ToFlagdConfig();

        // Assert
        Assert.Equal("/tmp/socket", config.SocketPath);
    }

    [Fact]
    public void Given_MaxEventStreamRetries_When_ToFlagdConfig_Then_ReturnsCorrectConfig()
    {
        // Arrange
        var options = new FlagdProviderOptions
        {
            MaxEventStreamRetries = 7
        };

        // Act
        var config = options.ToFlagdConfig();

        // Assert
        Assert.Equal(7, config.MaxEventStreamRetries);
    }

    [Fact]
    public void Given_ResolverType_When_ToFlagdConfig_Then_ReturnsCorrectConfig()
    {
        // Arrange
        var options = new FlagdProviderOptions
        {
            ResolverType = ResolverType.IN_PROCESS
        };

        // Act
        var config = options.ToFlagdConfig();

        // Assert
        Assert.Equal(ResolverType.IN_PROCESS, config.ResolverType);
    }

    [Fact]
    public void Given_SourceSelector_When_ToFlagdConfig_Then_ReturnsCorrectConfig()
    {
        // Arrange
        var options = new FlagdProviderOptions
        {
            SourceSelector = "my-source"
        };

        // Act
        var config = options.ToFlagdConfig();

        // Assert
        Assert.Equal("my-source", config.SourceSelector);
    }
}
