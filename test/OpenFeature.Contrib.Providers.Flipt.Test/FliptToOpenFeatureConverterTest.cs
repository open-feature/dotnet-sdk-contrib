using System.Net;
using Flipt.Rest;
using FluentAssertions;
using Moq;
using OpenFeature.Constant;
using OpenFeature.Contrib.Providers.Flipt.ClientWrapper;
using OpenFeature.Model;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flipt.Test;

public class FliptToOpenFeatureConverterTest
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
            .ThrowsAsync(new FliptRestException("", (int)thrownStatusCode, "", null, null));

        var fliptToOpenFeature = new FliptToOpenFeatureConverter(mockFliptClientWrapper.Object);
        var resolution = await fliptToOpenFeature.EvaluateBooleanAsync("flagKey", fallbackValue);
        resolution.Value.Should().Be(fallbackValue);
        resolution.ErrorType.Should().Be(expectedOpenFeatureErrorType);
        resolution.Reason.Should().Be(Reason.Error);
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
        resolution.Reason.Should().Be(Reason.TargetingMatch);
    }

    [Theory]
    [InlineData("show-feature", false)]
    [InlineData("show-feature", true)]
    public async Task EvaluateBooleanAsync_GivenNonExistentFlag_ShouldReturnDefaultValueWithFlagNotFoundError(
        string flagKey, bool fallBackValue)
    {
        var mockFliptClientWrapper = new Mock<IFliptClientWrapper>();
        mockFliptClientWrapper.Setup(fcw => fcw.EvaluateBooleanAsync(It.IsAny<EvaluationRequest>()))
            .ThrowsAsync(new FliptRestException("", (int)HttpStatusCode.NotFound, "", null, null));

        var fliptToOpenFeature = new FliptToOpenFeatureConverter(mockFliptClientWrapper.Object);
        var resolution = await fliptToOpenFeature.EvaluateBooleanAsync("show-feature", fallBackValue);

        resolution.FlagKey.Should().Be(flagKey);
        resolution.Value.Should().Be(fallBackValue);
        resolution.ErrorType.Should().Be(ErrorType.FlagNotFound);
        resolution.Reason.Should().Be(Reason.Error);
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
            .ThrowsAsync(new FliptRestException("", (int)thrownStatusCode, "", null, null));

        var fliptToOpenFeature = new FliptToOpenFeatureConverter(mockFliptClientWrapper.Object);
        var resolution = await fliptToOpenFeature.EvaluateAsync("flagKey", fallbackValue);
        resolution.Value.Should().Be(fallbackValue);
        resolution.ErrorType.Should().Be(expectedOpenFeatureErrorType);
        resolution.Reason.Should().Be(Reason.Error);
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
                Reason = EvaluationReason.MATCH_EVALUATION_REASON
            });

        var fliptToOpenFeature = new FliptToOpenFeatureConverter(mockFliptClientWrapper.Object);
        var resolution = await fliptToOpenFeature.EvaluateAsync(flagKey, valueFromSrc);

        resolution.FlagKey.Should().Be(flagKey);
        resolution.Variant.Should().Be(valueFromSrc.ToString() ?? string.Empty);
        resolution.Value.Should().BeEquivalentTo(expectedValue?.ToString());
        resolution.Reason.Should().Be(Reason.TargetingMatch);
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
                Reason = EvaluationReason.MATCH_EVALUATION_REASON
            });

        var fliptToOpenFeature = new FliptToOpenFeatureConverter(mockFliptClientWrapper.Object);
        var resolution = await fliptToOpenFeature.EvaluateAsync(flagKey, new Value());

        resolution.FlagKey.Should().Be(flagKey);
        resolution.Variant.Should().Be(variantKey);
        resolution.Value.Should().BeEquivalentTo(expectedValue);
    }


    [Fact]
    public async Task
        EvaluateVariantAsync_GivenNonExistentFlagWithNonNestedFallback_ShouldReturnDefaultValueWithFlagNotFoundError()
    {
        var fallbackValue = new Value(new Structure(new Dictionary<string, Value>
        {
            { "name", new Value("Mr. Robinson") }, { "age", new Value(12) }
        }));
        var mockFliptClientWrapper = new Mock<IFliptClientWrapper>();
        mockFliptClientWrapper.Setup(fcw => fcw.EvaluateVariantAsync(It.IsAny<EvaluationRequest>()))
            .ThrowsAsync(new FliptRestException("", (int)HttpStatusCode.NotFound, "", null, null));

        var fliptToOpenFeature = new FliptToOpenFeatureConverter(mockFliptClientWrapper.Object);
        var resolution = await fliptToOpenFeature.EvaluateAsync("non-existent-flag", fallbackValue);

        resolution.FlagKey.Should().Be("non-existent-flag");
        resolution.Variant.Should().BeNull();
        resolution.Value.Should().BeEquivalentTo(fallbackValue);
        resolution.ErrorType.Should().Be(ErrorType.FlagNotFound);
    }


    [Fact]
    public async Task
        EvaluateVariantAsync_GivenNonExistentFlagWithNestedFallback_ShouldReturnDefaultValueWithFlagNotFoundError()
    {
        var fallbackValue = new Value("");
        var mockFliptClientWrapper = new Mock<IFliptClientWrapper>();
        mockFliptClientWrapper.Setup(fcw => fcw.EvaluateVariantAsync(It.IsAny<EvaluationRequest>()))
            .ThrowsAsync(new FliptRestException("", (int)HttpStatusCode.NotFound, "", null, null));

        var fliptToOpenFeature = new FliptToOpenFeatureConverter(mockFliptClientWrapper.Object);
        var resolution = await fliptToOpenFeature.EvaluateAsync("non-existent-flag", fallbackValue);

        resolution.FlagKey.Should().Be("non-existent-flag");
        resolution.Variant.Should().BeNull();
        resolution.Value.Should().BeEquivalentTo(fallbackValue);
        resolution.ErrorType.Should().Be(ErrorType.FlagNotFound);
    }
}