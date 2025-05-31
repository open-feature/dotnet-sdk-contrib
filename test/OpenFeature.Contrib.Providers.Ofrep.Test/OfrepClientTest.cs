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
using Xunit;
using static Moq.It;

namespace OpenFeature.Contrib.Providers.Ofrep.Test;

public class OfrepClientTest : IDisposable
{
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly OfrepConfiguration _defaultConfiguration;
    private OfrepClient _client;

    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public OfrepClientTest()
    {
        _mockHandler = new Mock<HttpMessageHandler>();
        _defaultConfiguration = new OfrepConfiguration
        {
            BaseUrl = "http://localhost:8080",
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

    private void SetupMockResponse(HttpStatusCode statusCode, HttpContent content = null, string eTag = null)
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
    public async Task EvaluateFlagShouldCacheResponseWhenFirstRequest()
    {
        // Arrange
        var expectedResponse = new OfrepResponse<bool> { Value = true };
        var jsonContent = new StringContent(JsonSerializer.Serialize(expectedResponse, _jsonOptions));
        SetupMockResponse(HttpStatusCode.OK, jsonContent, "\"etag123\"");

        SetupClient();

        // Act
        var result = await _client.EvaluateFlag("flagKey", "boolean", false, null, CancellationToken.None);

        // Assert
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateFlagShouldReturnDefaultValueWhenNotFoundInCacheAndHttpFails()
    {
        // Arrange
        SetupClient();
        SetupMockException(new HttpRequestException("Network error"));

        // Act
        var result = await _client.EvaluateFlag("flagKey", "boolean", true, null, CancellationToken.None);

        // Assert
        result.Value.Should().BeTrue();
        result.ErrorCode.Should().NotBeNull();
    }

    private void SetupMockException<TException>(TException exception) where TException : Exception
    {
        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                IsAny<HttpRequestMessage>(),
                IsAny<CancellationToken>()
            )
            .ThrowsAsync(exception);
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
