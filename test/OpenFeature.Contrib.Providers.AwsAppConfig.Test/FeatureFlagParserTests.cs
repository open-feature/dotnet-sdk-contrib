using Xunit;
using System;
using System.Text.Json;
using OpenFeature.Model;
using Microsoft.Extensions.Configuration;
using OpenFeature.Contrib.Providers.AwsAppConfig;

public class FeatureFlagParserTests
{
    private readonly string _jsonContent;

    public FeatureFlagParserTests()
    {
        _jsonContent = System.IO.File.ReadAllText("test-data.json");
    }

    [Fact]
    public void ParseFeatureFlag_EnabledFlag_ReturnsValue()
    {        
        // Act
        var result = FeatureFlagParser.ParseFeatureFlag("test-enabled-flag", new Value(), _jsonContent);

        // Assert
        Assert.True(result.IsStructure);
        Assert.True(result.AsStructure["enabled"].AsBoolean);
        Assert.Equal("testValue", result.AsStructure["additionalAttribute"].AsString);
    }

    [Fact]
    public void ParseFeatureFlag_DisabledFlag_ReturnsValue()
    {       
        // Act
        var result = FeatureFlagParser.ParseFeatureFlag("test-disabled-flag", new Value(), _jsonContent);

        // Assert
        Assert.True(result.IsStructure);
        Assert.False(result.AsStructure["enabled"].AsBoolean);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("invalid")]
    public void ParseFeatureFlag_WhenValueIsInvalid_ThrowsArgumentNullException(string input)
    {        
        // Act & Assert
        if(input == null){
            Assert.Throws<ArgumentNullException>(() => FeatureFlagParser.ParseFeatureFlag("test-enabled-flag", new Value(), input));
        }
        else
        {
            Assert.Throws<JsonException>(() => FeatureFlagParser.ParseFeatureFlag("test-enabled-flag", new Value(), input));
        }
        
    }    
}
