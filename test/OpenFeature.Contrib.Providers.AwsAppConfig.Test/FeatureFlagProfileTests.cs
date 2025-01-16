using Xunit;
using OpenFeature.Contrib.Providers.AwsAppConfig;

public class FeatureFlagProfileTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var profile = new FeatureFlagProfile();

        // Assert
        Assert.NotNull(profile);
        // Add assertions for any default properties that should be initialized
    }

    [Fact]
    public void PropertySetter_ShouldSetProperties()
    {
        // Arrange
        var appname = "TestApplication";
        var environment = "Test Environment";
        var configProfileId = "Test Configuration";

        // Act
        var profile = new FeatureFlagProfile{
            ApplicationIdentifier = appname,
            EnvironmentIdentifier = environment,
            ConfigurationProfileIdentifier = configProfileId,
            
        };

        // Assert
        Assert.Equal(appname, profile.ApplicationIdentifier);
        Assert.Equal(environment, profile.EnvironmentIdentifier);
        Assert.Equal(configProfileId, profile.ConfigurationProfileIdentifier);
    }

    [Theory]
    [InlineData("TestApplication", "TestEnvironment", "TestConfigProfileId")]
    [InlineData("Test2Application", "Test2Environment", "Test2ConfigProfileId")]
    public void ToString_ShouldReturnKeyString(string appName, string env, string configProfileId)
    {
        // Arrange
        var profile = new FeatureFlagProfile {
            ApplicationIdentifier = appName,
            EnvironmentIdentifier = env, 
            ConfigurationProfileIdentifier = configProfileId,
        };

        // Act
        var result = profile.ToString();

        // Assert
        Assert.Equal($"{appName}_{env}_{configProfileId}", result);
    }

    [Theory]
    [InlineData("TestApplication", "TestEnvironment", "TestConfigProfileId")]
    [InlineData("Test2Application", "Test2Environment", "Test2ConfigProfileId")]
    public void IsValid_ReturnTrue(string appName, string env, string configProfileId)
    {
        // Arrange
        var profile = new FeatureFlagProfile {
            ApplicationIdentifier = appName,
            EnvironmentIdentifier = env,
            ConfigurationProfileIdentifier = configProfileId,
        };
        
        // Assert
        Assert.True(profile.IsValid);
    }

    [Theory]
    [InlineData("", "TestEnvironment", "TestConfigProfileId")]
    [InlineData("TestApplication", "", "TestConfigProfileId")]
    [InlineData("TestApplication", "TestEnvironment", "")]
    [InlineData("", "", "")]
    public void IsValid_ReturnFalse(string appName, string env, string configProfileId)
    {
        // Arrange
        var profile = new FeatureFlagProfile {
            ApplicationIdentifier = appName,
            EnvironmentIdentifier = env,
            ConfigurationProfileIdentifier = configProfileId,
        };        

        // Assert
        Assert.False(profile.IsValid);
    }     
}
