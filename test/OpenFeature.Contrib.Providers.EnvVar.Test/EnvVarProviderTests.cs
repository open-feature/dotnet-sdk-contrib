using System;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using OpenFeature.Constant;
using OpenFeature.Error;
using OpenFeature.Model;
using Xunit;

namespace OpenFeature.Contrib.Providers.EnvVar.Test;

public class EnvVarProviderTests
{
    [Theory]
    [AutoData]
    public async Task ResolveBooleanValueAsync_WhenEnvironmentVariablePresent_ShouldReturnValue(string prefix,
        string flagKey)
    {
        var value = true;
        Environment.SetEnvironmentVariable(prefix + flagKey, value.ToString());

        await ExecuteResolveValueTest(prefix, flagKey, false, value, Reason.Static,
            (provider, key, defaultValue) => provider.ResolveBooleanValueAsync(key, defaultValue));
    }

    [Theory]
    [AutoData]
    public async Task ResolveBooleanValueAsync_WhenEnvironmentVariableMissing_ShouldReturnDefaultWithError(string prefix,
        string flagKey, bool defaultValue)
    {
        // No matching environment value set
        await ExecuteResolveValueTest(prefix, flagKey, defaultValue, defaultValue, Reason.Error, ErrorType.FlagNotFound,
            (provider, key, @default) => provider.ResolveBooleanValueAsync(key, @default));
    }

    [Theory]
    [AutoData]
    public async Task ResolveBooleanValueAsync_WhenEnvironmentVariableContainsInvalidValue_ShouldError(
        string prefix, string flagKey, bool defaultValue)
    {
        var value = "xxxx"; // This value cannot be converted to a bool
        Environment.SetEnvironmentVariable(prefix + flagKey, value);

        await ExecuteResolveErrorTest(prefix, flagKey, defaultValue, ErrorType.TypeMismatch,
            (provider, key, @default) => provider.ResolveBooleanValueAsync(key, @default));

    }

    [Theory]
    [AutoData]
    public async Task ResolveStringValueAsync_WhenEnvironmentVariablePresent_ShouldReturnValue(string prefix,
        string flagKey, string value, string defaultValue)
    {
        Environment.SetEnvironmentVariable(prefix + flagKey, value);

        await ExecuteResolveValueTest(prefix, flagKey, defaultValue, value, Reason.Static,
            (provider, key, @default) => provider.ResolveStringValueAsync(key, defaultValue));
    }

    [Theory]
    [AutoData]
    public async Task ResolveStringValueAsync_WhenEnvironmentVariableMissing_ShouldReturnDefaultWithError(string prefix,
        string flagKey, string defaultValue)
    {
        // No matching environment value set
        await ExecuteResolveValueTest(prefix, flagKey, defaultValue, defaultValue, Reason.Error, ErrorType.FlagNotFound,
            (provider, key, @default) => provider.ResolveStringValueAsync(key, @default));
    }

    [Theory]
    [AutoData]
    public async Task ResolveIntegerValueAsync_WhenEnvironmentVariablePresent_ShouldReturnValue(string prefix,
        string flagKey, int value, int defaultValue)
    {
        Environment.SetEnvironmentVariable(prefix + flagKey, value.ToString());

        await ExecuteResolveValueTest(prefix, flagKey, defaultValue, value, Reason.Static,
            (provider, key, @default) => provider.ResolveIntegerValueAsync(key, @defaultValue));
    }

    [Theory]
    [AutoData]
    public async Task ResolveIntegerValueAsync_WhenEnvironmentVariableMissing_ShouldReturnDefaultWithError(string prefix,
        string flagKey, int defaultValue)
    {
        // No matching environment value set
        await ExecuteResolveValueTest(prefix, flagKey, defaultValue, defaultValue, Reason.Error, ErrorType.FlagNotFound,
            (provider, key, @default) => provider.ResolveIntegerValueAsync(key, @default));
    }

    [Theory]
    [AutoData]
    public async Task ResolveIntegerValueAsync_WhenEnvironmentVariableContainsInvalidValue_ShouldError(
        string prefix, string flagKey, int defaultValue)
    {
        var value = "xxxx"; // This value cannot be converted to an int
        Environment.SetEnvironmentVariable(prefix + flagKey, value);

        await ExecuteResolveErrorTest(prefix, flagKey, defaultValue, ErrorType.TypeMismatch,
            (provider, key, @default) => provider.ResolveIntegerValueAsync(key, @default));

    }

