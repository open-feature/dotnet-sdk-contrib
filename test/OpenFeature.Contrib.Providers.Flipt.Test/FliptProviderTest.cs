using Flipt.Rest;
using FluentAssertions;
using Moq;
using OpenFeature.Constant;
using OpenFeature.Contrib.Providers.Flipt.ClientWrapper;
using OpenFeature.Model;
using Xunit;

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
        var act = void() => new FliptProvider("");
        act.Should().Throw<UriFormatException>();
    }


    [Fact]
    public async Task
        ResolveNonBooleansAsync_GivenFlagThatHasATypeMismatch_ShouldReturnDefaultValueWithTypeMismatchError()
    {
        var mockFliptClientWrapper = new Mock<IFliptClientWrapper>();
        const string flagKey = "iamnotadouble";
        mockFliptClientWrapper.Setup(fcw => fcw.EvaluateVariantAsync(It.IsAny<EvaluationRequest>()))
            .ReturnsAsync(new VariantEvaluationResponse
            {
                FlagKey = flagKey,
                VariantKey = "variant-key",
                RequestId = Guid.NewGuid()
                    .ToString(),
                SegmentKeys = ["segment1"],
                VariantAttachment = "",
                Match = true,
                Reason = VariantEvaluationResponseReason.MATCH_EVALUATION_REASON
            });

        var provider = new FliptProvider(new FliptToOpenFeatureConverter(mockFliptClientWrapper.Object));

        var doubleResolution = await provider.ResolveDoubleValueAsync(flagKey, 0.0);
        doubleResolution.FlagKey.Should().Be(flagKey);
        doubleResolution.Value.Should().Be(0.0);
        doubleResolution.ErrorType.Should().Be(ErrorType.TypeMismatch);

        var integerResolution = await provider.ResolveIntegerValueAsync(flagKey, 0);
        integerResolution.FlagKey.Should().Be(flagKey);
        integerResolution.Value.Should().Be(0);
        integerResolution.ErrorType.Should().Be(ErrorType.TypeMismatch);

        var valueResolution = await provider.ResolveStructureValueAsync(flagKey, new Value());
        valueResolution.FlagKey.Should().Be(flagKey);
        valueResolution.Value.Should().BeEquivalentTo(new Value());
        valueResolution.ErrorType.Should().Be(ErrorType.TypeMismatch);
    }

    [Fact]
    public async Task ResolveStringValueAsync_WhenCalled_ShouldCallCorrectMethodFromFliptClientWrapper()
    {
        const string flagKey = "feature-flag-key";
        var (provider, mockFliptClientWrapper) = GenerateFliptProviderWithMockedDependencies(flagKey);
        await provider.ResolveStringValueAsync(flagKey, "");
        mockFliptClientWrapper.Verify(
            fcw => fcw.EvaluateVariantAsync(It.Is<EvaluationRequest>(er => er.FlagKey == flagKey)), Times.Once);
    }

    [Fact]
    public async Task ResolveDoubleValueAsync_WhenCalled_ShouldCallCorrectMethodFromFliptClientWrapper()
    {
        const string flagKey = "feature-flag-key";
        var (provider, mockFliptClientWrapper) = GenerateFliptProviderWithMockedDependencies(flagKey);
        await provider.ResolveDoubleValueAsync(flagKey, 0.0);
        mockFliptClientWrapper.Verify(
            fcw => fcw.EvaluateVariantAsync(It.Is<EvaluationRequest>(er => er.FlagKey == flagKey)), Times.Once);
    }

    [Fact]
    public async Task ResolveIntegerValueAsync_WhenCalled_ShouldCallCorrectMethodFromFliptClientWrapper()
    {
        const string flagKey = "feature-flag-key";
        var (provider, mockFliptClientWrapper) = GenerateFliptProviderWithMockedDependencies(flagKey);
        await provider.ResolveIntegerValueAsync(flagKey, 0);
        mockFliptClientWrapper.Verify(
            fcw => fcw.EvaluateVariantAsync(It.Is<EvaluationRequest>(er => er.FlagKey == flagKey)), Times.Once);
    }

    [Fact]
    public async Task ResolveStructureValueAsync_WhenCalled_ShouldCallCorrectMethodFromFliptClientWrapper()
    {
        const string flagKey = "feature-flag-key";
        var (provider, mockFliptClientWrapper) = GenerateFliptProviderWithMockedDependencies(flagKey);
        await provider.ResolveStructureValueAsync(flagKey, new Value());
        mockFliptClientWrapper.Verify(
            fcw => fcw.EvaluateVariantAsync(It.Is<EvaluationRequest>(er => er.FlagKey == flagKey)), Times.Once);
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_WhenCalled_ShouldCallCorrectMethodFromFliptClientWrapper()
    {
        const string flagKey = "feature-flag-key";
        var (provider, mockFliptClientWrapper) = GenerateFliptProviderWithMockedDependencies(flagKey);
        await provider.ResolveBooleanValueAsync(flagKey, false);
        mockFliptClientWrapper.Verify(
            fcw => fcw.EvaluateBooleanAsync(It.Is<EvaluationRequest>(er => er.FlagKey == flagKey)), Times.Once);
    }

    private (FliptProvider, Mock<IFliptClientWrapper>) GenerateFliptProviderWithMockedDependencies(string flagKey)
    {
        var mockFliptClientWrapper = new Mock<IFliptClientWrapper>();
        mockFliptClientWrapper.Setup(fcw => fcw.EvaluateVariantAsync(It.IsAny<EvaluationRequest>()))
            .ReturnsAsync(new VariantEvaluationResponse
            {
                FlagKey = flagKey,
                VariantKey = "variant-key",
                RequestId = Guid.NewGuid()
                    .ToString(),
                SegmentKeys = ["segment1"],
                VariantAttachment = "",
                Match = true,
                Reason = VariantEvaluationResponseReason.MATCH_EVALUATION_REASON
            });

        mockFliptClientWrapper.Setup(fcw => fcw.EvaluateBooleanAsync(It.IsAny<EvaluationRequest>()))
            .ReturnsAsync(new BooleanEvaluationResponse
            {
                FlagKey = flagKey,
                RequestId = Guid.NewGuid()
                    .ToString(),
                Enabled = true,
                Reason = BooleanEvaluationResponseReason.MATCH_EVALUATION_REASON
            });

        return (new FliptProvider(new FliptToOpenFeatureConverter(mockFliptClientWrapper.Object)),
            mockFliptClientWrapper);
    }
}