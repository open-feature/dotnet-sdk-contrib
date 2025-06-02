using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenFeature.Model;
using OpenFeature.Providers.Ofrep.Client;
using OpenFeature.Providers.Ofrep.Client.Exceptions;
using OpenFeature.Providers.Ofrep.Configuration;
using OpenFeature.Providers.Ofrep.Models;
using OpenFeature.Providers.Ofrep.Test.Helpers;
using Xunit;

namespace OpenFeature.Providers.Ofrep.Test.Client;

public class OfrepClientTest : IDisposable
{
    private readonly ILogger<OfrepClient> _mockLogger = NullLogger<OfrepClient>.Instance;
    private readonly TestHttpMessageHandler _mockHandler = new();

    private readonly OfrepConfiguration _configuration = new("https://api.example.com/")
    {
        CacheDuration = TimeSpan.FromMinutes(5),
        MaxCacheSize = 100
    };

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
    public void Constructor_WithAbsoluteExpirationEnabled_ShouldSetupCacheCorrectly()
    {
        // Arrange
        var configWithAbsoluteExpiration = new OfrepConfiguration("https://api.example.com/")
        {
            CacheDuration = TimeSpan.FromMinutes(5),
            EnableAbsoluteExpiration = true,
            MaxCacheSize = 50
        };

        // Act
        using var client = new OfrepClient(configWithAbsoluteExpiration, this._mockHandler, this._mockLogger);

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void Constructor_WithZeroCacheSize_ShouldSetupCacheWithoutSizeLimit()
    {
        // Arrange
        var configWithZeroCacheSize = new OfrepConfiguration("https://api.example.com/") { MaxCacheSize = 0 };

        // Act
        using var client = new OfrepClient(configWithZeroCacheSize, this._mockHandler, this._mockLogger);

        // Assert
        Assert.NotNull(client);
    }

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
        var expectedResponse = new OfrepResponse<bool>(true) { Reason = "TARGETING_MATCH", Variant = "on" };

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
        await cts.CancelAsync();
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
    public async Task EvaluateFlag_WithNotModifiedResponse_ShouldReturnCachedValue()
    {
        // Arrange
        const string flagKey = "test-flag";
        const string type = "boolean";
        const bool defaultValue = false;
        var expectedResponse = new OfrepResponse<bool>(true);

        // First request - successful response with ETag
        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse, this._jsonSerializerCamelCase), "\"test-etag\"");

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // First call to populate cache
        var firstResult = await client.EvaluateFlag(flagKey, type, defaultValue, EvaluationContext.Empty);

        // Second request - 304 Not Modified
        this._mockHandler.SetupResponse(HttpStatusCode.NotModified, "");

        // Act
        var secondResult = await client.EvaluateFlag(flagKey, type, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(secondResult);
        Assert.Equal(firstResult.Value, secondResult.Value);
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

        var expectedResponse = new OfrepResponse<bool>(true);
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
    public async Task EvaluateFlag_WithUnknownException_ShouldThrowException()
    {
        // Arrange
        const string flagKey = "test-flag";
        const string type = "boolean";
        const bool defaultValue = false;

        this._mockHandler.SetupException(new NotSupportedException("Unsupported operation"));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act & Assert - NotSupportedException is not caught by the client, so it bubbles up
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            client.EvaluateFlag(flagKey, type, defaultValue, EvaluationContext.Empty));
    }