    [Theory]
    [AutoData]
    public async Task ResolveDoubleValueAsync_WhenEnvironmentVariablePresent_ShouldReturnValue(string prefix,
        string flagKey, double value, double defaultValue)
    {
        Environment.SetEnvironmentVariable(prefix + flagKey, value.ToString());

        await ExecuteResolveValueTest(prefix, flagKey, defaultValue, value, Reason.Static,
            (provider, key, @default) => provider.ResolveDoubleValueAsync(key, @defaultValue));
    }

    [Theory]
    [AutoData]
    public async Task ResolveDoubleValueAsync_WhenEnvironmentVariableMissing_ShouldReturnDefaultWithError(string prefix,
        string flagKey, double defaultValue)
    {
        // No matching environment value set
        await ExecuteResolveValueTest(prefix, flagKey, defaultValue, defaultValue, Reason.Error, ErrorType.FlagNotFound,
            (provider, key, @default) => provider.ResolveDoubleValueAsync(key, @default));
    }

    [Theory]
    [AutoData]
    public async Task ResolveDoubleValueAsync_WhenEnvironmentVariableContainsInvalidValue_ShouldError(string prefix,
        string flagKey, double defaultValue)
    {
        var value = "xxxx"; // This value cannot be converted to a double
        Environment.SetEnvironmentVariable(prefix + flagKey, value);

        await ExecuteResolveErrorTest(prefix, flagKey, defaultValue, ErrorType.TypeMismatch,
            (provider, key, @default) => provider.ResolveDoubleValueAsync(key, @default));

    }

    [Theory]
    [AutoData]
    public async Task ResolveStructureValueAsync_WhenEnvironmentVariablePresent_ShouldReturnValue(string prefix,
        string flagKey, string value, string defaultValue)
    {
        Environment.SetEnvironmentVariable(prefix + flagKey, value);

        var provider = new EnvVarProvider(prefix);
        var resolutionDetails = await provider.ResolveStructureValueAsync(flagKey, new Value(defaultValue));

        Assert.Equal(value, resolutionDetails.Value.AsString);
        Assert.Equal(Reason.Static, resolutionDetails.Reason);
        Assert.Equal(ErrorType.None, resolutionDetails.ErrorType);
    }

    [Theory]
    [AutoData]
    public async Task ResolveValueFromClient_WhenProviderConfigured_ShouldReturnValue(string prefix, string flagKey)
    {
        Environment.SetEnvironmentVariable(prefix + flagKey, true.ToString());

        var provider = new EnvVarProvider(prefix);
        await OpenFeature.Api.Instance.SetProviderAsync(provider);
        var client = OpenFeature.Api.Instance.GetClient();

        var receivedValue = await client.GetBooleanValueAsync(flagKey, false);

        Assert.True(receivedValue);
    }

    private async Task ExecuteResolveValueTest<T>(string prefix, string flagKey, T defaultValue, T expectedValue,
        string expectedReason, Func<EnvVarProvider, string, T, Task<ResolutionDetails<T>>> resolve)
    {
        await ExecuteResolveValueTest(prefix, flagKey, defaultValue, expectedValue, expectedReason, ErrorType.None, resolve);
    }

    private async Task ExecuteResolveValueTest<T>(string prefix, string flagKey, T defaultValue, T expectedValue,
        string expectedReason, ErrorType expectedErrorType,
        Func<EnvVarProvider, string, T, Task<ResolutionDetails<T>>> resolve)
    {
        var provider = new EnvVarProvider(prefix);
        var resolutionDetails = await resolve(provider, flagKey, defaultValue);

        Assert.Equal(expectedValue, resolutionDetails.Value);
        Assert.Equal(expectedReason, resolutionDetails.Reason);
        Assert.Equal(expectedErrorType, resolutionDetails.ErrorType);
    }

    private async Task ExecuteResolveErrorTest<T>(string prefix, string flagKey, T defaultValue,
        ErrorType expectedErrorType, Func<EnvVarProvider, string, T, Task<ResolutionDetails<T>>> resolve)
    {
        var provider = new EnvVarProvider(prefix);
        var exception =
            await Assert.ThrowsAsync<FeatureProviderException>(() => resolve(provider, flagKey, defaultValue));

        Assert.Equal(expectedErrorType, exception.ErrorType);
    }
}
