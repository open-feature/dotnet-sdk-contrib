using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using OpenFeature.Contrib.Providers.Ofrep.Client;
using OpenFeature.Contrib.Providers.Ofrep.Configuration;
using OpenFeature.Contrib.Providers.Ofrep.Models;
using OpenFeature.Model;
using Xunit;
using static Moq.It;

namespace OpenFeature.Contrib.Providers.Ofrep.Test;

public class OfrepClientCachingTest : IDisposable
{
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly OfrepConfiguration _defaultConfiguration;
    private OfrepClient _client = null!;

    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public OfrepClientCachingTest()
    {
        _mockHandler = new Mock<HttpMessageHandler>();
        _defaultConfiguration = new OfrepConfiguration("http://localhost:8080")
        {
            CacheDuration = TimeSpan.FromMilliseconds(1000)
        };
    }

    private void SetupClient()
    {
        // Initialize cache in configuration
        _defaultConfiguration.CacheDuration = TimeSpan.FromMilliseconds(1000);
        _defaultConfiguration.MaxCacheSize = 100;

        // Create client with mock handler
        _client = new OfrepClient(_defaultConfiguration, _mockHandler.Object);
    }

    private void SetupMockResponse(HttpStatusCode statusCode, HttpContent? content = null, string? eTag = null)
    {
        var response = new HttpResponseMessage
        {
            StatusCode = statusCode,
            Content = content
        };

        if (eTag != null)
        {
            response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue(eTag);
        }

        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                IsAny<HttpRequestMessage>(),
                IsAny<CancellationToken>()
            )
            .ReturnsAsync(response);
    }

    [Fact]
    public async Task EvaluateFlagShouldUseCacheWhenCalledMultipleTimesWithSameParameters()
    {
        // Arrange
        var expectedResponse = new OfrepResponse<bool>(true);
        var jsonContent = new StringContent(JsonSerializer.Serialize(expectedResponse, _jsonOptions));
        SetupMockResponse(HttpStatusCode.OK, jsonContent, "\"etag123\"");

        SetupClient();

        // Act - Call twice with the same parameters
        var result1 = await _client.EvaluateFlag("flagKey", "boolean", false, null, CancellationToken.None);
        var result2 = await _client.EvaluateFlag("flagKey", "boolean", false, null, CancellationToken.None);

        // Assert
        Assert.True(result1.Value);
        Assert.True(result2.Value);

        // Verify HTTP call happened exactly once (second call should use cache)
        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Exactly(1),
            IsAny<HttpRequestMessage>(),
            IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task EvaluateFlagShouldWorkWithNormalizedCacheKeys()
    {
        // Arrange
        var expectedResponse = new OfrepResponse<bool>(true);
        var jsonContent = new StringContent(JsonSerializer.Serialize(expectedResponse, _jsonOptions));

        // Create a new mock handler for this test
        var mockHandler = new Mock<HttpMessageHandler>();

        // Setup initial response
        var initialResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = jsonContent
        };
        initialResponse.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"etag123\"");

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                IsAny<HttpRequestMessage>(),
                IsAny<CancellationToken>()
            )
            .ReturnsAsync(initialResponse);

        // Create client with our mock handler
        var client = new OfrepClient(_defaultConfiguration, mockHandler.Object);

        // Act - Call with different but equivalent null context
        var result1 = await client.EvaluateFlag("flagKey", "boolean", false, null, CancellationToken.None);
        var evaluationContext = EvaluationContext.Builder().Build();
        var result2 = await client.EvaluateFlag("flagKey", "boolean", false, evaluationContext, CancellationToken.None);

        // Assert
        Assert.True(result1.Value);
        Assert.True(result2.Value);

        // Verify HTTP call happened exactly once (second call should use cache even with empty vs null context)
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Exactly(1),
            IsAny<HttpRequestMessage>(),
            IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task EvaluateFlagShouldFallbackToCacheDataWhenRequestFails()
    {
        // Arrange
        var expectedResponse = new OfrepResponse<bool>(true);
        var jsonContent = new StringContent(JsonSerializer.Serialize(expectedResponse, _jsonOptions));

        // Create a new mock handler for this test
        var mockHandler = new Mock<HttpMessageHandler>();

        // Track whether this is the first call
        bool firstCall = true;

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                IsAny<HttpRequestMessage>(),
                IsAny<CancellationToken>()
            )
            .ReturnsAsync(() =>
            {
                if (firstCall)
                {
                    firstCall = false;
                    var response = new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = jsonContent
                    };
                    response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"etag123\"");
                    return response;
                }

                throw new HttpRequestException("Network error");
            });

        // Create client with our mock handler
        var client = new OfrepClient(_defaultConfiguration, mockHandler.Object);

        // Act - First call to populate cache
        var result1 = await client.EvaluateFlag("flagKey", "boolean", false, null, CancellationToken.None);

        // Second call should fail but use stale cache
        var result2 = await client.EvaluateFlag("flagKey", "boolean", false, null, CancellationToken.None);

        // Assert
        Assert.True(result1.Value);
        Assert.True(result2.Value); // Should return cached value

        // Verify both calls were attempted
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Exactly(2),
            IsAny<HttpRequestMessage>(),
            IsAny<CancellationToken>()
        );
    }

    public void Dispose()
    {
        if (_client != null)
        {
            _client.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}
