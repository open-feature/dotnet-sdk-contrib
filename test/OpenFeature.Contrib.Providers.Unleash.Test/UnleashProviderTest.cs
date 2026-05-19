using System;
using System.Threading.Tasks;
using NSubstitute;
using OpenFeature.Constant;
using OpenFeature.Error;
using OpenFeature.Model;
using Unleash;
using Unleash.Internal;
using Xunit;

using Payload = Yggdrasil.Payload;

namespace OpenFeature.Contrib.Providers.Unleash.Test;

public class UnleashProviderTest
{
    private readonly IUnleash _mockUnleash;
    private readonly UnleashProvider _provider;

    public UnleashProviderTest()
    {
        this._mockUnleash = Substitute.For<IUnleash>();
        this._provider = new UnleashProvider(this._mockUnleash);
    }

    [Fact]
    public void GetMetadata_ReturnsCorrectName()
    {
        var metadata = this._provider.GetMetadata();
        Assert.Equal("Unleash Provider", metadata.Name);
    }

    // Boolean evaluation tests

    [Fact]
    public async Task ResolveBooleanValue_WhenEnabled_ReturnsTrue()
    {
        this._mockUnleash.IsEnabled("feature-1", Arg.Any<UnleashContext>(), Arg.Any<bool>()).Returns(true);

        var result = await this._provider.ResolveBooleanValueAsync("feature-1", false);

        Assert.True(result.Value);
        Assert.Equal("feature-1", result.FlagKey);
    }

    [Fact]
    public async Task ResolveBooleanValue_WhenDisabled_ReturnsFalse()
    {
        this._mockUnleash.IsEnabled("feature-1", Arg.Any<UnleashContext>(), Arg.Any<bool>()).Returns(false);

        var result = await this._provider.ResolveBooleanValueAsync("feature-1", false);

        Assert.False(result.Value);
    }

    [Fact]
    public async Task ResolveBooleanValue_DefaultValuePassedThrough()
    {
        this._mockUnleash.IsEnabled("unknown-feature", Arg.Any<UnleashContext>(), true).Returns(true);

        var result = await this._provider.ResolveBooleanValueAsync("unknown-feature", true);

        Assert.True(result.Value);
    }

    // String evaluation tests

    [Fact]
    public async Task ResolveStringValue_WithVariant_ReturnsPayload()
    {
        var variant = new Variant("variantA", new Payload("string", "hello"), true, true);
        this._mockUnleash.GetVariant("feature-1", Arg.Any<UnleashContext>()).Returns(variant);

        var result = await this._provider.ResolveStringValueAsync("feature-1", "default");

        Assert.Equal("hello", result.Value);
        Assert.Equal("variantA", result.Variant);
    }

    [Fact]
    public async Task ResolveStringValue_WhenDisabled_ReturnsDefault()
    {
        this._mockUnleash.GetVariant("feature-1", Arg.Any<UnleashContext>()).Returns(Variant.DISABLED_VARIANT);

        var result = await this._provider.ResolveStringValueAsync("feature-1", "default");

        Assert.Equal("default", result.Value);
    }

    // Integer evaluation tests

    [Fact]
    public async Task ResolveIntegerValue_WithVariant_ReturnsParsedInt()
    {
        var variant = new Variant("variantA", new Payload("number", "42"), true, true);
        this._mockUnleash.GetVariant("feature-1", Arg.Any<UnleashContext>()).Returns(variant);

        var result = await this._provider.ResolveIntegerValueAsync("feature-1", 0);

        Assert.Equal(42, result.Value);
        Assert.Equal("variantA", result.Variant);
    }

    [Fact]
    public async Task ResolveIntegerValue_WhenDisabled_ReturnsDefault()
    {
        this._mockUnleash.GetVariant("feature-1", Arg.Any<UnleashContext>()).Returns(Variant.DISABLED_VARIANT);

        var result = await this._provider.ResolveIntegerValueAsync("feature-1", 99);

        Assert.Equal(99, result.Value);
    }

