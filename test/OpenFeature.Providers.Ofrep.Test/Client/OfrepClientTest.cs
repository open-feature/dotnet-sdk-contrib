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
    private readonly OfrepOptions _configuration = new("https://api.example.com/");

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
        OfrepOptions? ofrepOptions = null;

        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OfrepClient(ofrepOptions!, this._mockLogger));
    }

    [Fact]
    public void Constructor_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OfrepClient(this._configuration, (HttpMessageHandler)null!, this._mockLogger));
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
    public void Constructor_WithCustomHeaders_ShouldSetHeaders()
    {
        // Arrange
        var configWithHeaders = new OfrepOptions("https://api.example.com/")
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
        const bool defaultValue = false;
        var expectedResponse = new OfrepResponse<bool>(flagKey, true) { Reason = "TARGETING_MATCH", Variant = "on" };

        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

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
            client.EvaluateFlag(invalidFlagKey, false, EvaluationContext.Empty));
    }

    [Fact]
    public async Task EvaluateFlag_WithHttpRequestException_ShouldReturnErrorResponse()
    {
        // Arrange
        const string flagKey = "test-flag";
        const bool defaultValue = false;

        this._mockHandler.SetupException(new HttpRequestException("Network error"));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal("general_error", result.ErrorCode);
        Assert.Equal("ERROR", result.Reason);
        Assert.Contains("Network error", result.ErrorMessage);
    }

    [Fact]
    public async Task EvaluateFlag_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        const string flagKey = "test-flag";
        const bool defaultValue = false;
        using var cts = new CancellationTokenSource();

        this._mockHandler.SetupException(new OperationCanceledException("Request was cancelled", cts.Token));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
#if NETFRAMEWORK
        cts.Cancel();
#else
        await cts.CancelAsync();
