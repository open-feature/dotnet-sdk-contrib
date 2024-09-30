using System.Net;
using Flipt.Clients;
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
    [InlineData(HttpStatusCode.NotFound, ErrorType.FlagNotFound)]
    [InlineData(HttpStatusCode.BadRequest, ErrorType.TypeMismatch)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorType.ProviderNotReady)]
    [InlineData(HttpStatusCode.Forbidden, ErrorType.ProviderNotReady)]
    public async Task ResolveBooleanValueAsync_GivenWrongURl_ShouldHandleHttpRequestException(
        HttpStatusCode thrownStatusCode, ErrorType expectedOpenFeatureErrorType)
    {
        var evaluationClient = new Mock<Evaluation>();
        evaluationClient.Setup(ev =>
                ev.EvaluateBooleanAsync(new EvaluationRequest("", "", Guid.NewGuid().ToString(),
                    new Dictionary<string, string>())))
            .ThrowsAsync(new HttpRequestException("", null, thrownStatusCode));


        var fliptProvider = new FliptProvider(new FliptToOpenFeatureConverter(evaluationClient.Object));
        var resolution = await fliptProvider.ResolveBooleanValueAsync("flagKey", false);
        resolution.Should().Be(false);
        resolution.ErrorType.Should().Be(expectedOpenFeatureErrorType);
    }
}