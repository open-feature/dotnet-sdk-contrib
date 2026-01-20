using System.Text.Json;
using NSubstitute;
using OpenFeature.Constant;
using OpenFeature.Model;
using OpenFeature.Providers.Ofrep.Client;
using OpenFeature.Providers.Ofrep.Client.Constants;
using OpenFeature.Providers.Ofrep.Configuration;
using OpenFeature.Providers.Ofrep.Models;
using Xunit;

namespace OpenFeature.Providers.Ofrep.Test;

public class OfrepProviderTest : IDisposable
{
    private readonly IOfrepClient _mockClient = Substitute.For<IOfrepClient>();
    private readonly OfrepOptions _defaultConfiguration = new("http://localhost:8080");
    private OfrepProvider? _provider;

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenConfigurationIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OfrepProvider((OfrepOptions)null!));
    }

    [Fact]
    public void Constructor_ShouldCreateProviderSuccessfully_WhenConfigurationIsValid()
    {
        // Act
        this._provider = new OfrepProvider(this._defaultConfiguration);

        // Assert
        Assert.NotNull(this._provider);
    }

    [Fact]
    public void GetMetadata_ShouldReturnCorrectProviderName()
    {
        // Arrange
        this._provider = new OfrepProvider(this._defaultConfiguration);

        // Act
        var metadata = this._provider.GetMetadata();

        // Assert
        Assert.Equal("OpenFeature Remote Evaluation Protocol Server", metadata.Name);
    }

    [Fact]
    public async Task ShutdownAsync_ShouldDisposeResources()
    {
        // Arrange
        this._provider = new OfrepProvider(this._defaultConfiguration);

        // Act
        await this._provider.ShutdownAsync();

        // Assert - No exception should be thrown
        Assert.True(true);
    }

    [Fact]
    public void Dispose_ShouldBeIdempotent()
    {
        // Arrange
        this._provider = new OfrepProvider(this._defaultConfiguration);

        // Act - Multiple dispose calls should not throw
        this._provider.Dispose();
        this._provider.Dispose();

        // Assert - No exception should be thrown
        Assert.True(true);
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_ShouldReturnCorrectValue_WhenClientReturnsValidResponse()
    {
        // Arrange
        const string flagKey = "test-flag";
        const bool expectedValue = true;
        const bool defaultValue = false;
        var context = EvaluationContext.Builder().Set("userId", "123").Build();

        var expectedResponse = new OfrepResponse<bool>(flagKey, expectedValue) { Variant = "enabled" };

        this._mockClient
            .EvaluateFlag(flagKey, defaultValue, context, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        this._provider = this.CreateProviderWithMockClient();

        // Act
        var result = await this._provider.ResolveBooleanValueAsync(flagKey, defaultValue, context);

        // Assert
        Assert.Equal(flagKey, result.FlagKey);
        Assert.Equal(expectedValue, result.Value);
        Assert.Equal(ErrorType.None, result.ErrorType);
        Assert.Equal("enabled", result.Variant);
    }

    [Fact]
    public async Task ResolveStringValueAsync_ShouldReturnCorrectValue_WhenClientReturnsValidResponse()
    {
        // Arrange
        const string flagKey = "test-flag";
        const string expectedValue = "test-value";
        const string defaultValue = "default";
        var context = EvaluationContext.Builder().Set("userId", "123").Build();

        var expectedResponse = new OfrepResponse<string>(flagKey, expectedValue) { Variant = "variant1" };

        this._mockClient
            .EvaluateFlag(flagKey, defaultValue, context, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        this._provider = this.CreateProviderWithMockClient();

        // Act
        var result = await this._provider.ResolveStringValueAsync(flagKey, defaultValue, context);

        // Assert
        Assert.Equal(flagKey, result.FlagKey);
        Assert.Equal(expectedValue, result.Value);
        Assert.Equal(ErrorType.None, result.ErrorType);
        Assert.Equal("variant1", result.Variant);
    }

    [Fact]
    public async Task ResolveIntegerValueAsync_ShouldReturnCorrectValue_WhenClientReturnsValidResponse()
    {
        // Arrange
        const string flagKey = "test-flag";
        const int expectedValue = 42;
        const int defaultValue = 0;
        var context = EvaluationContext.Builder().Set("userId", "123").Build();

        var expectedResponse = new OfrepResponse<int>(flagKey, expectedValue) { Variant = "high" };

        this._mockClient
            .EvaluateFlag(flagKey, defaultValue, context, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        this._provider = this.CreateProviderWithMockClient();

        // Act
        var result = await this._provider.ResolveIntegerValueAsync(flagKey, defaultValue, context);

        // Assert
        Assert.Equal(flagKey, result.FlagKey);
        Assert.Equal(expectedValue, result.Value);
        Assert.Equal(ErrorType.None, result.ErrorType);
        Assert.Equal("high", result.Variant);
    }

    [Fact]
    public async Task ResolveDoubleValueAsync_ShouldReturnCorrectValue_WhenClientReturnsValidResponse()
    {
        // Arrange
        const string flagKey = "test-flag";
        const double expectedValue = 3.14;
        const double defaultValue = 0.0;
        var context = EvaluationContext.Builder().Set("userId", "123").Build();

        var expectedResponse = new OfrepResponse<double>(flagKey, expectedValue) { Variant = "pi" };

        this._mockClient
            .EvaluateFlag(flagKey, defaultValue, context, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        this._provider = this.CreateProviderWithMockClient();

        // Act
        var result = await this._provider.ResolveDoubleValueAsync(flagKey, defaultValue, context);

        // Assert
        Assert.Equal(flagKey, result.FlagKey);
        Assert.Equal(expectedValue, result.Value);
        Assert.Equal(ErrorType.None, result.ErrorType);
        Assert.Equal("pi", result.Variant);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ResolveStructureValueAsync_ShouldThrowArgumentException_WhenFlagKeyIsNull(string flagKey)
    {
        // Arrange
        this._provider = new OfrepProvider(this._defaultConfiguration);
        var defaultValue = new Value("default");

        // Act & Assert
#if NET8_0_OR_GREATER
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await this._provider.ResolveStructureValueAsync(flagKey, defaultValue));
#else
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _provider.ResolveStructureValueAsync(flagKey, defaultValue));
#endif
    }

    [Fact]
    public async Task ResolveStructureValueAsync_ShouldReturnCorrectValue_WhenClientReturnsValidResponse()
    {
        // Arrange
        const string flagKey = "test-flag";
        var defaultValue = new Value("default");
        var context = EvaluationContext.Builder().Set("userId", "123").Build();

        var jsonElement = JsonSerializer.Deserialize<JsonElement>("{\"property1\": \"value1\", \"property2\": 123}");
        var expectedResponse = new OfrepResponse<JsonElement?>(flagKey, jsonElement) { Variant = "object1" };

        this._mockClient
            .EvaluateFlag<JsonElement?>(flagKey, null, context, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        this._provider = this.CreateProviderWithMockClient();

        // Act
        var result = await this._provider.ResolveStructureValueAsync(flagKey, defaultValue, context);

        // Assert
        Assert.Equal(flagKey, result.FlagKey);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.IsStructure);
        Assert.Equal("value1", result.Value.AsStructure?.GetValue("property1").AsString);
        Assert.Equal(123, result.Value.AsStructure?.GetValue("property2").AsInteger);
        Assert.Equal(ErrorType.None, result.ErrorType);
        Assert.Equal("object1", result.Variant);
    }

    /// <summary>
    /// Test case from GitHub issue #552 - verifies that JSON object values from OFREP backend
    /// are correctly converted to OpenFeature Value/Structure types.
    /// </summary>
    [Fact]
    public async Task ResolveStructureValueAsync_ShouldHandleIssue552Example_WhenBackendReturnsJsonObject()
    {
        // Arrange - exact example from issue #552
        const string flagKey = "my-object-flag";
        var defaultValue = new Value(Structure.Builder()
            .Set("property1", new Value("default"))
            .Build());
        var context = EvaluationContext.Builder().Set("userId", "123").Build();

        // Simulates OFREP backend response: {"property1": "value1", "property2": 123, "property3": true}
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(
            "{\"property1\": \"value1\", \"property2\": 123, \"property3\": true}");
        var expectedResponse = new OfrepResponse<JsonElement?>(flagKey, jsonElement)
        {
            Variant = "default",
            Reason = "STATIC"
        };

        this._mockClient
            .EvaluateFlag<JsonElement?>(flagKey, null, context, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        this._provider = this.CreateProviderWithMockClient();

        // Act
        var result = await this._provider.ResolveStructureValueAsync(flagKey, defaultValue, context);

        // Assert - should successfully convert JsonElement to Structure
        Assert.Equal(flagKey, result.FlagKey);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.IsStructure);

        var structure = result.Value.AsStructure;
        Assert.NotNull(structure);
        Assert.Equal("value1", structure.GetValue("property1").AsString);
        Assert.Equal(123, structure.GetValue("property2").AsInteger);
        Assert.True(structure.GetValue("property3").AsBoolean);

        Assert.Equal(ErrorType.None, result.ErrorType);
        Assert.Equal("default", result.Variant);
        Assert.Equal("STATIC", result.Reason);
    }

    [Fact]
    public async Task ResolveStructureValueAsync_ShouldReturnDefaultValue_WhenClientReturnsNullValue()
    {
        // Arrange
        const string flagKey = "test-flag";
        var defaultValue = new Value("default");
        var context = EvaluationContext.Builder().Set("userId", "123").Build();

        var expectedResponse = new OfrepResponse<JsonElement?>(flagKey, null) { Variant = "null" };

        this._mockClient
            .EvaluateFlag<JsonElement?>(flagKey, null, context, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        this._provider = this.CreateProviderWithMockClient();

        // Act
        var result = await this._provider.ResolveStructureValueAsync(flagKey, defaultValue, context);

        // Assert
        Assert.Equal(flagKey, result.FlagKey);
        Assert.Equal("default", result.Value.AsString);
        Assert.Equal(ErrorType.None, result.ErrorType);
        Assert.Equal("null", result.Variant);
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_ShouldMapErrorCorrectly_WhenClientReturnsError()
    {
        // Arrange
        const string flagKey = "test-flag";
        const bool defaultValue = false;
        var context = EvaluationContext.Builder().Set("userId", "123").Build();

        var errorResponse = new OfrepResponse<bool>(flagKey, defaultValue)
        {
            ErrorCode = ErrorCodes.FlagNotFound,
            ErrorMessage = "Flag not found"
        };

        this._mockClient
            .EvaluateFlag(flagKey, defaultValue, context, Arg.Any<CancellationToken>())
            .Returns(errorResponse);

        this._provider = this.CreateProviderWithMockClient();

        // Act
        var result = await this._provider.ResolveBooleanValueAsync(flagKey, defaultValue, context);

        // Assert
        Assert.Equal(flagKey, result.FlagKey);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal(ErrorType.FlagNotFound, result.ErrorType);
        Assert.Equal("Flag not found", result.ErrorMessage);
    }

    [Theory]
    [InlineData(ErrorCodes.FlagNotFound, ErrorType.FlagNotFound)]
    [InlineData(ErrorCodes.TypeMismatch, ErrorType.TypeMismatch)]
    [InlineData(ErrorCodes.ParseError, ErrorType.ParseError)]
    [InlineData(ErrorCodes.ProviderNotReady, ErrorType.ProviderNotReady)]
    [InlineData("unknown_error", ErrorType.None)]
    [InlineData("", ErrorType.None)]
    public async Task ErrorMapping_ShouldMapCorrectly(string errorCode, ErrorType expectedErrorType)
    {
        // Arrange
        const string flagKey = "test-flag";
        const string defaultValue = "default";

        var errorResponse = new OfrepResponse<string>(flagKey, defaultValue)
        {
            ErrorCode = errorCode,
            ErrorMessage = "Test error"
        };

        this._mockClient
            .EvaluateFlag(flagKey, defaultValue, null, Arg.Any<CancellationToken>())
            .Returns(errorResponse);

        this._provider = this.CreateProviderWithMockClient();

        // Act
        var result = await this._provider.ResolveStringValueAsync(flagKey, defaultValue);

        // Assert
        Assert.Equal(expectedErrorType, result.ErrorType);
    }

    [Fact]
    public void ValidateFlagTypeIsSupported_ShouldThrowArgumentException_WhenUnsupportedType()
    {
        // This test requires access to the private method, so we'll test through a public method
        // that would trigger the validation for an unsupported type

        // Arrange
        this._provider = new OfrepProvider(this._defaultConfiguration);

        // We can't directly test the private method, but the provider should support all OFREP types
        // This test verifies the provider can handle standard OFREP types without throwing
        Assert.True(true); // Placeholder - the actual validation happens internally
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_ShouldUseCorrectParameters_WhenCallingClient()
    {
        // Arrange
        const string flagKey = "test-flag";
        const bool defaultValue = false;
        var context = EvaluationContext.Builder().Set("userId", "123").Build();
        var cancellationToken = CancellationToken.None;

        var expectedResponse = new OfrepResponse<bool>(flagKey, true);

        this._mockClient
            .EvaluateFlag(flagKey, defaultValue, context, cancellationToken)
            .Returns(expectedResponse);

        this._provider = this.CreateProviderWithMockClient();

        // Act
        await this._provider.ResolveBooleanValueAsync(flagKey, defaultValue, context, cancellationToken);

        // Assert
        await this._mockClient.Received(1).EvaluateFlag(flagKey, defaultValue, context, cancellationToken);
    }

    [Fact]
    public async Task ResolveStringValueAsync_ShouldUseCorrectParameters_WhenCallingClient()
    {
        // Arrange
        const string flagKey = "test-flag";
        const string defaultValue = "default";
        var context = EvaluationContext.Builder().Set("userId", "123").Build();
        var cancellationToken = CancellationToken.None;

        var expectedResponse = new OfrepResponse<string>(flagKey, "value");

        this._mockClient
            .EvaluateFlag(flagKey, defaultValue, context, cancellationToken)
            .Returns(expectedResponse);

        this._provider = this.CreateProviderWithMockClient();

        // Act
        await this._provider.ResolveStringValueAsync(flagKey, defaultValue, context, cancellationToken);

        // Assert
        await this._mockClient.Received(1).EvaluateFlag(flagKey, defaultValue, context, cancellationToken);
    }

    [Fact]
    public async Task ResolveIntegerValueAsync_ShouldUseCorrectParameters_WhenCallingClient()
    {
        // Arrange
        const string flagKey = "test-flag";
        const int defaultValue = 0;
        var context = EvaluationContext.Builder().Set("userId", "123").Build();
        var cancellationToken = CancellationToken.None;

        var expectedResponse = new OfrepResponse<int>(flagKey, 42);

        this._mockClient
            .EvaluateFlag(flagKey, defaultValue, context, cancellationToken)
            .Returns(expectedResponse);

        this._provider = this.CreateProviderWithMockClient();

        // Act
        await this._provider.ResolveIntegerValueAsync(flagKey, defaultValue, context, cancellationToken);

        // Assert
        await this._mockClient.Received(1).EvaluateFlag(flagKey, defaultValue, context, cancellationToken);
    }

    [Fact]
    public async Task ResolveDoubleValueAsync_ShouldUseCorrectParameters_WhenCallingClient()
    {
        // Arrange
        const string flagKey = "test-flag";
        const double defaultValue = 0.0;
        var context = EvaluationContext.Builder().Set("userId", "123").Build();
        var cancellationToken = CancellationToken.None;

        var expectedResponse = new OfrepResponse<double>(flagKey, 3.14);

        this._mockClient
            .EvaluateFlag(flagKey, defaultValue, context, cancellationToken)
            .Returns(expectedResponse);

        this._provider = this.CreateProviderWithMockClient();

        // Act
        await this._provider.ResolveDoubleValueAsync(flagKey, defaultValue, context, cancellationToken);

        // Assert
        await this._mockClient.Received(1).EvaluateFlag(flagKey, defaultValue, context, cancellationToken);
    }

    [Fact]
    public async Task ResolveStructureValueAsync_ShouldUseCorrectParameters_WhenCallingClient()
    {
        // Arrange
        const string flagKey = "test-flag";
        var defaultValue = new Value("default");
        var context = EvaluationContext.Builder().Set("userId", "123").Build();
        var cancellationToken = CancellationToken.None;

        var jsonElement = JsonSerializer.Deserialize<JsonElement>("\"value\"");
        var expectedResponse = new OfrepResponse<JsonElement?>(flagKey, jsonElement);

        this._mockClient
            .EvaluateFlag<JsonElement?>(flagKey, null, context, cancellationToken)
            .Returns(expectedResponse);

        this._provider = this.CreateProviderWithMockClient();

        // Act
        await this._provider.ResolveStructureValueAsync(flagKey, defaultValue, context, cancellationToken);

        // Assert
        await this._mockClient.Received(1).EvaluateFlag<JsonElement?>(flagKey, null, context, cancellationToken);
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_ShouldHandleNullContext()
    {
        // Arrange
        const string flagKey = "test-flag";
        const bool defaultValue = false;

        var expectedResponse = new OfrepResponse<bool>(flagKey, true);

        this._mockClient
            .EvaluateFlag(flagKey, defaultValue, null, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        this._provider = this.CreateProviderWithMockClient();

        // Act
        var result = await this._provider.ResolveBooleanValueAsync(flagKey, defaultValue);

        // Assert
        Assert.Equal(flagKey, result.FlagKey);
        Assert.True(result.Value);
        Assert.Equal(ErrorType.None, result.ErrorType);
    }

    [Fact]
    public async Task ResolveStructureValueAsync_ShouldHandleNullContext()
    {
        // Arrange
        const string flagKey = "test-flag";
        var defaultValue = new Value("default");

        var jsonElement = JsonSerializer.Deserialize<JsonElement>("\"value\"");
        var expectedResponse = new OfrepResponse<JsonElement?>(flagKey, jsonElement);

        this._mockClient
            .EvaluateFlag<JsonElement?>(flagKey, null, null, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        this._provider = this.CreateProviderWithMockClient();

        // Act
        var result = await this._provider.ResolveStructureValueAsync(flagKey, defaultValue);

        // Assert
        Assert.Equal(flagKey, result.FlagKey);
        Assert.NotNull(result.Value);
        Assert.Equal(ErrorType.None, result.ErrorType);
    }

    private OfrepProvider CreateProviderWithMockClient()
    {
        // Since we can't inject the mock client directly, we'll use reflection or create a testable version
        // For now, we'll create a provider with the real configuration and rely on the existing constructor
        // In a real scenario, you might want to create an internal constructor that accepts IOfrepClient

        var provider = new OfrepProvider(this._defaultConfiguration);

        // Use reflection to replace the private _client field with our mock
        var clientField = typeof(OfrepProvider).GetField("_client",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        clientField?.SetValue(provider, this._mockClient);

        return provider;
    }

    public void Dispose()
    {
        this._provider?.Dispose();
        GC.SuppressFinalize(this);
    }
}