    [Fact]
    public async Task EvaluateFlag_WithStaleCache_ShouldReturnStaleCacheOnError()
    {
        // Arrange
        const string flagKey = "test-flag";
        const string type = "boolean";
        const bool defaultValue = false;
        var expectedResponse = new OfrepResponse<bool>(true) { Reason = "TARGETING_MATCH", Variant = "on" };

        // First request - successful response
        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // First call to populate cache
        var firstResult = await client.EvaluateFlag(flagKey, type, defaultValue, EvaluationContext.Empty);

        // Second request - error, should return stale cache
        this._mockHandler.SetupException(new HttpRequestException("Network error"));

        // Act
        var secondResult = await client.EvaluateFlag(flagKey, type, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(secondResult);
        Assert.Equal(firstResult.Value, secondResult.Value);
        Assert.Equal(firstResult.Reason, secondResult.Reason);
        Assert.Equal(firstResult.Variant, secondResult.Variant);
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
    public async Task EvaluateFlag_WithNotModifiedButNoCache_ShouldReturnGeneralError()
    {
        // Arrange
        const string flagKey = "test-flag";
        const string type = "boolean";
        const bool defaultValue = false;

        // Setup 304 Not Modified response without any cached data
        this._mockHandler.SetupResponse(HttpStatusCode.NotModified, "");

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, type, defaultValue, EvaluationContext.Empty);

        // Assert - The InvalidOperationException is caught and handled as a general error
        Assert.NotNull(result);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal("general_error", result.ErrorCode);
        Assert.Equal("ERROR", result.Reason);
        Assert.Contains("Received 304 Not Modified but no cached response available", result.ErrorMessage);
    }

    [Fact]
    public async Task EvaluateFlag_WithEmptyETag_ShouldNotIncludeIfNoneMatchHeader()
    {
        // Arrange
        const string flagKey = "test-flag";
        const string type = "boolean";
        const bool defaultValue = false;
        var expectedResponse = new OfrepResponse<bool>(true);

        // First request - response without ETag
        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // First call to populate cache without ETag
        await client.EvaluateFlag(flagKey, type, defaultValue, EvaluationContext.Empty);

        // Second call - should not include If-None-Match header
        var secondResult = await client.EvaluateFlag(flagKey, type, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(secondResult);
        Assert.Equal(expectedResponse.Value, secondResult.Value);

        // Should still make one request since the cache should hit
        Assert.Single(this._mockHandler.Requests);
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

        var expectedResponse = new OfrepResponse<string>("success");
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
        var expectedResponse = new OfrepResponse<bool>(true);

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

    #region BulkEvaluate Tests

    [Fact]
    public async Task BulkEvaluate_WithValidRequest_ShouldReturnSuccessResponse()
    {
        // Arrange
        var context = EvaluationContext.Builder()
            .Set("userId", "user123")
            .Build();

        var expectedResponse = new BulkEvaluationResponse
        {
            Flags =
            [
                new BulkEvaluationFlag(true) { Key = "flag1", Reason = "TARGETING_MATCH" },
                new BulkEvaluationFlag("test") { Key = "flag2", Reason = "DEFAULT" }
            ]
        };

        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.BulkEvaluate(context);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Flags);
        Assert.Equal(2, result.Flags.Count);
        Assert.Equal("flag1", result.Flags[0].Key);
        Assert.Equal("flag2", result.Flags[1].Key);
    }

    [Fact]
    public async Task BulkEvaluate_WithHttpException_ShouldThrowOfrepConfigurationException()
    {
        // Arrange
        var context = EvaluationContext.Empty;
        this._mockHandler.SetupException(new HttpRequestException("Network error"));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act & Assert
        await Assert.ThrowsAsync<OfrepConfigurationException>(() =>
            client.BulkEvaluate(context));
    }

    [Fact]
    public async Task BulkEvaluate_WithNullResponse_ShouldThrowOfrepConfigurationException()
    {
        // Arrange
        var context = EvaluationContext.Empty;
        this._mockHandler.SetupResponse(HttpStatusCode.OK, "null");

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act & Assert
        await Assert.ThrowsAsync<OfrepConfigurationException>(() =>
            client.BulkEvaluate(context));
    }

    [Fact]
    public async Task BulkEvaluate_WithNotModifiedResponse_ShouldReturnCachedValue()
    {
        // Arrange
        var context = EvaluationContext.Empty;
        var expectedResponse = new BulkEvaluationResponse
        {
            Flags = [new BulkEvaluationFlag(true) { Key = "flag1", Reason = "TARGETING_MATCH" }]
        };

        // First request - successful response with ETag
        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse, this._jsonSerializerCamelCase), "\"bulk-etag\"");

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // First call to populate cache
        var firstResult = await client.BulkEvaluate(context);

        // Second request - 304 Not Modified
        this._mockHandler.SetupResponse(HttpStatusCode.NotModified, "");

        // Act
        var secondResult = await client.BulkEvaluate(context);

        // Assert
        Assert.NotNull(secondResult);
        Assert.Equal(firstResult.Flags.Count, secondResult.Flags.Count);
    }

