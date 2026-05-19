using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using OpenFeature.Constant;
using OpenFeature.Error;
using OpenFeature.Model;
using Unleash;
using Xunit;

namespace OpenFeature.Contrib.Providers.Unleash.Test;

public class UnleashProviderTest : IAsyncLifetime
{
    private readonly UnleashProvider _provider;

    public UnleashProviderTest()
    {
        var bootstrapPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "bootstrap.json");
        var settings = new UnleashSettings
        {
            AppName = "test-app",
            UnleashApi = new Uri("http://unleash.test/api/"),
            InstanceTag = "test",
            SendMetricsInterval = null
        };
        settings.UseBootstrapFileProvider(bootstrapPath);
        ApplyFileSystemWorkaround(settings);

        this._provider = new UnleashProvider(settings);
    }

    /// <summary>
    /// Workaround for https://github.com/Unleash/unleash-dotnet-sdk/issues/141
    /// The internal FileSystem property is not initialized before ToggleBootstrapFileProvider.Read() is called.
    /// Fix: https://github.com/Unleash/unleash-dotnet-sdk/pull/347
    /// </summary>
    internal static void ApplyFileSystemWorkaround(UnleashSettings settings)
    {
        var fileSystemProp = typeof(UnleashSettings).GetProperty("FileSystem", BindingFlags.NonPublic | BindingFlags.Instance);
        var fileSystemType = typeof(UnleashSettings).Assembly.GetType("Unleash.Internal.FileSystem");
        var fileSystem = Activator.CreateInstance(fileSystemType, System.Text.Encoding.UTF8);
        fileSystemProp.SetValue(settings, fileSystem);
    }

    public Task InitializeAsync() => this._provider.InitializeAsync(EvaluationContext.Empty);
    public Task DisposeAsync() => this._provider.ShutdownAsync();

    [Fact]
    public void GetMetadata_ReturnsCorrectName()
    {
        Assert.Equal("Unleash Provider", this._provider.GetMetadata().Name);
    }

    // Boolean evaluation tests

    [Fact]
    public async Task ResolveBooleanValue_WhenEnabled_ReturnsTrue()
    {
        var result = await this._provider.ResolveBooleanValueAsync("boolean-flag", false);

        Assert.True(result.Value);
        Assert.Equal("boolean-flag", result.FlagKey);
    }

    [Fact]
    public async Task ResolveBooleanValue_WhenDisabled_ReturnsFalse()
    {
        var result = await this._provider.ResolveBooleanValueAsync("disabled-flag", false);

        Assert.False(result.Value);
    }

    [Fact]
    public async Task ResolveBooleanValue_UnknownFlag_ReturnsDefault()
    {
        var result = await this._provider.ResolveBooleanValueAsync("nonexistent-flag", true);

        Assert.True(result.Value);
    }

    // String evaluation tests

    [Fact]
    public async Task ResolveStringValue_WithVariant_ReturnsPayload()
    {
        var result = await this._provider.ResolveStringValueAsync("string-flag", "default");

        Assert.Equal("hello", result.Value);
        Assert.Equal("variantA", result.Variant);
    }

    [Fact]
    public async Task ResolveStringValue_WhenDisabled_ReturnsDefault()
    {
        var result = await this._provider.ResolveStringValueAsync("disabled-flag", "default");

        Assert.Equal("default", result.Value);
        Assert.Equal(Reason.Default, result.Reason);
    }

    // Integer evaluation tests

    [Fact]
    public async Task ResolveIntegerValue_WithVariant_ReturnsParsedInt()
    {
        var result = await this._provider.ResolveIntegerValueAsync("integer-flag", 0);

        Assert.Equal(42, result.Value);
        Assert.Equal("variantInt", result.Variant);
    }

    [Fact]
    public async Task ResolveIntegerValue_WhenDisabled_ReturnsDefault()
    {
        var result = await this._provider.ResolveIntegerValueAsync("disabled-flag", 99);

        Assert.Equal(99, result.Value);
        Assert.Equal(Reason.Default, result.Reason);
    }

    [Fact]
    public async Task ResolveIntegerValue_InvalidPayload_ThrowsTypeMismatch()
    {
        await Assert.ThrowsAsync<TypeMismatchException>(
            () => this._provider.ResolveIntegerValueAsync("invalid-int-flag", 0));
    }

    // Double evaluation tests

    [Fact]
    public async Task ResolveDoubleValue_WithVariant_ReturnsParsedDouble()
    {
        var result = await this._provider.ResolveDoubleValueAsync("double-flag", 0.0);

        Assert.Equal(3.14, result.Value);
        Assert.Equal("variantDouble", result.Variant);
    }

    [Fact]
    public async Task ResolveDoubleValue_WhenDisabled_ReturnsDefault()
    {
        var result = await this._provider.ResolveDoubleValueAsync("disabled-flag", 1.5);

        Assert.Equal(1.5, result.Value);
        Assert.Equal(Reason.Default, result.Reason);
    }

    [Fact]
    public async Task ResolveDoubleValue_InvalidPayload_ThrowsTypeMismatch()
    {
        await Assert.ThrowsAsync<TypeMismatchException>(
            () => this._provider.ResolveDoubleValueAsync("invalid-int-flag", 0.0));
    }

    // Structure evaluation tests

    [Fact]
    public async Task ResolveStructureValue_WithVariant_ReturnsPayloadAsValue()
    {
        var result = await this._provider.ResolveStructureValueAsync("json-flag", new Value("default"));

        Assert.Equal("{\"key\":\"value\"}", result.Value.AsString);
        Assert.Equal("variantJson", result.Variant);
    }

    [Fact]
    public async Task ResolveStructureValue_WhenDisabled_ReturnsDefault()
    {
        var defaultValue = new Value("default");
        var result = await this._provider.ResolveStructureValueAsync("disabled-flag", defaultValue);

        Assert.Equal(defaultValue, result.Value);
        Assert.Equal(Reason.Default, result.Reason);
    }

    // Null payload edge cases

    [Fact]
    public async Task ResolveStringValue_WithNullPayload_ReturnsDefault()
    {
        var result = await this._provider.ResolveStringValueAsync("no-payload-flag", "fallback");

        Assert.Equal("fallback", result.Value);
        Assert.Equal("variantNoPayload", result.Variant);
    }

    [Fact]
    public async Task ResolveIntegerValue_WithNullPayload_ThrowsTypeMismatch()
    {
        await Assert.ThrowsAsync<TypeMismatchException>(
            () => this._provider.ResolveIntegerValueAsync("no-payload-flag", 0));
    }

    [Fact]
    public async Task ResolveDoubleValue_WithNullPayload_ThrowsTypeMismatch()
    {
        await Assert.ThrowsAsync<TypeMismatchException>(
            () => this._provider.ResolveDoubleValueAsync("no-payload-flag", 0.0));
    }

    [Fact]
    public async Task ResolveStructureValue_WithNullPayload_ReturnsDefault()
    {
        var defaultValue = new Value("default");
        var result = await this._provider.ResolveStructureValueAsync("no-payload-flag", defaultValue);

        Assert.Equal(defaultValue, result.Value);
        Assert.Equal("variantNoPayload", result.Variant);
    }

    // Context transformation tests

    [Fact]
    public async Task ResolveBooleanValue_WithTargetingKey_MatchesUserStrategy()
    {
        var context = EvaluationContext.Builder()
            .SetTargetingKey("user-123")
            .Build();

        var result = await this._provider.ResolveBooleanValueAsync("user-targeted-flag", false, context);

        Assert.True(result.Value);
    }

    [Fact]
    public async Task ResolveBooleanValue_WithWrongUser_ReturnsFalse()
    {
        var context = EvaluationContext.Builder()
            .SetTargetingKey("wrong-user")
            .Build();

        var result = await this._provider.ResolveBooleanValueAsync("user-targeted-flag", false, context);

        Assert.False(result.Value);
    }

    [Fact]
    public async Task ResolveBooleanValue_WithNullContext_DoesNotThrow()
    {
        var result = await this._provider.ResolveBooleanValueAsync("boolean-flag", false, null);

        Assert.True(result.Value);
    }
}
