using Xunit;
using Moq;
using OpenFeature.Model;
using Amazon.AppConfigData.Model;
using System.Text;
using System.IO;
using OpenFeature.Contrib.Providers.AwsAppConfig;
using System.Threading.Tasks;
using System.Collections.Generic;

public class AppConfigProviderTests
{
    private readonly Mock<IRetrievalApi> _mockAppConfigApi;
    private readonly AppConfigProvider _provider;
    private readonly string _jsonContent;
    private const string ApplicationName = "TestApp";
    private const string EnvironmentName = "TestEnv";

    public AppConfigProviderTests()
    {
        _mockAppConfigApi = new Mock<IRetrievalApi>();
        _provider = new AppConfigProvider(_mockAppConfigApi.Object, ApplicationName, EnvironmentName);
        _jsonContent = System.IO.File.ReadAllText("test-data.json");
    }

    #region ResolveBooleanValueAsync Tests
    [Fact]
    public async Task ResolveBooleanValueAsync_WhenFlagExists_ReturnsCorrectValue()
    {
        // Arrange
        const string flagKey = "configProfileId:test-enabled-flag:enabled";
        const bool expectedValue = true;
        SetupMockResponse(_jsonContent);

        // Act
        var result = await _provider.ResolveBooleanValueAsync(flagKey, false);

        // Assert
        Assert.Equal(expectedValue, result.Value);
        Assert.Equal(flagKey, result.FlagKey);
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_WhenFlagDoesNotExist_ReturnsDefaultValue()
    {
        // Arrange
        const string flagKey = "configProfileId:test-enabled-flag:enabled";
        const bool defaultValue = false;
        SetupMockResponse("{}");

        // Act
        var result = await _provider.ResolveBooleanValueAsync(flagKey, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result.Value);
    }
    #endregion

    #region ResolveDoubleValueAsync Tests
    [Fact]
    public async Task ResolveDoubleValueAsync_WhenFlagExists_ReturnsCorrectValue()
    {
        // Arrange
        const string flagKey = "configProfileId:test-enabled-flag:doubleAttribute";
        const double expectedValue = 3.14;
        SetupMockResponse(_jsonContent);

        // Act
        var result = await _provider.ResolveDoubleValueAsync(flagKey, 0.0);

        // Assert
        Assert.Equal(expectedValue, result.Value);
        Assert.Equal(flagKey, result.FlagKey);
    }

    [Fact]
    public async Task ResolveDoubleValueAsync_WhenFlagDoesNotExist_ReturnsDefaultValue()
    {
        // Arrange
        const string flagKey = "configProfileId:test-enabled-flag:doubleAttribute";
        const double defaultValue = 1.0;
        SetupMockResponse("{}");

        // Act
        var result = await _provider.ResolveDoubleValueAsync(flagKey, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result.Value);
    }
    #endregion

    #region ResolveIntegerValueAsync Tests
    [Fact]
    public async Task ResolveIntegerValueAsync_WhenFlagExists_ReturnsCorrectValue()
    {
        // Arrange
        const string flagKey = "configProfileId:test-enabled-flag:intAttribute";
        const int expectedValue = 42;
        SetupMockResponse(_jsonContent);

        // Act
        var result = await _provider.ResolveIntegerValueAsync(flagKey, 0);

        // Assert
        Assert.Equal(expectedValue, result.Value);
        Assert.Equal(flagKey, result.FlagKey);
    }

    [Fact]
    public async Task ResolveIntegerValueAsync_WhenFlagDoesNotExist_ReturnsDefaultValue()
    {
        // Arrange
        const string flagKey = "configProfileId:test-enabled-flag:intAttribute";
        const int defaultValue = 100;
        SetupMockResponse("{}");

        // Act
        var result = await _provider.ResolveIntegerValueAsync(flagKey, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result.Value);
    }
    #endregion

    #region ResolveStringValueAsync Tests
    [Fact]
    public async Task ResolveStringValueAsync_WhenFlagExists_ReturnsCorrectValue()
    {
        // Arrange
        const string flagKey = "configProfileId:test-enabled-flag:stringAttribute";
        const string expectedValue = "testValue";
        SetupMockResponse(_jsonContent);

        // Act
        var result = await _provider.ResolveStringValueAsync(flagKey, "default");

        // Assert
        Assert.Equal(expectedValue, result.Value);
        Assert.Equal(flagKey, result.FlagKey);
    }

    [Fact]
    public async Task ResolveStringValueAsync_WhenFlagDoesNotExist_ReturnsDefaultValue()
    {
        // Arrange
        const string flagKey = "configProfileId:test-enabled-flag:stringAttribute";
        const string defaultValue = "default-value";
        SetupMockResponse("{}");

        // Act
        var result = await _provider.ResolveStringValueAsync(flagKey, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result.Value);
    }
    #endregion

    #region ResolveStructureValueAsync Tests
    [Fact]
    public async Task ResolveStructureValueAsync_WhenFlagExists_ReturnsCorrectValue()
    {
        // Arrange
        const string flagKey = "configProfileId:test-enabled-flag";
        const string jsonValue = "{\"key\": \"value\", \"number\": 42}";
        SetupMockResponse($"{{\"{flagKey}\": {jsonValue}}}");

        // Act
        var result = await _provider.ResolveStructureValueAsync(flagKey, new Value());

        // Assert
        Assert.NotNull(result.Value.AsStructure);
        Assert.Equal("value", result.Value.AsStructure["key"].AsString);
        Assert.Equal(42, result.Value.AsStructure["number"].AsInteger);
    }

    [Fact]
    public async Task ResolveStructureValueAsync_WhenFlagDoesNotExist_ReturnsDefaultValue()
    {
        // Arrange
        const string flagKey = "configProfileId:test-enabled-flag";
        var defaultValue = new Value(new Dictionary<string, Value> 
        { 
            ["default"] = new Value("default") 
        });
        SetupMockResponse("{}");

        // Act
        var result = await _provider.ResolveStructureValueAsync(flagKey, defaultValue);

        // Assert
        Assert.Equal(defaultValue.AsStructure["default"].AsString, 
                    result.Value.AsStructure["default"].AsString);
    }
    #endregion

    #region Attribute Resolution Tests
    [Fact]
    public async Task ResolveValue_WithAttributeKey_ReturnsAttributeValue()
    {
        // Arrange
        const string flagKey = "myFlag:color";
        const string expectedValue = "blue";
        SetupMockResponse($"{{\"myFlag\": {{\"color\": \"{expectedValue}\"}}}}");

        // Act
        var result = await _provider.ResolveStringValueAsync(flagKey, "default");

        // Assert
        Assert.Equal(expectedValue, result.Value);
    }

    [Fact]
    public async Task ResolveValue_WithInvalidAttributeKey_ReturnsDefaultValue()
    {
        // Arrange
        const string flagKey = "myFlag:invalidAttribute";
        const string defaultValue = "default";
        SetupMockResponse("{\"myFlag\": {\"color\": \"blue\"}}");

        // Act
        var result = await _provider.ResolveStringValueAsync(flagKey, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result.Value);
    }
    #endregion

    private void SetupMockResponse(string jsonContent)
    {
        var response = new GetLatestConfigurationResponse
        {
            Configuration = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent))
        };

        _mockAppConfigApi
            .Setup(x => x.GetLatestConfigurationAsync(It.IsAny<FeatureFlagProfile>()))
            .ReturnsAsync(response);
    }
}