    [Fact]
    public async Task ResolveIntegerValue_InvalidPayload_ThrowsTypeMismatch()
    {
        var variant = new Variant("variantA", new Payload("string", "not-a-number"), true, true);
        this._mockUnleash.GetVariant("feature-1", Arg.Any<UnleashContext>()).Returns(variant);

        await Assert.ThrowsAsync<TypeMismatchException>(
            () => this._provider.ResolveIntegerValueAsync("feature-1", 0));
    }

    // Double evaluation tests

    [Fact]
    public async Task ResolveDoubleValue_WithVariant_ReturnsParsedDouble()
    {
        var variant = new Variant("variantA", new Payload("number", "3.14"), true, true);
        this._mockUnleash.GetVariant("feature-1", Arg.Any<UnleashContext>()).Returns(variant);

        var result = await this._provider.ResolveDoubleValueAsync("feature-1", 0.0);

        Assert.Equal(3.14, result.Value);
        Assert.Equal("variantA", result.Variant);
    }

    [Fact]
    public async Task ResolveDoubleValue_WhenDisabled_ReturnsDefault()
    {
        this._mockUnleash.GetVariant("feature-1", Arg.Any<UnleashContext>()).Returns(Variant.DISABLED_VARIANT);

        var result = await this._provider.ResolveDoubleValueAsync("feature-1", 1.5);

        Assert.Equal(1.5, result.Value);
    }

    [Fact]
    public async Task ResolveDoubleValue_InvalidPayload_ThrowsTypeMismatch()
    {
        var variant = new Variant("variantA", new Payload("string", "not-a-double"), true, true);
        this._mockUnleash.GetVariant("feature-1", Arg.Any<UnleashContext>()).Returns(variant);

        await Assert.ThrowsAsync<TypeMismatchException>(
            () => this._provider.ResolveDoubleValueAsync("feature-1", 0.0));
    }

    // Structure evaluation tests

    [Fact]
    public async Task ResolveStructureValue_WithVariant_ReturnsPayloadAsValue()
    {
        var variant = new Variant("variantA", new Payload("json", "{\"key\":\"value\"}"), true, true);
        this._mockUnleash.GetVariant("feature-1", Arg.Any<UnleashContext>()).Returns(variant);

        var result = await this._provider.ResolveStructureValueAsync("feature-1", new Value("default"));

        Assert.Equal("{\"key\":\"value\"}", result.Value.AsString);
        Assert.Equal("variantA", result.Variant);
    }

    [Fact]
    public async Task ResolveStructureValue_WhenDisabled_ReturnsDefault()
    {
        this._mockUnleash.GetVariant("feature-1", Arg.Any<UnleashContext>()).Returns(Variant.DISABLED_VARIANT);

        var defaultValue = new Value("default");
        var result = await this._provider.ResolveStructureValueAsync("feature-1", defaultValue);

        Assert.Equal(defaultValue, result.Value);
    }

    // Context transformation tests

    [Fact]
    public async Task ResolveBooleanValue_WithContext_PassesTransformedContext()
    {
        this._mockUnleash.IsEnabled("feature-1", Arg.Is<UnleashContext>(ctx =>
            ctx.UserId == "user-123" &&
            ctx.SessionId == "session-456" &&
            ctx.RemoteAddress == "192.168.1.1" &&
            ctx.Environment == "production" &&
            ctx.AppName == "my-app"
        ), Arg.Any<bool>()).Returns(true);

        var context = EvaluationContext.Builder()
            .SetTargetingKey("user-123")
            .Set("sessionId", "session-456")
            .Set("remoteAddress", "192.168.1.1")
            .Set("environment", "production")
            .Set("appName", "my-app")
            .Build();

        var result = await this._provider.ResolveBooleanValueAsync("feature-1", false, context);

        Assert.True(result.Value);
    }

