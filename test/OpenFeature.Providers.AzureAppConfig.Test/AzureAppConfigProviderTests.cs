using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.AppConfiguration;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OpenFeature.Constant;
using OpenFeature.Model;
using Xunit;

namespace OpenFeature.Providers.AzureAppConfig.Test;

public class AzureAppConfigProviderTests
{
    [Fact]
    public void Constructor_WithConnectionString_ShouldInitializeProvider()
    {
        // Arrange
        var connectionString = "Endpoint=https://test.azconfig.io;Id=test;Secret=test";

        // Act
        var provider = new AzureAppConfigProvider(connectionString);

        // Assert
        Assert.NotNull(provider);
        var metadata = provider.GetMetadata();
        Assert.Equal("Azure App Configuration Provider", metadata.Name);
    }

    [Fact]
    public void Constructor_WithConfigurationClient_ShouldInitializeProvider()
    {
        // Arrange
        var mockClient = Substitute.For<ConfigurationClient>();
        var options = new AzureAppConfigProviderOptions();

        // Act
        var provider = new AzureAppConfigProvider(mockClient, options);

        // Assert
        Assert.NotNull(provider);
        var metadata = provider.GetMetadata();
        Assert.Equal("Azure App Configuration Provider", metadata.Name);
    }

    [Fact]
    public void Constructor_WithNullConnectionString_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AzureAppConfigProvider((string)null!));
        Assert.Throws<ArgumentNullException>(() => new AzureAppConfigProvider(string.Empty));
    }

    [Fact]
    public void Constructor_WithNullConfigurationClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AzureAppConfigProvider((ConfigurationClient)null!));
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_WithValidEnabledFlag_ShouldReturnTrue()
    {
        // Arrange
        var flagKey = "test-flag";
        var defaultValue = false;
        var mockClient = Substitute.For<ConfigurationClient>();
        var provider = new AzureAppConfigProvider(mockClient);

        var featureFlag = new FeatureFlag
        {
            Id = flagKey,
            Enabled = true,
            Variants =
            [
                new FeatureFlagVariant { Name = "True", ConfigurationValue = true }
            ],
            Allocation = new Allocation { DefaultWhenEnabled = "True" }
        };

        var configValue = CreateConfigurationSetting(flagKey, featureFlag);
        mockClient.GetConfigurationSettingAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(configValue, Substitute.For<Response>()));

        // Act
        var result = await provider.ResolveBooleanValueAsync(flagKey, defaultValue);

        // Assert
        Assert.Equal(flagKey, result.FlagKey);
        Assert.True(result.Value);
        Assert.Equal(Reason.Static, result.Reason);
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_WithValidDisabledFlag_ShouldReturnDefaultValue()
    {
        // Arrange
        var flagKey = "test-flag";
        var defaultValue = true;
        var mockClient = Substitute.For<ConfigurationClient>();
        var provider = new AzureAppConfigProvider(mockClient);

        var featureFlag = new FeatureFlag
        {
            Id = flagKey,
            Enabled = false,
            Variants =
            [
                new FeatureFlagVariant { Name = "True", ConfigurationValue = true }
            ],
            Allocation = new Allocation { DefaultWhenEnabled = "True" }
        };

        var configValue = CreateConfigurationSetting(flagKey, featureFlag);
        mockClient.GetConfigurationSettingAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(configValue, Substitute.For<Response>()));

        // Act
        var result = await provider.ResolveBooleanValueAsync(flagKey, defaultValue);

        // Assert
        Assert.Equal(flagKey, result.FlagKey);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal(Reason.Disabled, result.Reason);
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_WithFlagNotFound_ShouldReturnFlagNotFoundError()
    {
        // Arrange
        var flagKey = "test-flag";
        var defaultValue = false;
        var mockClient = Substitute.For<ConfigurationClient>();
        var provider = new AzureAppConfigProvider(mockClient);

        var requestFailedException = new RequestFailedException(404, "Not Found");
        mockClient.GetConfigurationSettingAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(requestFailedException);

        // Act
        var result = await provider.ResolveBooleanValueAsync(flagKey, defaultValue);

        // Assert
        Assert.Equal(flagKey, result.FlagKey);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal(ErrorType.FlagNotFound, result.ErrorType);
        Assert.Equal(Reason.Error, result.Reason);
        Assert.Equal("Not Found", result.ErrorMessage);
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_WithNullConfigValue_ShouldReturnFlagNotFoundError()
    {
        // Arrange
        var flagKey = "test-flag";
        var defaultValue = false;
        var mockClient = Substitute.For<ConfigurationClient>();
        var provider = new AzureAppConfigProvider(mockClient);

        mockClient.GetConfigurationSettingAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Response.FromValue<ConfigurationSetting>(null!, Substitute.For<Response>()));

        // Act
        var result = await provider.ResolveBooleanValueAsync(flagKey, defaultValue);

        // Assert
        Assert.Equal(flagKey, result.FlagKey);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal(ErrorType.FlagNotFound, result.ErrorType);
        Assert.Equal(Reason.Error, result.Reason);
        Assert.Equal("Feature flag not found", result.ErrorMessage);
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_WithInvalidJson_ShouldReturnGeneralError()
    {
        // Arrange
        var flagKey = "test-flag";
        var defaultValue = false;
        var mockClient = Substitute.For<ConfigurationClient>();
        var provider = new AzureAppConfigProvider(mockClient);

        var configValue = new ConfigurationSetting(flagKey, "invalid json");
        mockClient.GetConfigurationSettingAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(configValue, Substitute.For<Response>()));

        // Act
        var result = await provider.ResolveBooleanValueAsync(flagKey, defaultValue);

        // Assert
        Assert.Equal(flagKey, result.FlagKey);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal(ErrorType.General, result.ErrorType);
        Assert.Equal(Reason.Error, result.Reason);
        Assert.Equal("Failed to deserialize feature flag", result.ErrorMessage);
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_WithNoVariants_ShouldReturnGeneralError()
    {
        // Arrange
        var flagKey = "test-flag";
        var defaultValue = false;
        var mockClient = Substitute.For<ConfigurationClient>();
        var provider = new AzureAppConfigProvider(mockClient);

        var featureFlag = new FeatureFlag
        {
            Id = flagKey,
            Enabled = true,
            Variants = [], // Empty variants list
            Allocation = new Allocation { DefaultWhenEnabled = "True" }
        };

        var configValue = CreateConfigurationSetting(flagKey, featureFlag);
        mockClient.GetConfigurationSettingAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(configValue, Substitute.For<Response>()));

        // Act
        var result = await provider.ResolveBooleanValueAsync(flagKey, defaultValue);

        // Assert
        Assert.Equal(flagKey, result.FlagKey);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal(ErrorType.General, result.ErrorType);
        Assert.Equal(Reason.Error, result.Reason);
        Assert.Equal("Feature flag has no variants", result.ErrorMessage);
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_WithVariantNotFound_ShouldReturnGeneralError()
    {
        // Arrange
        var flagKey = "test-flag";
        var defaultValue = false;
        var mockClient = Substitute.For<ConfigurationClient>();
        var provider = new AzureAppConfigProvider(mockClient);

        var featureFlag = new FeatureFlag
        {
            Id = flagKey,
            Enabled = true,
            Variants =
            [
                new FeatureFlagVariant { Name = "False", ConfigurationValue = false }
            ],
            Allocation = new Allocation { DefaultWhenEnabled = "True" } // Variant "True" doesn't exist
        };

        var configValue = CreateConfigurationSetting(flagKey, featureFlag);
        mockClient.GetConfigurationSettingAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(configValue, Substitute.For<Response>()));

        // Act
        var result = await provider.ResolveBooleanValueAsync(flagKey, defaultValue);

        // Assert
        Assert.Equal(flagKey, result.FlagKey);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal(ErrorType.General, result.ErrorType);
        Assert.Equal(Reason.Error, result.Reason);
        Assert.Equal("Feature flag has no variants", result.ErrorMessage);
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_WithCustomPrefix_ShouldUseCorrectKey()
    {
        // Arrange
        var flagKey = "test-flag";
        var defaultValue = false;
        var customPrefix = "custom.prefix/";
        var options = new AzureAppConfigProviderOptions { FeatureFlagPrefix = customPrefix };
        var mockClient = Substitute.For<ConfigurationClient>();
        var provider = new AzureAppConfigProvider(mockClient, options);

        var featureFlag = new FeatureFlag
        {
            Id = flagKey,
            Enabled = true,
            Variants =
            [
                new FeatureFlagVariant { Name = "True", ConfigurationValue = true }
            ],
            Allocation = new Allocation { DefaultWhenEnabled = "True" }
        };

        var configValue = CreateConfigurationSetting(flagKey, featureFlag);
        mockClient.GetConfigurationSettingAsync(customPrefix + flagKey, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(configValue, Substitute.For<Response>()));

        // Act
        var result = await provider.ResolveBooleanValueAsync(flagKey, defaultValue);

        // Assert
        Assert.Equal(flagKey, result.FlagKey);
        Assert.True(result.Value);
        await mockClient.Received(1).GetConfigurationSettingAsync(customPrefix + flagKey, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_WithGeneralException_ShouldReturnGeneralError()
    {
        // Arrange
        var flagKey = "test-flag";
        var defaultValue = false;
        var mockClient = Substitute.For<ConfigurationClient>();
        var provider = new AzureAppConfigProvider(mockClient);

        var exception = new InvalidOperationException("General error");
        mockClient.GetConfigurationSettingAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(exception);

        // Act
        var result = await provider.ResolveBooleanValueAsync(flagKey, defaultValue);

        // Assert
        Assert.Equal(flagKey, result.FlagKey);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal(ErrorType.General, result.ErrorType);
        Assert.Equal(Reason.Error, result.Reason);
        Assert.Equal("General error", result.ErrorMessage);
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_WithCaseInsensitiveVariantMatch_ShouldReturnCorrectValue()
    {
        // Arrange
        var flagKey = "test-flag";
        var defaultValue = false;
        var mockClient = Substitute.For<ConfigurationClient>();
        var provider = new AzureAppConfigProvider(mockClient);

        var featureFlag = new FeatureFlag
        {
            Id = flagKey,
            Enabled = true,
            Variants =
            [
                new FeatureFlagVariant { Name = "true", ConfigurationValue = true }, // lowercase
                new FeatureFlagVariant { Name = "false", ConfigurationValue = false }
            ],
            Allocation = new Allocation { DefaultWhenEnabled = "TRUE" } // uppercase
        };

        var configValue = CreateConfigurationSetting(flagKey, featureFlag);
        mockClient.GetConfigurationSettingAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(configValue, Substitute.For<Response>()));

        // Act
        var result = await provider.ResolveBooleanValueAsync(flagKey, defaultValue);

        // Assert
        Assert.Equal(flagKey, result.FlagKey);
        Assert.True(result.Value);
        Assert.Equal(Reason.Static, result.Reason);
    }

    [Fact]
    public async Task ResolveStringValueAsync_ShouldReturnTypeMismatchError()
    {
        // Arrange
        var flagKey = "test-flag";
        var defaultValue = "default";
        var mockClient = Substitute.For<ConfigurationClient>();
        var provider = new AzureAppConfigProvider(mockClient);

        // Act
        var result = await provider.ResolveStringValueAsync(flagKey, defaultValue);

        // Assert
        Assert.Equal(flagKey, result.FlagKey);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal(ErrorType.TypeMismatch, result.ErrorType);
        Assert.Equal(Reason.Error, result.Reason);
        Assert.Equal("String values are not supported. Use boolean feature flags only.", result.ErrorMessage);
    }

    [Fact]
    public async Task ResolveIntegerValueAsync_ShouldReturnTypeMismatchError()
    {
        // Arrange
        var flagKey = "test-flag";
        var defaultValue = 42;
        var mockClient = Substitute.For<ConfigurationClient>();
        var provider = new AzureAppConfigProvider(mockClient);

        // Act
        var result = await provider.ResolveIntegerValueAsync(flagKey, defaultValue);

        // Assert
        Assert.Equal(flagKey, result.FlagKey);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal(ErrorType.TypeMismatch, result.ErrorType);
        Assert.Equal(Reason.Error, result.Reason);
        Assert.Equal("Integer values are not supported. Use boolean feature flags only.", result.ErrorMessage);
    }

    [Fact]
    public async Task ResolveDoubleValueAsync_ShouldReturnTypeMismatchError()
    {
        // Arrange
        var flagKey = "test-flag";
        var defaultValue = 3.14;
        var mockClient = Substitute.For<ConfigurationClient>();
        var provider = new AzureAppConfigProvider(mockClient);

        // Act
        var result = await provider.ResolveDoubleValueAsync(flagKey, defaultValue);

        // Assert
        Assert.Equal(flagKey, result.FlagKey);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal(ErrorType.TypeMismatch, result.ErrorType);
        Assert.Equal(Reason.Error, result.Reason);
        Assert.Equal("Double values are not supported. Use boolean feature flags only.", result.ErrorMessage);
    }

    [Fact]
    public async Task ResolveStructureValueAsync_ShouldReturnTypeMismatchError()
    {
        // Arrange
        var flagKey = "test-flag";
        var mockClient = Substitute.For<ConfigurationClient>();
        var provider = new AzureAppConfigProvider(mockClient);
        var defaultValue = new Value();

        // Act
        var result = await provider.ResolveStructureValueAsync(flagKey, defaultValue);

        // Assert
        Assert.Equal(flagKey, result.FlagKey);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal(ErrorType.TypeMismatch, result.ErrorType);
        Assert.Equal(Reason.Error, result.Reason);
        Assert.Equal("Structure values are not supported. Use boolean feature flags only.", result.ErrorMessage);
    }

    private static ConfigurationSetting CreateConfigurationSetting(string key, FeatureFlag featureFlag)
    {
        var json = JsonSerializer.Serialize(featureFlag);
        return new ConfigurationSetting(key, json);
    }
}