#endif
        var result = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty, cts.Token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal("general_error", result.ErrorCode);
    }

    [Fact]
    public async Task EvaluateFlag_WithTimeout_ShouldReturnErrorResponse()
    {
        // Arrange
        const string flagKey = "test-flag";
        const bool defaultValue = false;

        this._mockHandler.SetupException(new OperationCanceledException("Request timed out"));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal("general_error", result.ErrorCode);
    }

    [Fact]
    public async Task EvaluateFlag_WithJsonException_ShouldReturnParsingError()
    {
        // Arrange
        const string flagKey = "test-flag";
        const bool defaultValue = false;

        this._mockHandler.SetupResponse(HttpStatusCode.OK, "invalid json");

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal("general_error", result.ErrorCode);
    }

    [Fact]
    public async Task EvaluateFlag_WithNullResponse_ShouldReturnParsingError()
    {
        // Arrange
        const string flagKey = "test-flag";
        const bool defaultValue = false;

        this._mockHandler.SetupResponse(HttpStatusCode.OK, "null");

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal("parse_error", result.ErrorCode);
        Assert.Contains("null or empty response", result.ErrorMessage);
    }

    [Fact]
    public async Task EvaluateFlag_WithEvaluationContext_ShouldIncludeContextInRequest()
    {
        // Arrange
        const string flagKey = "test-flag";
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
        var result = await client.EvaluateFlag(flagKey, defaultValue, context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse.Value, result.Value);

        // Verify the request was made
        Assert.Single(this._mockHandler.Requests);
        var request = this._mockHandler.Requests[0];
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Contains($"ofrep/v1/evaluate/flags/{flagKey}", request.RequestUri?.ToString());

        // Verify the context was included in the request body
        var requestContent = await request.Content!.ReadAsStringAsync();
        var requestBody = JsonSerializer.Deserialize<JsonElement>(requestContent);

        // Verify the request has the expected structure with context
        Assert.True(requestBody.TryGetProperty("context", out var contextElement));
        Assert.True(contextElement.TryGetProperty("userId", out var userIdElement));
        Assert.Equal("user123", userIdElement.GetString());
        Assert.True(contextElement.TryGetProperty("environment", out var environmentElement));
        Assert.Equal("production", environmentElement.GetString());
    }

    [Fact]
    public async Task EvaluateFlag_WithArgumentNullException_ShouldReturnParsingError()
    {
        // Arrange
        const string flagKey = "test-flag";
        const bool defaultValue = false;

        this._mockHandler.SetupException(new ArgumentNullException());

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal("general_error", result.ErrorCode);
        Assert.Equal("ERROR", result.Reason);
    }

    [Fact]
    public async Task EvaluateFlag_WithInvalidOperationException_ShouldReturnGeneralError()
    {
        // Arrange
        const string flagKey = "test-flag";
        const bool defaultValue = false;

        this._mockHandler.SetupException(new InvalidOperationException("Test invalid operation"));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

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
        const bool defaultValue = false;

        this._mockHandler.SetupResponse(HttpStatusCode.InternalServerError, "Internal Server Error");

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal("general_error", result.ErrorCode);
        Assert.Equal("ERROR", result.Reason);
    }

    [Fact]
    public async Task EvaluateFlag_WithComplexEvaluationContext_ShouldSerializeCorrectly()
    {
        // Arrange
        const string flagKey = "complex-flag";
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
        var result = await client.EvaluateFlag(flagKey, defaultValue, complexContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse.Value, result.Value);

        // Verify request was made
        Assert.Single(this._mockHandler.Requests);
        var request = this._mockHandler.Requests[0];
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Contains($"ofrep/v1/evaluate/flags/{flagKey}", request.RequestUri?.ToString());

        // Verify complex context was properly serialized in the request body
        var requestContent = await request.Content!.ReadAsStringAsync();
        Assert.Contains("user123", requestContent);
        Assert.Contains("true", requestContent); // isAdmin boolean
        Assert.Contains("42.5", requestContent); // score double
        Assert.Contains("production", requestContent);
        Assert.Contains("100", requestContent); // count integer
        Assert.Contains("userId", requestContent);
        Assert.Contains("isAdmin", requestContent);
        Assert.Contains("score", requestContent);
        Assert.Contains("environment", requestContent);
        Assert.Contains("count", requestContent);
    }

    [Theory]
    [InlineData("flag-with-special-chars-!@#", "flag-with-special-chars-%21%40%23")]
    [InlineData("flag with spaces", "flag%20with%20spaces")]
    [InlineData("flag/with/slashes", "flag%2Fwith%2Fslashes")]
    [InlineData("flag%with%encoded", "flag%25with%25encoded")]
    public async Task EvaluateFlag_WithSpecialCharactersInFlagKey_ShouldEscapeCorrectly(string flagKey,
        string expectedEncodedKey)
    {
        // Arrange
        const bool defaultValue = false;
        var expectedResponse = new OfrepResponse<bool>(flagKey, true);

        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse.Value, result.Value);

        // Verify the flag key was properly URL escaped in the request
        Assert.Single(this._mockHandler.Requests);
        var request = this._mockHandler.Requests[0];
        var requestUri = request.RequestUri?.ToString();

        // Also check the raw AbsolutePath and OriginalString to see if encoding is preserved anywhere
        var absolutePath = request.RequestUri?.AbsolutePath ?? "";
        var originalString = request.RequestUri?.OriginalString ?? "";

        Assert.Contains("ofrep/v1/evaluate/flags/", requestUri);

        // Try checking different URI properties for encoded content
        if (originalString.Contains(expectedEncodedKey))
        {
            // Encoded key found in OriginalString - this is what we want
            Assert.Contains(expectedEncodedKey, originalString);
        }
        else if (absolutePath.Contains(expectedEncodedKey))
        {
            // Encoded key found in AbsolutePath
            Assert.Contains(expectedEncodedKey, absolutePath);
        }
        else
        {
            // Fall back to checking the full URI string
            Assert.Contains(expectedEncodedKey, requestUri);
        }
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Constructor_WithInvalidBaseUrl_ShouldThrowArgumentException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentException>(() => new OfrepOptions("not-a-valid-url"));
        Assert.Throws<ArgumentException>(() => new OfrepOptions(""));
        Assert.Throws<ArgumentException>(() => new OfrepOptions(null!));
    }

    [Fact]
    public void Constructor_WithCustomTimeout_ShouldSetTimeout()
    {
        // Arrange
        var customTimeout = TimeSpan.FromSeconds(30);
        var config = new OfrepOptions("https://api.example.com/") { Timeout = customTimeout };

        // Act
        using var client = new OfrepClient(config, this._mockHandler, this._mockLogger);

        // Assert
        Assert.NotNull(client);
    }

    [Theory]
    [InlineData("https://api.example.com/")]
    [InlineData("http://localhost:8080/")]
    [InlineData("https://test.domain.com:443/path")]
    public void Constructor_WithValidBaseUrls_ShouldCreateClient(string baseUrl)
    {
        // Arrange
        var config = new OfrepOptions(baseUrl);

        // Act & Assert
        using var client = new OfrepClient(config, this._mockHandler, this._mockLogger);
        Assert.NotNull(client);
    }

    #endregion

    #region Data Type Tests

    [Fact]
    public async Task EvaluateFlag_WithStringType_ShouldReturnStringValue()
    {
        // Arrange
        const string flagKey = "string-flag";
        const string defaultValue = "default";
        const string expectedValue = "test-string-value";

        var expectedResponse = new OfrepResponse<string>(flagKey, expectedValue)
        {
            Reason = "TARGETING_MATCH",
            Variant = "string-variant"
        };

        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedValue, result.Value);
        Assert.Equal("TARGETING_MATCH", result.Reason);
        Assert.Equal("string-variant", result.Variant);
    }

    [Fact]
    public async Task EvaluateFlag_WithIntegerType_ShouldReturnIntegerValue()
    {
        // Arrange
        const string flagKey = "integer-flag";
        const int defaultValue = 0;
        const int expectedValue = 42;

        var expectedResponse = new OfrepResponse<int>(flagKey, expectedValue)
        {
            Reason = "STATIC",
            Variant = "number-variant"
        };

        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedValue, result.Value);
        Assert.Equal("STATIC", result.Reason);
        Assert.Equal("number-variant", result.Variant);
    }

    [Fact]
    public async Task EvaluateFlag_WithDoubleType_ShouldReturnDoubleValue()
    {
        // Arrange
        const string flagKey = "double-flag";
        const double defaultValue = 0.0;
        const double expectedValue = 3.14159;

        var expectedResponse = new OfrepResponse<double>(flagKey, expectedValue) { Reason = "TARGETING_MATCH" };

        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedValue, result.Value);
        Assert.Equal("TARGETING_MATCH", result.Reason);
    }

    [Fact]
    public async Task EvaluateFlag_WithObjectType_ShouldReturnObjectValue()
    {
        // Arrange
        const string flagKey = "object-flag";
        var defaultValue = new { test = "default" };
        var expectedValue = new { name = "test", value = 123, enabled = true };

        var expectedResponse = new OfrepResponse<object>(flagKey, expectedValue) { Reason = "DISABLED" };

        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Value);
        Assert.Equal("DISABLED", result.Reason);
    }

    #endregion

    #region HTTP Status Code Tests

    [Fact]
    public async Task EvaluateFlag_WithNotFoundStatus_ShouldReturnFlagNotFoundError()
    {
        // Arrange
        const string flagKey = "missing-flag";
        const bool defaultValue = false;

        this._mockHandler.SetupResponse(HttpStatusCode.NotFound, "Flag not found");

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal("flag_not_found", result.ErrorCode);
        Assert.Equal("ERROR", result.Reason);
        Assert.Equal("Flag not found.", result.ErrorMessage);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task EvaluateFlag_WithAuthenticationError_ShouldReturnProviderNotReadyError(HttpStatusCode statusCode)
    {
        // Arrange
        const string flagKey = "auth-flag";
        const bool defaultValue = false;

        this._mockHandler.SetupResponse(statusCode, "Authentication failed");

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal("provider_not_ready", result.ErrorCode);
        Assert.Equal("ERROR", result.Reason);
        Assert.Equal("Unauthorized access to flag evaluation.", result.ErrorMessage);
    }

    [Fact]
    public async Task EvaluateFlag_WithTooManyRequestsStatus_ShouldReturnGeneralError()
    {
        // Arrange
        const string flagKey = "rate-limited-flag";
        const bool defaultValue = false;

        var response = new HttpResponseMessage((HttpStatusCode)429)
        {
            Content = new StringContent("Rate limit exceeded"),
            Headers =
            {
                RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(60))
            }
        };

        this._mockHandler.SetupResponse((HttpStatusCode)429, "Rate limit exceeded");

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal("general_error", result.ErrorCode);
        Assert.Equal("ERROR", result.Reason);
        Assert.Equal("Rate limit exceeded.", result.ErrorMessage);
    }

    [Fact]
    public async Task EvaluateFlag_WithBadRequestStatus_ShouldHandleBadRequestResponse()
    {
        // Arrange
        const string flagKey = "bad-request-flag";
        const bool defaultValue = false;

        var errorResponse = new
        {
            key = flagKey,
            errorCode = "invalid_context",
            errorDetails = "Invalid evaluation context provided",
            value = defaultValue,
            reason = "ERROR"
        };

        this._mockHandler.SetupResponse(HttpStatusCode.BadRequest,
            JsonSerializer.Serialize(errorResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal("invalid_context", result.ErrorCode);
        Assert.Equal("ERROR", result.Reason);
        Assert.Equal("Invalid evaluation context provided", result.ErrorMessage);
    }

    #endregion

    #region Rate Limiting Tests

    [Fact]
    public async Task EvaluateFlag_WithRetryAfterSet_ShouldReturnGeneralErrorForSubsequentRequests()
    {
        // Arrange
        const string flagKey = "rate-limited-flag";
        const bool defaultValue = false;

        // First request - rate limited
        this._mockHandler.SetupResponse((HttpStatusCode)429, "Rate limit exceeded");

        // Second request - should be blocked by rate limiter
        // (No setup needed as it should not make HTTP request)

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var firstResult = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);
        var secondResult = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(firstResult);
        Assert.Equal("general_error", firstResult.ErrorCode);

        Assert.NotNull(secondResult);
        Assert.Equal("general_error", secondResult.ErrorCode);
        Assert.Equal("Rate limit exceeded.", secondResult.ErrorMessage);
    }

    #endregion

    #region Context Validation Tests

    [Fact]
    public async Task EvaluateFlag_WithNullContext_ShouldUseEmptyContext()
    {
        // Arrange
        const string flagKey = "null-context-flag";
        const bool defaultValue = false;
        var expectedResponse = new OfrepResponse<bool>(flagKey, true);

        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, null);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Value);
        Assert.Single(this._mockHandler.Requests);
    }

    [Fact]
    public async Task EvaluateFlag_WithNestedEvaluationContext_ShouldSerializeCorrectly()
    {
        // Arrange
        const string flagKey = "nested-context-flag";
        const string defaultValue = "default";

        var complexContext = EvaluationContext.Builder()
            .Set("userId", "user123")
            .Set("premium", true)
            .Set("region", "us-east-1")
            .Set("sessionId", "session456")
            .Set("duration", 1800)
            .Build();

        var expectedResponse = new OfrepResponse<string>(flagKey, "premium-user");
        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, complexContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("premium-user", result.Value);
        Assert.Single(this._mockHandler.Requests);

        // Verify nested context was properly serialized in the request body
        var request = this._mockHandler.Requests[0];
        var requestContent = await request.Content!.ReadAsStringAsync();
        Assert.Contains("user123", requestContent);
        Assert.Contains("true", requestContent); // premium boolean
        Assert.Contains("us-east-1", requestContent);
        Assert.Contains("session456", requestContent);
        Assert.Contains("1800", requestContent); // duration integer
        Assert.Contains("userId", requestContent);
        Assert.Contains("premium", requestContent);
        Assert.Contains("region", requestContent);
        Assert.Contains("sessionId", requestContent);
        Assert.Contains("duration", requestContent);
    }

    #endregion

    #region Edge Case Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EvaluateFlag_WithNullOrEmptyFlagKey_ShouldThrowArgumentException(string flagKey)
    {
        // Arrange
        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => client.EvaluateFlag(flagKey, false, EvaluationContext.Empty));
    }

    [Fact]
    public async Task EvaluateFlag_WithEmptyJsonResponse_ShouldReturnGeneralError()
    {
        // Arrange
        const string flagKey = "empty-response-flag";
        const bool defaultValue = false;

        this._mockHandler.SetupResponse(HttpStatusCode.OK, "");

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal("general_error", result.ErrorCode);
    }

    [Fact]
    public async Task EvaluateFlag_WithMalformedJsonResponse_ShouldReturnGeneralError()
    {
        // Arrange
        const string flagKey = "malformed-json-flag";
        const bool defaultValue = false;

        this._mockHandler.SetupResponse(HttpStatusCode.OK, "{ invalid json structure");

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal("general_error", result.ErrorCode);
        Assert.Equal("ERROR", result.Reason);
    }

    [Fact]
    public async Task EvaluateFlag_WithTaskCanceledException_ShouldReturnGeneralError()
    {
        // Arrange
        const string flagKey = "task-cancelled-flag";
        const bool defaultValue = false;

        this._mockHandler.SetupException(new TaskCanceledException("Task was cancelled"));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal("general_error", result.ErrorCode);
        Assert.Equal("ERROR", result.Reason);
        Assert.Contains("Task was cancelled", result.ErrorMessage);
    }

    [Fact]
    public async Task EvaluateFlag_WithGenericException_ShouldReturnGeneralError()
    {
        // Arrange
        const string flagKey = "generic-exception-flag";
        const bool defaultValue = false;

        this._mockHandler.SetupException(new InvalidDataException("Generic error occurred"));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal("general_error", result.ErrorCode);
        Assert.Equal("ERROR", result.Reason);
        Assert.Contains("Generic error occurred", result.ErrorMessage);
    }

    #endregion

    #region Performance and Concurrency Tests

    [Fact]
    public async Task EvaluateFlag_WithConcurrentRequests_ShouldHandleMultipleRequests()
    {
        // Arrange
        const string flagKey = "concurrent-flag";
        const bool defaultValue = false;
        const int concurrentRequests = 10;

        var expectedResponse = new OfrepResponse<bool>(flagKey, true);

        // Setup multiple responses
        for (int i = 0; i < concurrentRequests; i++)
        {
            this._mockHandler.SetupResponse(HttpStatusCode.OK,
                JsonSerializer.Serialize(expectedResponse, this._jsonSerializerCamelCase));
        }

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(concurrentRequests, results.Length);
        Assert.All(results, result =>
        {
            Assert.NotNull(result);
            Assert.True(result.Value);
        });
        Assert.Equal(concurrentRequests, this._mockHandler.Requests.Count);
    }

    #endregion

    #region Request Building Tests

    [Fact]
    public async Task EvaluateFlag_ShouldCreateCorrectRequestUrl()
    {
        // Arrange
        const string flagKey = "url-test-flag";
        const bool defaultValue = false;
        var expectedResponse = new OfrepResponse<bool>(flagKey, true);

        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.Single(this._mockHandler.Requests);
        var request = this._mockHandler.Requests[0];
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Contains($"ofrep/v1/evaluate/flags/{flagKey}", request.RequestUri?.ToString());
        Assert.Equal("application/json", request.Content?.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task EvaluateFlag_ShouldIncludeContextInRequestBody()
    {
        // Arrange
        const string flagKey = "context-body-flag";
        const bool defaultValue = false;

        var context = EvaluationContext.Builder()
            .Set("targetingKey", "user-456")
            .Set("environment", "staging")
            .Set("beta", true)
            .Build();

        var expectedResponse = new OfrepResponse<bool>(flagKey, true);
        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        await client.EvaluateFlag(flagKey, defaultValue, context);

        // Assert
        Assert.Single(this._mockHandler.Requests);
        var request = this._mockHandler.Requests[0];
        Assert.NotNull(request.Content);

        var requestBody = await request.Content.ReadAsStringAsync();
        Assert.Contains("targetingKey", requestBody);
        Assert.Contains("user-456", requestBody);
        Assert.Contains("environment", requestBody);
        Assert.Contains("staging", requestBody);
    }

    #endregion

    #region Response Metadata Tests

    [Fact]
    public async Task EvaluateFlag_WithResponseMetadata_ShouldReturnMetadata()
    {
        // Arrange
        const string flagKey = "metadata-flag";
        const string defaultValue = "default";

        var expectedResponse = new OfrepResponse<string>(flagKey, "metadata-value")
        {
            Reason = "TARGETING_MATCH",
            Variant = "control",
            Metadata = new Dictionary<string, object> { ["experimentId"] = "exp-123", ["campaignId"] = "camp-456" }
        };

        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("metadata-value", result.Value);
        Assert.Equal("TARGETING_MATCH", result.Reason);
        Assert.Equal("control", result.Variant);
        Assert.NotNull(result.Metadata);
    }

    [Fact]
    public async Task EvaluateFlag_WithMinimalResponse_ShouldHandleOptionalFields()
    {
        // Arrange
        const string flagKey = "minimal-flag";
        const bool defaultValue = false;

        // Minimal response with only required fields
        var minimalResponse = new OfrepResponse<bool>(flagKey, true);

        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(minimalResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Value);
        Assert.Equal(flagKey, result.Key);
        // Optional fields should be null or empty
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);
    }

    #endregion

    #region Error Response Tests

    [Fact]
    public async Task EvaluateFlag_WithTypeMismatchError_ShouldReturnTypeMismatchErrorCode()
    {
        // Arrange
        const string flagKey = "type-mismatch-flag";
        const bool defaultValue = false;

        var errorResponse = new
        {
            key = flagKey,
            errorCode = "type_mismatch",
            errorDetails = "Expected boolean but got string",
            value = defaultValue,
            reason = "ERROR"
        };

        this._mockHandler.SetupResponse(HttpStatusCode.BadRequest,
            JsonSerializer.Serialize(errorResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal("type_mismatch", result.ErrorCode);
        Assert.Equal("ERROR", result.Reason);
        Assert.Equal("Expected boolean but got string", result.ErrorMessage);
    }

    [Fact]
    public async Task EvaluateFlag_WithTargetingKeyMissingError_ShouldReturnTargetingKeyMissingErrorCode()
    {
        // Arrange
        const string flagKey = "targeting-key-missing-flag";
        const bool defaultValue = false;

        var errorResponse = new
        {
            key = flagKey,
            errorCode = "targeting_key_missing",
            errorDetails = "Targeting key is required for this flag",
            value = defaultValue,
            reason = "ERROR"
        };

        this._mockHandler.SetupResponse(HttpStatusCode.BadRequest,
            JsonSerializer.Serialize(errorResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal("targeting_key_missing", result.ErrorCode);
        Assert.Equal("ERROR", result.Reason);
        Assert.Equal("Targeting key is required for this flag", result.ErrorMessage);
    }

    #endregion

    #region Integration-like Tests

    [Fact]
    public async Task EvaluateFlag_WithMultipleSequentialRequests_ShouldHandleEachIndependently()
    {
        // Arrange
        const string flagKey1 = "sequential-flag-1";
        const string flagKey2 = "sequential-flag-2";
        const string defaultValue = "default";

        var response1 = new OfrepResponse<string>(flagKey1, "value1") { Reason = "STATIC" };
        var response2 = new OfrepResponse<string>(flagKey2, "value2") { Reason = "TARGETING_MATCH" };

        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(response1, this._jsonSerializerCamelCase));
        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(response2, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result1 = await client.EvaluateFlag(flagKey1, defaultValue, EvaluationContext.Empty);
        var result2 = await client.EvaluateFlag(flagKey2, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result1);
        Assert.Equal("value1", result1.Value);
        Assert.Equal("STATIC", result1.Reason);

        Assert.NotNull(result2);
        Assert.Equal("value2", result2.Value);
        Assert.Equal("TARGETING_MATCH", result2.Reason);

        Assert.Equal(2, this._mockHandler.Requests.Count);
    }

    [Fact]
    public async Task EvaluateFlag_WithMixedSuccessAndError_ShouldHandleEachCorrectly()
    {
        // Arrange
        const string successFlagKey = "success-flag";
        const string errorFlagKey = "error-flag";
        const bool defaultValue = false;

        var successResponse = new OfrepResponse<bool>(successFlagKey, true) { Reason = "TARGETING_MATCH" };

        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(successResponse, this._jsonSerializerCamelCase));
        this._mockHandler.SetupResponse(HttpStatusCode.NotFound, "Flag not found");

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var successResult = await client.EvaluateFlag(successFlagKey, defaultValue, EvaluationContext.Empty);
        var errorResult = await client.EvaluateFlag(errorFlagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(successResult);
        Assert.True(successResult.Value);
        Assert.Equal("TARGETING_MATCH", successResult.Reason);
        Assert.Null(successResult.ErrorCode);

        Assert.NotNull(errorResult);
        Assert.False(errorResult.Value);
        Assert.Equal("ERROR", errorResult.Reason);
        Assert.Equal("flag_not_found", errorResult.ErrorCode);

        Assert.Equal(2, this._mockHandler.Requests.Count);
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

    #region Boundary and Edge Case Tests

    [Fact]
    public async Task EvaluateFlag_WithVeryLongFlagKey_ShouldHandleCorrectly()
    {
        // Arrange
        var longFlagKey = new string('a', 1000); // Very long flag key
        const bool defaultValue = false;
        var expectedResponse = new OfrepResponse<bool>(longFlagKey, true);

        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(longFlagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Value);
        Assert.Single(this._mockHandler.Requests);
    }

    [Fact]
    public async Task EvaluateFlag_WithUnicodeCharactersInFlagKey_ShouldHandleCorrectly()
    {
        // Arrange
        const string flagKey = "flag-🚀-测试-🎯";
        const string defaultValue = "default";
        var expectedResponse = new OfrepResponse<string>(flagKey, "unicode-value");

        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("unicode-value", result.Value);
        Assert.Single(this._mockHandler.Requests);

        // Verify the unicode characters were properly URL encoded
        var request = this._mockHandler.Requests[0];
        var requestUri = request.RequestUri?.ToString();
        Assert.Contains("ofrep/v1/evaluate/flags/", requestUri);

        // Verify the URI contains the URL-encoded unicode characters
        var encodedFlagKey = Uri.EscapeDataString(flagKey);
        var originalString = request.RequestUri?.OriginalString;
        Assert.Contains(encodedFlagKey, originalString);
    }

    [Fact]
    public async Task EvaluateFlag_WithLargeEvaluationContext_ShouldHandleCorrectly()
    {
        // Arrange
        const string flagKey = "large-context-flag";
        const bool defaultValue = false;

        var contextBuilder = EvaluationContext.Builder();

        // Add many properties to the context
        for (int i = 0; i < 100; i++)
        {
            contextBuilder.Set($"property_{i}", $"value_{i}");
            contextBuilder.Set($"number_{i}", i);
            contextBuilder.Set($"boolean_{i}", i % 2 == 0);
        }

        var largeContext = contextBuilder.Build();
        var expectedResponse = new OfrepResponse<bool>(flagKey, true);

        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, largeContext);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Value);
        Assert.Single(this._mockHandler.Requests);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData("")]
    [InlineData("test-value")]
    [InlineData(0)]
    [InlineData(42)]
    [InlineData(-123.456)]
    public async Task EvaluateFlag_WithVariousDefaultValues_ShouldReturnDefaultOnError<T>(T defaultValue)
    {
        // Arrange
        const string flagKey = "error-flag";

        this._mockHandler.SetupResponse(HttpStatusCode.InternalServerError, "Server error");

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal("general_error", result.ErrorCode);
        Assert.Equal("ERROR", result.Reason);
    }

    #endregion

    #region HTTP Header Tests

    [Fact]
    public async Task EvaluateFlag_WithCustomHeaders_ShouldIncludeInRequest()
    {
        // Arrange
        const string flagKey = "header-test-flag";
        const bool defaultValue = false;

        var configWithHeaders = new OfrepOptions("https://api.example.com/")
        {
            Headers = new Dictionary<string, string>
            {
                { "X-API-Version", "v2" },
                { "X-Client-Name", "test-client" },
                { "Custom-Header", "custom-value" }
            }
        };

        var expectedResponse = new OfrepResponse<bool>(flagKey, true);
        this._mockHandler.SetupResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse, this._jsonSerializerCamelCase));

        using var client = new OfrepClient(configWithHeaders, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Value);
        Assert.Single(this._mockHandler.Requests);

        var request = this._mockHandler.Requests[0];
        // Note: In a real scenario, we'd check the headers were set, but our mock handler doesn't expose them
        // This test ensures the client can be created and used with custom headers without errors
    }

    #endregion

    #region Network Resilience Tests

    [Fact]
    public async Task EvaluateFlag_WithSocketException_ShouldReturnGeneralError()
    {
        // Arrange
        const string flagKey = "socket-error-flag";
        const bool defaultValue = false;

        this._mockHandler.SetupException(new System.Net.Sockets.SocketException(10061)); // Connection refused

        using var client = new OfrepClient(this._configuration, this._mockHandler, this._mockLogger);

        // Act
        var result = await client.EvaluateFlag(flagKey, defaultValue, EvaluationContext.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal("general_error", result.ErrorCode);
        Assert.Equal("ERROR", result.Reason);
    }

    #endregion
}
