using System.Net;
using Flipt.DTOs;
using FluentAssertions;
using Moq;
using OpenFeature.Constant;

namespace OpenFeature.Contrib.Providers.Flipt.Test;

public class FliptProviderTest
{
    private readonly string _fliptUrl = "http://localhost:8080/";

    [Fact]
    public void CreateFliptProvider_ShouldReturnFliptProvider()
    {
        // Flipt library always returns a flipt instance
        var fliptProvider = new FliptProvider(_fliptUrl);
        Assert.NotNull(fliptProvider);
    }

    [Fact]
    public void CreateFliptProvider_GivenEmptyUrl_ShouldThrowInvalidOperationException()
    {
        var act = void () => new FliptProvider("");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("BaseURL must be provided.");
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, ErrorType.FlagNotFound, false)]
    [InlineData(HttpStatusCode.BadRequest, ErrorType.TypeMismatch, false)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorType.ProviderNotReady, false)]
    [InlineData(HttpStatusCode.Forbidden, ErrorType.ProviderNotReady, false)]
    public async Task ResolveBooleanValueAsync_GivenWrongURl_ShouldHandleHttpRequestException(
        HttpStatusCode thrownStatusCode, ErrorType expectedOpenFeatureErrorType, bool fallbackValue)
    {
        var mockFliptClientWrapper = new Mock<IFliptClientWrapper>();
        mockFliptClientWrapper.Setup(ev =>
                ev.EvaluateBooleanAsync(It.IsAny<EvaluationRequest>()))
            .ThrowsAsync(new HttpRequestException("", null, thrownStatusCode));


        var fliptProvider = new FliptProvider(new FliptToOpenFeatureConverter(mockFliptClientWrapper.Object));
        var resolution = await fliptProvider.ResolveBooleanValueAsync("flagKey", fallbackValue);
        resolution.Value.Should().Be(fallbackValue);
        resolution.ErrorType.Should().Be(expectedOpenFeatureErrorType);
    }
}