    [Fact]
    public async Task ResolveBooleanValue_WithCustomProperties_PassesProperties()
    {
        this._mockUnleash.IsEnabled("feature-1", Arg.Is<UnleashContext>(ctx =>
            ctx.Properties["customProp"] == "customValue"
        ), Arg.Any<bool>()).Returns(true);

        var context = EvaluationContext.Builder()
            .Set("customProp", "customValue")
            .Build();

        var result = await this._provider.ResolveBooleanValueAsync("feature-1", false, context);

        Assert.True(result.Value);
    }

    [Fact]
    public async Task ResolveBooleanValue_WithNullContext_DoesNotThrow()
    {
        this._mockUnleash.IsEnabled("feature-1", Arg.Any<UnleashContext>(), false).Returns(false);

        var result = await this._provider.ResolveBooleanValueAsync("feature-1", false, null);

        Assert.False(result.Value);
    }

    // Shutdown test

    [Fact]
    public void Shutdown_WhenNotOwned_DoesNotDispose()
    {
        this._provider.Shutdown();

        this._mockUnleash.DidNotReceive().Dispose();
    }

    // Constructor tests

    [Fact]
    public void Constructor_WithNullUnleash_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new UnleashProvider((IUnleash)null));
    }

    [Fact]
    public void Constructor_WithNullSettings_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new UnleashProvider((UnleashSettings)null));
    }

    [Fact]
    public void Constructor_WithSettings_CreatesProviderAndShutdownDisposes()
    {
        var settings = new UnleashSettings
        {
            AppName = "test-app",
            UnleashApi = new Uri("http://unleash.test/api/"),
            InstanceTag = "test"
        };

        var provider = new UnleashProvider(settings);

        Assert.Equal("Unleash Provider", provider.GetMetadata().Name);

        // Get the underlying IUnleash instance via reflection
        var unleashField = typeof(UnleashProvider).GetField("_unleash",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var unleash = (DefaultUnleash)unleashField.GetValue(provider);

        // Get the cancellation token source to verify disposal
        var ctsField = typeof(DefaultUnleash).GetField("cancellationTokenSource",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var cts = (System.Threading.CancellationTokenSource)ctsField.GetValue(unleash);

        Assert.False(cts.IsCancellationRequested);

        provider.Shutdown();

        Assert.True(cts.IsCancellationRequested);
    }

    // Null variant / null payload edge cases

    [Fact]
    public async Task ResolveStringValue_WhenGetVariantReturnsNull_ReturnsDefault()
    {
        this._mockUnleash.GetVariant("feature-1", Arg.Any<UnleashContext>()).Returns((Variant)null);

        var result = await this._provider.ResolveStringValueAsync("feature-1", "fallback");

        Assert.Equal("fallback", result.Value);
        Assert.Equal(Reason.Default, result.Reason);
    }

    [Fact]
    public async Task ResolveStringValue_WithNullPayload_ReturnsDefault()
    {
        var variant = new Variant("variantA", null, true, true);
        this._mockUnleash.GetVariant("feature-1", Arg.Any<UnleashContext>()).Returns(variant);

        var result = await this._provider.ResolveStringValueAsync("feature-1", "fallback");

        Assert.Equal("fallback", result.Value);
        Assert.Equal("variantA", result.Variant);
    }

    [Fact]
    public async Task ResolveIntegerValue_WithNullPayload_ThrowsTypeMismatch()
    {
        var variant = new Variant("variantA", null, true, true);
        this._mockUnleash.GetVariant("feature-1", Arg.Any<UnleashContext>()).Returns(variant);

        await Assert.ThrowsAsync<TypeMismatchException>(
            () => this._provider.ResolveIntegerValueAsync("feature-1", 0));
    }

    [Fact]
    public async Task ResolveDoubleValue_WithNullPayload_ThrowsTypeMismatch()
    {
        var variant = new Variant("variantA", null, true, true);
        this._mockUnleash.GetVariant("feature-1", Arg.Any<UnleashContext>()).Returns(variant);

        await Assert.ThrowsAsync<TypeMismatchException>(
            () => this._provider.ResolveDoubleValueAsync("feature-1", 0.0));
    }

    [Fact]
    public async Task ResolveStructureValue_WithNullPayload_ReturnsDefault()
    {
        var variant = new Variant("variantA", null, true, true);
        this._mockUnleash.GetVariant("feature-1", Arg.Any<UnleashContext>()).Returns(variant);

        var defaultValue = new Value("default");
        var result = await this._provider.ResolveStructureValueAsync("feature-1", defaultValue);

        Assert.Equal(defaultValue, result.Value);
        Assert.Equal("variantA", result.Variant);
    }

    // Reason assertions for disabled variants

    [Fact]
    public async Task ResolveIntegerValue_WhenDisabled_ReturnsDefaultReason()
    {
        this._mockUnleash.GetVariant("feature-1", Arg.Any<UnleashContext>()).Returns(Variant.DISABLED_VARIANT);

        var result = await this._provider.ResolveIntegerValueAsync("feature-1", 99);

        Assert.Equal(99, result.Value);
        Assert.Equal(Reason.Default, result.Reason);
    }

    [Fact]
    public async Task ResolveDoubleValue_WhenDisabled_ReturnsDefaultReason()
    {
        this._mockUnleash.GetVariant("feature-1", Arg.Any<UnleashContext>()).Returns(Variant.DISABLED_VARIANT);

        var result = await this._provider.ResolveDoubleValueAsync("feature-1", 1.5);

        Assert.Equal(1.5, result.Value);
        Assert.Equal(Reason.Default, result.Reason);
    }

    [Fact]
    public async Task ResolveStringValue_WhenDisabled_ReturnsDefaultReason()
    {
        this._mockUnleash.GetVariant("feature-1", Arg.Any<UnleashContext>()).Returns(Variant.DISABLED_VARIANT);

        var result = await this._provider.ResolveStringValueAsync("feature-1", "default");

        Assert.Equal("default", result.Value);
        Assert.Equal(Reason.Default, result.Reason);
    }

    // Context transformation edge cases

    [Fact]
    public async Task Context_WithCurrentTime_ParsesCorrectly()
    {
        var expectedTime = new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);

        this._mockUnleash.IsEnabled("feature-1", Arg.Is<UnleashContext>(ctx =>
            ctx.CurrentTime.HasValue &&
            ctx.CurrentTime.Value == expectedTime
        ), Arg.Any<bool>()).Returns(true);

        var context = EvaluationContext.Builder()
            .Set("currentTime", "2024-06-15T10:30:00+00:00")
            .Build();

        var result = await this._provider.ResolveBooleanValueAsync("feature-1", false, context);

        Assert.True(result.Value);
    }

    [Fact]
    public async Task Context_UserIdExplicit_OverridesTargetingKey()
    {
        this._mockUnleash.IsEnabled("feature-1", Arg.Is<UnleashContext>(ctx =>
            ctx.UserId == "explicit-user"
        ), Arg.Any<bool>()).Returns(true);

        var context = EvaluationContext.Builder()
            .SetTargetingKey("targeting-key-user")
            .Set("userId", "explicit-user")
            .Build();

        var result = await this._provider.ResolveBooleanValueAsync("feature-1", false, context);

        Assert.True(result.Value);
    }

    [Fact]
    public async Task Context_TargetingKeyOnly_MapsToUserId()
    {
        this._mockUnleash.IsEnabled("feature-1", Arg.Is<UnleashContext>(ctx =>
            ctx.UserId == "targeting-key-user"
        ), Arg.Any<bool>()).Returns(true);

        var context = EvaluationContext.Builder()
            .SetTargetingKey("targeting-key-user")
            .Build();

        var result = await this._provider.ResolveBooleanValueAsync("feature-1", false, context);

        Assert.True(result.Value);
    }
}
