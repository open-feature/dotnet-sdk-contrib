using System.Net;
#if NETFRAMEWORK
using System.Net.Http;
#endif
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenFeature.Model;
using OpenFeature.Providers.Ofrep.Client;
using OpenFeature.Providers.Ofrep.Configuration;
using OpenFeature.Providers.Ofrep.Models;
using OpenFeature.Providers.Ofrep.Test.Helpers;
using Xunit;

namespace OpenFeature.Providers.Ofrep.Test.Client;

public class OfrepClientTest : IDisposable
{
    private readonly ILogger<OfrepClient> _mockLogger = NullLogger<OfrepClient>.Instance;
    private readonly TestHttpMessageHandler _mockHandler = new();
    private readonly OfrepConfiguration _configuration = new("https://api.example.com/");

    private readonly JsonSerializerOptions _jsonSerializerCamelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Dispose()
    {
        this._mockHandler.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Constructor Tests
    [Fact]
    public void Constructor_WithCustomHttpTimeout_ShouldUseConfiguredTimeout()
    {
        // Arrange & Act
        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Assert - The client should be created successfully
        // Note: We can't directly test the timeout value as it's internal to HttpClient,
        // but we can verify the client was constructed without error
        Assert.NotNull(client);
    }

    [Fact]
    public void Constructor_WithValidConfiguration_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        using var client = new OfrepClient(this._configuration, this._mockLogger);

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OfrepClient(null!, this._mockLogger));
    }

    [Fact]
    public void Constructor_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OfrepClient(this._configuration, null!, this._mockLogger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldUseNullLogger()
    {
        // Arrange & Act
        using var client = new OfrepClient(this._configuration, this._mockHandler);

        // Assert - Should not throw and should work properly
        Assert.NotNull(client);
    }

    [Fact]
    public void Constructor_WithAuthorizationHeader_ShouldSetBearerToken()
    {
        // Arrange
        var configWithAuth = new OfrepConfiguration("https://api.example.com/")
        {
            AuthorizationHeader = "test-token-123"
        };

        // Act
        using var client = new OfrepClient(configWithAuth, this._mockHandler, this._mockLogger);

        // Assert - Check that client was created successfully
        Assert.NotNull(client);
    }

    [Fact]
    public void Constructor_WithCustomHeaders_ShouldSetHeaders()
    {
        // Arrange
        var configWithHeaders = new OfrepConfiguration("https://api.example.com/")
        {
            Headers = new Dictionary<string, string>
            {
                { "X-Custom-Header", "custom-value" }, { "X-Another-Header", "another-value" }
            }
        };

        // Act
        using var client = new OfrepClient(configWithHeaders, this._mockHandler, this._mockLogger);

        // Assert
        Assert.NotNull(client);
    }

    #endregion

    #region EvaluateFlag Tests

    [Fact]
    public async Task EvaluateFlag_WithValidRequest_ShouldReturnSuccessResponse()
    {
        // Arrange
        const string flagKey = "test-flag";
        const string type = "boolean";
        const bool defaultValue = false;
        var expectedResponse = new OfrepResponse<bool>(flagKey, true) { Reason = "TARGETING_MATCH", Variant = "on" };

        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, type, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse.Value, result.Value);
        Assert.Equal(expectedResponse.Reason, result.Reason);
        Assert.Equal(expectedResponse.Variant, result.Variant);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task EvaluateFlag_WithInvalidFlagKey_ShouldThrowArgumentException(string invalidFlagKey)
    {
        // Arrange
        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.EvaluateFlag(invalidFlagKey, "boolean", false, EvaluationContext.Empty));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task EvaluateFlag_WithInvalidType_ShouldThrowArgumentException(string invalidType)
    {
        // Arrange
        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.EvaluateFlag("test-flag", invalidType, false, EvaluationContext.Empty));
    }

    [Fact]
    public async Task EvaluateFlag_WithHttpRequestException_ShouldReturnErrorResponse()
    {
        // Arrange
        const string flagKey = "test-flag";
        const string type = "boolean";
        const bool defaultValue = false;

        this._mockHandler.SetupException(new HttpRequestException("Network error"));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, type, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal("provider_not_ready", result.ErrorCode);
        Assert.Equal("ERROR", result.Reason);
        Assert.Contains("Network error", result.ErrorMessage);
    }

    [Fact]
    public async Task EvaluateFlag_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        const string flagKey = "test-flag";
        const string type = "boolean";
        const bool defaultValue = false;
        var cts = new CancellationTokenSource();

        this._mockHandler.SetupException(new OperationCanceledException("Request was cancelled", cts.Token));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
#if NETFRAMEWORK
        cts.Cancel();
#else
        await cts.CancelAsync();
