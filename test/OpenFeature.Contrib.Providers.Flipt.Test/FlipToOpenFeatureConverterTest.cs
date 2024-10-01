using System.Net;
using Flipt.DTOs;
using FluentAssertions;
using Moq;
using OpenFeature.Constant;

namespace OpenFeature.Contrib.Providers.Flipt.Test;

public class FlipToOpenFeatureConverterTest
{
    [Theory]
    [InlineData(HttpStatusCode.NotFound, ErrorType.FlagNotFound, false)]
    [InlineData(HttpStatusCode.BadRequest, ErrorType.TypeMismatch, false)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorType.ProviderNotReady, false)]
    [InlineData(HttpStatusCode.Forbidden, ErrorType.ProviderNotReady, false)]
    [InlineData(HttpStatusCode.TooManyRequests, ErrorType.General, false)]
    public async Task EvaluateBooleanAsync_GivenHttpRequestException_ShouldHandleHttpRequestException(
        HttpStatusCode thrownStatusCode, ErrorType expectedOpenFeatureErrorType, bool fallbackValue)
    {
        var mockFliptClientWrapper = new Mock<IFliptClientWrapper>();
        mockFliptClientWrapper.Setup(ev =>
                ev.EvaluateBooleanAsync(It.IsAny<EvaluationRequest>()))
            .ThrowsAsync(new HttpRequestException("", null, thrownStatusCode));

        var fliptToOpenFeature = new FliptToOpenFeatureConverter(mockFliptClientWrapper.Object);
        var resolution = await fliptToOpenFeature.EvaluateBooleanAsync("flagKey", fallbackValue);
        resolution.Value.Should().Be(fallbackValue);
        resolution.ErrorType.Should().Be(expectedOpenFeatureErrorType);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, ErrorType.FlagNotFound, 0.0)]
    [InlineData(HttpStatusCode.BadRequest, ErrorType.TypeMismatch, 0.0)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorType.ProviderNotReady, 0.0)]
    [InlineData(HttpStatusCode.Forbidden, ErrorType.ProviderNotReady, 0.0)]
    [InlineData(HttpStatusCode.TooManyRequests, ErrorType.General, 0.0)]
    public async Task EvaluateAsync_GivenHttpRequestException_ShouldHandleHttpRequestException(
        HttpStatusCode thrownStatusCode, ErrorType expectedOpenFeatureErrorType, double fallbackValue)
    {
        var mockFliptClientWrapper = new Mock<IFliptClientWrapper>();
        mockFliptClientWrapper.Setup(ev =>
                ev.EvaluateVariantAsync(It.IsAny<EvaluationRequest>()))
            .ThrowsAsync(new HttpRequestException("", null, thrownStatusCode));

        var fliptToOpenFeature = new FliptToOpenFeatureConverter(mockFliptClientWrapper.Object);
        var resolution = await fliptToOpenFeature.EvaluateAsync("flagKey", fallbackValue);
        resolution.Value.Should().Be(fallbackValue);
        resolution.ErrorType.Should().Be(expectedOpenFeatureErrorType);
    }
}