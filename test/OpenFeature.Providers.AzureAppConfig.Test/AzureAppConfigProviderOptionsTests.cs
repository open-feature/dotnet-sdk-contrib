using Xunit;

namespace OpenFeature.Providers.AzureAppConfig.Test;

public class AzureAppConfigProviderOptionsTests
{
    [Fact]
    public void DefaultValues_ShouldBeSetCorrectly()
    {
        // Act
        var options = new AzureAppConfigProviderOptions();

        // Assert
        Assert.Equal(".appconfig.featureflag/", options.FeatureFlagPrefix);
        Assert.Null(options.Label);
    }

    [Theory]
    [InlineData("custom.prefix/")]
    [InlineData("")]
    [InlineData("my-app-")]
    public void FeatureFlagPrefix_ShouldBeSettable(string prefix)
    {
        // Act
        var options = new AzureAppConfigProviderOptions { FeatureFlagPrefix = prefix };

        // Assert
        Assert.Equal(prefix, options.FeatureFlagPrefix);
    }

    [Theory]
    [InlineData("production")]
    [InlineData("staging")]
    [InlineData(null)]
    public void Label_ShouldBeSettable(string label)
    {
        // Act
        var options = new AzureAppConfigProviderOptions { Label = label };

        // Assert
        Assert.Equal(label, options.Label);
    }
}
