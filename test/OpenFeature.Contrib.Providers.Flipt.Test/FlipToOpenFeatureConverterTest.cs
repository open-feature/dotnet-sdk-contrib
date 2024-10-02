using System.Net;
using Flipt.DTOs;
using FluentAssertions;
using Moq;
using OpenFeature.Constant;
using OpenFeature.Model;
using Reason = Flipt.Models.Reason;

namespace OpenFeature.Contrib.Providers.Flipt.Test;

public class FlipToOpenFeatureConverterTest
{
    // EvaluateBooleanAsync Tests
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
        mockFliptClientWrapper.Setup(fcw =>
                fcw.EvaluateBooleanAsync(It.IsAny<EvaluationRequest>()))
            .ThrowsAsync(new HttpRequestException("", null, thrownStatusCode));

        var fliptToOpenFeature = new FliptToOpenFeatureConverter(mockFliptClientWrapper.Object);
        var resolution = await fliptToOpenFeature.EvaluateBooleanAsync("flagKey", fallbackValue);
        resolution.Value.Should().Be(fallbackValue);
        resolution.ErrorType.Should().Be(expectedOpenFeatureErrorType);
    }

    [Theory]
    [InlineData("show-feature", true)]
    [InlineData("show-feature", false)]
    public async Task EvaluateBooleanAsync_GivenExistingFlag_ShouldReturnFlagValue(string flagKey,
        bool valueFromSrc)
    {
        var mockFliptClientWrapper = new Mock<IFliptClientWrapper>();
        mockFliptClientWrapper.Setup(fcw => fcw.EvaluateBooleanAsync(It.IsAny<EvaluationRequest>()))
            .ReturnsAsync(new BooleanEvaluationResponse
            {
                Enabled = valueFromSrc,
                FlagKey = flagKey,
                RequestId = Guid.NewGuid().ToString()
            });

        var fliptToOpenFeature = new FliptToOpenFeatureConverter(mockFliptClientWrapper.Object);
        var resolution = await fliptToOpenFeature.EvaluateBooleanAsync("show-feature", false);

        resolution.FlagKey.Should().Be(flagKey);
        resolution.Value.Should().Be(valueFromSrc);
    }

    [Theory]
    [InlineData("show-feature", false)]
    [InlineData("show-feature", true)]
    public async Task EvaluateBooleanAsync_GivenNonExistentFlag_ShouldReturnDefaultValueWithFlagNotFoundError(
        string flagKey, bool fallBackValue)
    {
        var mockFliptClientWrapper = new Mock<IFliptClientWrapper>();
        mockFliptClientWrapper.Setup(fcw => fcw.EvaluateBooleanAsync(It.IsAny<EvaluationRequest>()))
            .ThrowsAsync(new HttpRequestException("", null, HttpStatusCode.NotFound));

        var fliptToOpenFeature = new FliptToOpenFeatureConverter(mockFliptClientWrapper.Object);
        var resolution = await fliptToOpenFeature.EvaluateBooleanAsync("show-feature", fallBackValue);

        resolution.FlagKey.Should().Be(flagKey);
        resolution.Value.Should().Be(fallBackValue);
        resolution.ErrorType.Should().Be(ErrorType.FlagNotFound);
    }

    // EvaluateAsync Tests

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
        mockFliptClientWrapper.Setup(fcw =>
                fcw.EvaluateVariantAsync(It.IsAny<EvaluationRequest>()))
            .ThrowsAsync(new HttpRequestException("", null, thrownStatusCode));

        var fliptToOpenFeature = new FliptToOpenFeatureConverter(mockFliptClientWrapper.Object);
        var resolution = await fliptToOpenFeature.EvaluateAsync("flagKey", fallbackValue);
        resolution.Value.Should().Be(fallbackValue);
        resolution.ErrorType.Should().Be(expectedOpenFeatureErrorType);
    }

    [Theory]
    [InlineData("variant-flag", 1.0, 1.0)]
    [InlineData("variant-flag", "thisisastring", "thisisastring")]
    [InlineData("variant-flag", 1, 1)]
    public async Task EvaluateAsync_GivenExistingVariantFlagWhichIsNotAnObject_ShouldReturnFlagValue(string flagKey,
        object valueFromSrc, object? expectedValue = null, string variantAttachment = "")
    {
        var mockFliptClientWrapper = new Mock<IFliptClientWrapper>();
        mockFliptClientWrapper.Setup(fcw => fcw.EvaluateVariantAsync(It.IsAny<EvaluationRequest>()))
            .ReturnsAsync(new VariantEvaluationResponse
            {
                FlagKey = flagKey,
                VariantKey = valueFromSrc.ToString() ?? string.Empty,
                RequestId = Guid.NewGuid().ToString(),
                SegmentKeys = ["segment1"],
                VariantAttachment = variantAttachment,
                Match = true,
                Reason = Reason.MatchEvaluationReason
            });

        var fliptToOpenFeature = new FliptToOpenFeatureConverter(mockFliptClientWrapper.Object);
        var resolution = await fliptToOpenFeature.EvaluateAsync(flagKey, valueFromSrc);

        resolution.FlagKey.Should().Be(flagKey);
        resolution.Variant.Should().Be(valueFromSrc.ToString() ?? string.Empty);
        resolution.Value.Should().BeEquivalentTo(expectedValue?.ToString());
    }

    [Fact]
    public async Task EvaluateAsync_GivenExistingVariantFlagAndWithAnObject_ShouldReturnFlagValue()
    {
        const string flagKey = "variant-flag";
        const string variantKey = "variant-A";
        const string valueFromSrc = """
                                    {
                                        "name": "Mr. Robinson",
                                        "age": 12,
                                    }
                                    """;
        var expectedValue = new Value(new Structure(new Dictionary<string, Value>
        {
            { "name", new Value("Mr. Robinson") }, { "age", new Value(12) }
        }));

        var mockFliptClientWrapper = new Mock<IFliptClientWrapper>();
        mockFliptClientWrapper.Setup(fcw => fcw.EvaluateVariantAsync(It.IsAny<EvaluationRequest>()))
            .ReturnsAsync(new VariantEvaluationResponse
            {
                FlagKey = flagKey,
                VariantKey = variantKey,
                RequestId = Guid.NewGuid().ToString(),
                SegmentKeys = ["segment1"],
                VariantAttachment = valueFromSrc,
                Match = true,
                Reason = Reason.MatchEvaluationReason
            });

        var fliptToOpenFeature = new FliptToOpenFeatureConverter(mockFliptClientWrapper.Object);
        var resolution = await fliptToOpenFeature.EvaluateAsync(flagKey, new Value());

        resolution.FlagKey.Should().Be(flagKey);
        resolution.Variant.Should().Be(variantKey);
        resolution.Value.Should().BeEquivalentTo(expectedValue);
    }
    /* Todo Andrei: Mga kulang pa na unit test
     - Successful na flag
       - Boolean
       - Variant and other types
     - Wrong flag name
       - Boolean
       - Variant
     - Type mismatch call

     Strategy:
     1. I-mock lang return type ni Flipt
    */
}