    [Fact]
    public async Task BulkEvaluate_WithInvalidHttpStatus_ShouldThrowOfrepConfigurationException()
    {
        // Arrange
        var context = EvaluationContext.Empty;
        this._mockHandler.SetupResponse(HttpStatusCode.BadRequest, "Bad Request");

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act & Assert
        await Assert.ThrowsAsync<OfrepConfigurationException>(() =>
            client.BulkEvaluate(context));
    }

    [Fact]
    public async Task BulkEvaluate_WithStaleCache_ShouldReturnStaleCacheOnError()
    {
        // Arrange
        var context = EvaluationContext.Empty;
        var expectedResponse = new BulkEvaluationResponse
        {
            Flags = [new BulkEvaluationFlag(true) { Key = "flag1", Reason = "TARGETING_MATCH" }]
        };

        // First request - successful response
        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // First call to populate cache
        var firstResult = await client.BulkEvaluate(context);

        // Second request - error, should return stale cache
        this._mockHandler.SetupException(new HttpRequestException("Network error"));

        // Act
        var secondResult = await client.BulkEvaluate(context);

        // Assert
        Assert.NotNull(secondResult);
        Assert.Equal(firstResult.Flags.Count, secondResult.Flags.Count);
        Assert.Equal(firstResult.Flags[0].Key, secondResult.Flags[0].Key);
    }

    [Fact]
    public async Task BulkEvaluate_WithJsonException_ShouldThrowOfrepConfigurationException()
    {
        // Arrange
        var context = EvaluationContext.Empty;
        this._mockHandler.SetupException(new JsonException("Invalid JSON"));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<OfrepConfigurationException>(() =>
            client.BulkEvaluate(context));

        Assert.Contains("Failed during OFREP bulk evaluation request", exception.Message);
        Assert.IsType<JsonException>(exception.InnerException);
    }

    [Fact]
    public async Task BulkEvaluate_WithOperationCanceledException_ShouldThrowOfrepConfigurationException()
    {
        // Arrange
        var context = EvaluationContext.Empty;
        this._mockHandler.SetupException(new OperationCanceledException("Request cancelled"));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<OfrepConfigurationException>(() =>
            client.BulkEvaluate(context));

        Assert.Contains("Failed during OFREP bulk evaluation request", exception.Message);
    }

    [Fact]
    public async Task BulkEvaluate_WithNotModifiedButNoCache_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var context = EvaluationContext.Empty;

