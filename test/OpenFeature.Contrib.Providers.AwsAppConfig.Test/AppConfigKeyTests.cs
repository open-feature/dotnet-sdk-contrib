using Xunit;
using OpenFeature.Contrib.Providers.AwsAppConfig;
using System;

public class AppConfigKeyTests
{
    [Fact]
    public void Constructor_3input_WithValidParameters_ShouldSetProperties()
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
    [InlineData(null, "env", "config")]
    [InlineData("app", null, "config")]    
    public void Constructor_3input_WithInvalidParameters_ShouldThrowArgumentException(
        string confiProfileId, string flagKey, string attributeKey)
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new AppConfigKey(confiProfileId, flagKey, attributeKey));
    }   

    [Theory]
    [InlineData("app-123", "env-123", "config-123")]
    [InlineData("app_123", "env_123", "config_123")]
    [InlineData("app.123", "env.123", "config.123")]
    public void Constructor_3input_WithSpecialCharacters_ShouldAcceptValidPatterns(
        string configProfileId, string flagKey, string attributeKey)
    {
        // Arrange & Act
        var key = new AppConfigKey(configProfileId, flagKey, attributeKey);

        // Assert
        Assert.Equal(configProfileId, key.ConfigurationProfileId);
        Assert.Equal(flagKey, key.FlagKey);
        Assert.Equal(attributeKey, key.AttributeKey);
    }    

    [Fact]
    public void Constructor_3input_WithWhitespaceValues_ShouldThrowArgumentException()
    {
        // Arrange
        var application = "   ";
        var environment = "env";
        var configuration = "config";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new AppConfigKey(application, environment, configuration));
    }

    [Fact]
    public void Constructor_WithNullKey_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new AppConfigKey(null));
        Assert.Equal("Key cannot be null or empty", exception.Message);
    }

    [Fact]
    public void Constructor_WithEmptyKey_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new AppConfigKey(string.Empty));
        Assert.Equal("Key cannot be null or empty", exception.Message);
    }

    [Fact]
    public void Constructor_WithWhitespaceKey_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new AppConfigKey("   "));
        Assert.Equal("Key cannot be null or empty", exception.Message);
    }

    [Fact]
    public void Constructor_WithSinglePart_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new AppConfigKey("singlepart"));
        Assert.Equal("Invalid key format. Flag key is expected in configurationProfileId:flagKey[:attributeKey] format", exception.Message);
    }

    [Fact]
    public void Constructor_WithTwoParts_SetsPropertiesCorrectly()
    {
        // Arrange
        var key = "profile123:flag456";

        // Act
        var appConfigKey = new AppConfigKey(key);

        // Assert
        Assert.Equal("profile123", appConfigKey.ConfigurationProfileId);
        Assert.Equal("flag456", appConfigKey.FlagKey);
        Assert.Null(appConfigKey.AttributeKey);
        Assert.False(appConfigKey.HasAttribute);
    }

    [Fact]
    public void Constructor_WithThreeParts_SetsPropertiesCorrectly()
    {
        // Arrange
        var key = "profile123:flag456:attr789";

        // Act
        var appConfigKey = new AppConfigKey(key);

        // Assert
        Assert.Equal("profile123", appConfigKey.ConfigurationProfileId);
        Assert.Equal("flag456", appConfigKey.FlagKey);
        Assert.Equal("attr789", appConfigKey.AttributeKey);
        Assert.True(appConfigKey.HasAttribute);
    }

    [Theory]
    [InlineData("profile123:flag456:attr789:extra:parts")]
    [InlineData("profile123::attr789:extra:parts")]
    [InlineData("profile123:flagkey456:")]
    [InlineData("profile123:")]
    [InlineData(":flagkey456")]
    [InlineData(":flagkey456:")]
    [InlineData("::attribute789")]
    [InlineData("::")]
    [InlineData(":::")]
    [InlineData("RandomSgring)()@*Q()*#Q$@#$")]
    public void Constructor_WithInvalidPattern_ShouldThrowArgumentException(string key)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new AppConfigKey("::"));
        Assert.Equal("Invalid key format. Flag key is expected in configurationProfileId:flagKey[:attributeKey] format", exception.Message);
    }    

    [Theory]
    [InlineData("profile123::attr789")]
    [InlineData(":flag456:attr789")]
    [InlineData("::attr789")]
    public void Constructor_WithEmptyMiddleParts_PreservesNonEmptyParts(string key)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new AppConfigKey(key));
        Assert.Equal("Invalid key format. Flag key is expected in configurationProfileId:flagKey[:attributeKey] format", exception.Message);
    }    

    [Fact]
    public void Constructor_WithLeadingSeparator_ThrowsArgumentException()
    {
        // Arrange
        var key = ":profile123:flag456";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new AppConfigKey(key));
        Assert.Equal("Invalid key format. Flag key is expected in configurationProfileId:flagKey[:attributeKey] format", exception.Message);
    }

    [Fact]
    public void HasAttribute_WhenAttributeKeyIsNull_ReturnsFalse()
    {
        // Arrange
        var appConfigKey = new AppConfigKey("profileId", "flagKey");

        // Act & Assert
        Assert.False(appConfigKey.HasAttribute);
    }

    [Fact]
    public void HasAttribute_WhenAttributeKeyIsEmpty_ReturnsFalse()
    {
        // Arrange
        var appConfigKey = new AppConfigKey("profileId", "flagKey", "");

        // Act & Assert
        Assert.False(appConfigKey.HasAttribute);
    }

    [Fact]
    public void HasAttribute_WhenAttributeKeyIsWhitespace_ReturnsFalse()
    {
        // Arrange
        var appConfigKey = new AppConfigKey("profileId", "flagKey", "   ");

        // Act & Assert
        Assert.False(appConfigKey.HasAttribute);
    }

    [Fact]
    public void HasAttribute_WhenAttributeKeyIsProvided_ReturnsTrue()
    {
        // Arrange
        var appConfigKey = new AppConfigKey("profileId", "flagKey", "attributeKey");

        // Act & Assert
        Assert.True(appConfigKey.HasAttribute);
    }

    [Fact]
    public void HasAttribute_WhenConstructedWithStringWithAttribute_ReturnsTrue()
    {
        // Arrange
        var appConfigKey = new AppConfigKey("profileId:flagKey:attributeKey");

        // Act & Assert
        Assert.True(appConfigKey.HasAttribute);
    }

    [Fact]
    public void HasAttribute_WhenConstructedWithStringWithoutAttribute_ReturnsFalse()
    {
        // Arrange
        var appConfigKey = new AppConfigKey("profileId:flagKey");

        // Act & Assert
        Assert.False(appConfigKey.HasAttribute);
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
    [InlineData("app1:env1:config1")]
    [InlineData("my-app:my-env:my-config")]
    [InlineData("APP:ENV")]
    public void ToString_WithSingleInput_ShouldReturnFormattedString(string input)
    {
        // Arrange
        var key = new AppConfigKey(input);

        // Act
        var result = key.ToString();

        // Assert
        Assert.Contains(key.ConfigurationProfileId, result);
        Assert.Contains(key.FlagKey, result);        
    }
}