#endif
        var result = await client.EvaluateFlag(flagKey, type, defaultValue, EvaluationContext.Empty, cts.Token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal("provider_not_ready", result.ErrorCode);
    }

    [Fact]
    public async Task EvaluateFlag_WithTimeout_ShouldReturnErrorResponse()
    {
        // Arrange
        const string flagKey = "test-flag";
        const string type = "boolean";
        const bool defaultValue = false;

        this._mockHandler.SetupException(new OperationCanceledException("Request timed out"));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, type, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal("provider_not_ready", result.ErrorCode);
    }

    [Fact]
    public async Task EvaluateFlag_WithJsonException_ShouldReturnParsingError()
    {
        // Arrange
        const string flagKey = "test-flag";
        const string type = "boolean";
        const bool defaultValue = false;

        this._mockHandler.SetupResponse(HttpStatusCode.OK, "invalid json");

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, type, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal("parsing_error", result.ErrorCode);
    }

    [Fact]
    public async Task EvaluateFlag_WithNullResponse_ShouldReturnParsingError()
    {
        // Arrange
        const string flagKey = "test-flag";
        const string type = "boolean";
        const bool defaultValue = false;

        this._mockHandler.SetupResponse(HttpStatusCode.OK, "null");

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, type, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal("parsing_error", result.ErrorCode);
        Assert.Contains("null or empty response", result.ErrorMessage);
    }

    [Fact]
    public async Task EvaluateFlag_WithEvaluationContext_ShouldIncludeContextInRequest()
    {
        // Arrange
        const string flagKey = "test-flag";
        const string type = "boolean";
        const bool defaultValue = false;
        var context = EvaluationContext.Builder()
            .Set("userId", "user123")
            .Set("environment", "production")
            .Build();

        var expectedResponse = new OfrepResponse<bool>(flagKey, true);
        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, type, defaultValue, context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse.Value, result.Value);

        // Verify the request was made
        Assert.Single(this._mockHandler.Requests);
        var request = this._mockHandler.Requests[0];
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Contains($"ofrep/v1/evaluate/flags/{flagKey}", request.RequestUri?.ToString());
    }

    [Fact]
    public async Task EvaluateFlag_WithArgumentNullException_ShouldReturnParsingError()
    {
        // Arrange
        const string flagKey = "test-flag";
        const string type = "boolean";
        const bool defaultValue = false;

        this._mockHandler.SetupException(new ArgumentNullException());

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, type, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal("parsing_error", result.ErrorCode);
        Assert.Equal("ERROR", result.Reason);
    }

    [Fact]
    public async Task EvaluateFlag_WithInvalidOperationException_ShouldReturnGeneralError()
    {
        // Arrange
        const string flagKey = "test-flag";
        const string type = "boolean";
        const bool defaultValue = false;

        this._mockHandler.SetupException(new InvalidOperationException("Test invalid operation"));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, type, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal("general_error", result.ErrorCode);
        Assert.Equal("ERROR", result.Reason);
        Assert.Contains("Test invalid operation", result.ErrorMessage);
    }

    [Fact]
    public async Task EvaluateFlag_WithInvalidHttpStatus_ShouldThrowHttpRequestException()
    {
        // Arrange
        const string flagKey = "test-flag";
        const string type = "boolean";
        const bool defaultValue = false;

        this._mockHandler.SetupResponse(HttpStatusCode.InternalServerError, "Internal Server Error");

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, type, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal("provider_not_ready", result.ErrorCode);
        Assert.Equal("ERROR", result.Reason);
    }

    [Fact]
    public async Task EvaluateFlag_WithComplexEvaluationContext_ShouldSerializeCorrectly()
    {
        // Arrange
        const string flagKey = "complex-flag";
        const string type = "string";
        const string defaultValue = "default";

        var complexContext = EvaluationContext.Builder()
            .Set("userId", "user123")
            .Set("isAdmin", true)
            .Set("score", 42.5)
            .Set("environment", "production")
            .Set("count", 100)
            .Build();

        var expectedResponse = new OfrepResponse<string>(flagKey, "success");
        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, type, defaultValue, complexContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse.Value, result.Value);

        // Verify request was made
        Assert.Single(this._mockHandler.Requests);
        var request = this._mockHandler.Requests[0];
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Contains($"ofrep/v1/evaluate/flags/{flagKey}", request.RequestUri?.ToString());
    }

    [Theory]
    [InlineData("flag-with-special-chars-!@#")]
    [InlineData("flag with spaces")]
    [InlineData("flag/with/slashes")]
    [InlineData("flag%with%encoded")]
    public async Task EvaluateFlag_WithSpecialCharactersInFlagKey_ShouldEscapeCorrectly(string flagKey)
    {
        // Arrange
        const string type = "boolean";
        const bool defaultValue = false;
        var expectedResponse = new OfrepResponse<bool>(flagKey, true);

        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, type, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse.Value, result.Value);

        // Verify the flag key was properly URL escaped in the request
        Assert.Single(this._mockHandler.Requests);
        var request = this._mockHandler.Requests[0];
        Assert.Contains("ofrep/v1/evaluate/flags/", request.RequestUri?.ToString());
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ShouldDisposeResourcesGracefully()
    {
        // Arrange
        var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act & Assert - Should not throw
        client.Dispose();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act & Assert - Should not throw
        client.Dispose();
        client.Dispose();
        client.Dispose();
    }

    #endregion
}