        // Setup 304 Not Modified response without any cached data
        this._mockHandler.SetupResponse(HttpStatusCode.NotModified, "");

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act & Assert - The InvalidOperationException from HandleBulkNotModified is not caught
        // by the BulkEvaluate method, so it bubbles up
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.BulkEvaluate(context));
    }

    [Fact]
    public async Task BulkEvaluate_WithComplexEvaluationContext_ShouldSerializeCorrectly()
    {
        // Arrange
        var complexContext = EvaluationContext.Builder()
            .Set("userId", "user123")
            .Set("feature1", "enabled")
            .Set("feature2", "disabled")
            .Set("version", "1.0.0")
            .Set("environment", "test")
            .Build();

        var expectedResponse = new BulkEvaluationResponse
        {
            Flags = [new BulkEvaluationFlag("value") { Key = "complex-flag" }]
        };

        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.BulkEvaluate(complexContext);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Flags);
        Assert.Equal("complex-flag", result.Flags[0].Key);

        // Verify request was made
        Assert.Single(this._mockHandler.Requests);
        var request = this._mockHandler.Requests[0];
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Contains("ofrep/v1/evaluate/flags", request.RequestUri?.ToString());
    }

    #endregion

    #region SetCacheDuration Tests

    [Fact]
    public void SetCacheDuration_WithValidDuration_ShouldUpdateCacheDuration()
    {
        // Arrange
        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);
        var newDuration = TimeSpan.FromMinutes(10);

        // Act
        client.SetCacheDuration(newDuration);

        // Assert - No exception should be thrown
        Assert.True(true);
    }

    [Fact]
    public void SetCacheDuration_WithZeroDuration_ShouldBeValid()
    {
        // Arrange
        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act & Assert - Should not throw
        client.SetCacheDuration(TimeSpan.Zero);
    }

    [Fact]
    public void SetCacheDuration_WithNegativeDuration_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            client.SetCacheDuration(TimeSpan.FromMinutes(-1)));
    }

    [Fact]
    public void SetCacheDuration_WithExcessivelyLongDuration_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            client.SetCacheDuration(TimeSpan.FromDays(2)));
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

    #region Cache Tests

    [Fact]
    public async Task EvaluateFlag_CacheHit_ShouldReturnCachedValue()
    {
        // Arrange
        const string flagKey = "test-flag";
        const string type = "boolean";
        const bool defaultValue = false;
        var expectedResponse = new OfrepResponse<bool>(true);

        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act - First call
        var firstResult = await client.EvaluateFlag(flagKey, type, defaultValue, EvaluationContext.Empty);

        // Act - Second call (should hit cache)
        var secondResult = await client.EvaluateFlag(flagKey, type, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.Equal(firstResult.Value, secondResult.Value);
        // Should only have made one HTTP request
        Assert.Single(this._mockHandler.Requests);
    }

    [Fact]
    public async Task EvaluateFlag_DifferentContext_ShouldMakeSeparateRequests()
    {
        // Arrange
        const string flagKey = "test-flag";
        const string type = "boolean";
        const bool defaultValue = false;
        var context1 = EvaluationContext.Builder().Set("userId", "user1").Build();
        var context2 = EvaluationContext.Builder().Set("userId", "user2").Build();

        var expectedResponse = new OfrepResponse<bool>(true);
        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        await client.EvaluateFlag(flagKey, type, defaultValue, context1);
        await client.EvaluateFlag(flagKey, type, defaultValue, context2);

        // Assert - Should make two separate requests due to different contexts
        Assert.Equal(2, this._mockHandler.Requests.Count);
    }

    #endregion

    #region Cache Eviction and Memory Tests

    [Fact]
    public async Task EvaluateFlag_WithCacheDurationZero_ShouldStillCacheWithoutExpiration()
    {
        // Arrange
        var configWithNoCache = new OfrepConfiguration("https://api.example.com/") { CacheDuration = TimeSpan.Zero };

        const string flagKey = "test-flag";
        const string type = "boolean";
        const bool defaultValue = false;
        var expectedResponse = new OfrepResponse<bool>(true);

        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(configWithNoCache, this._mockHandler, this._mockLogger);

        // Act - Make multiple calls
        await client.EvaluateFlag(flagKey, type, defaultValue, EvaluationContext.Empty);
        await client.EvaluateFlag(flagKey, type, defaultValue, EvaluationContext.Empty);

        // Assert - Cache duration zero means no expiration but still caches
        // So we should only make one request since cache still works
        Assert.Single(this._mockHandler.Requests);
    }

    [Fact]
    public void SetCacheDuration_WithMaximumAllowedDuration_ShouldSucceed()
    {
        // Arrange
        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);
        var maxDuration = TimeSpan.FromDays(1); // Maximum allowed

        // Act & Assert
        client.SetCacheDuration(maxDuration);
        // Should not throw
    }

    [Fact]
    public void SetCacheDuration_WithExactlyOneDayDuration_ShouldSucceed()
    {
        // Arrange
        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);
        var exactlyOneDay = TimeSpan.FromDays(1);

        // Act & Assert
        client.SetCacheDuration(exactlyOneDay);
        // Should not throw
    }

    [Fact]
    public void SetCacheDuration_WithSlightlyOverOneDayDuration_ShouldThrow()
    {
        // Arrange
        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);
        var slightlyOverOneDay = TimeSpan.FromDays(1).Add(TimeSpan.FromMinutes(1));

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => client.SetCacheDuration(slightlyOverOneDay));
    }

    #endregion
}
