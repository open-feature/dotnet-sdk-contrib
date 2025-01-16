using Xunit;
using OpenFeature.Contrib.Providers.AwsAppConfig;
using System;

public class AppConfigKeyTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldSetProperties()
    {
        // Arrange
        var configProfileID = "TestConfigProfile";
        var flagKey = "TestFlagKey";
        var attributeKey = "TestAttributeKey";

        // Act
        var key = new AppConfigKey(configProfileID, flagKey, attributeKey);

        // Assert
        Assert.Equal(configProfileID, key.ConfigurationProfileId);
        Assert.Equal(flagKey, key.FlagKey);
        Assert.Equal(attributeKey, key.AttributeKey);
    }

    [Theory]
    [InlineData("", "env", "config")]
    [InlineData("app", "", "config")]
    [InlineData("app", "env", "")]
    [InlineData(null, "env", "config")]
    [InlineData("app", null, "config")]
    [InlineData("app", "env", null)]
    public void Constructor_WithInvalidParameters_ShouldThrowArgumentException(
        string application, string environment, string configuration)
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new AppConfigKey(application, environment, configuration));
    }

    [Theory]
    [InlineData("app1", "env1", "config1")]
    [InlineData("my-app", "my-env", "my-config")]
    [InlineData("APP", "ENV", "CONFIG")]
    public void ToString_ShouldReturnFormattedString(
        string configProfileId, string flagKey, string attributeKey)
    {
        // Arrange
        var key = new AppConfigKey(configProfileId, flagKey, attributeKey);

        // Act
        var result = key.ToString();

        // Assert
        Assert.Contains(configProfileId, result);
        Assert.Contains(flagKey, result);
        Assert.Contains(attributeKey, result);
    }

    [Theory]
    [InlineData("app-123", "env-123", "config-123")]
    [InlineData("app_123", "env_123", "config_123")]
    [InlineData("app.123", "env.123", "config.123")]
    public void Constructor_WithSpecialCharacters_ShouldAcceptValidPatterns(
        string configProfileId, string flagKey, string attributeKey)
    {
        // Arrange & Act
        var key = new AppConfigKey(configProfileId, flagKey, attributeKey);

        // Assert
        Assert.Equal(configProfileId, key.ConfigurationProfileId);
        Assert.Equal(flagKey, key.FlagKey);
        Assert.Equal(attributeKey, key.AttributeKey);
    }

    [Theory]
    [InlineData("app$123", "env", "config")]
    [InlineData("app", "env#123", "config")]
    [InlineData("app", "env", "config@123")]
    public void Constructor_WithInvalidCharacters_ShouldThrowArgumentException(
        string application, string environment, string configuration)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new AppConfigKey(application, environment, configuration));
    }

    [Fact]
    public void Constructor_WithWhitespaceValues_ShouldThrowArgumentException()
    {
        // Arrange
        var application = "   ";
        var environment = "env";
        var configuration = "config";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new AppConfigKey(application, environment, configuration));
    }

    [Theory]
    [InlineData("a", "env", "config")] // too short
    [InlineData("app", "e", "config")] // too short
    [InlineData("app", "env", "c")]    // too short
    public void Constructor_WithTooShortValues_ShouldThrowArgumentException(
        string application, string environment, string configuration)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new AppConfigKey(application, environment, configuration));
    }
}
