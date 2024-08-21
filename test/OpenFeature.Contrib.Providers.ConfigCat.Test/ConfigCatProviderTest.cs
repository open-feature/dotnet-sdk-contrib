using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using ConfigCat.Client;
using OpenFeature.Constant;
using OpenFeature.Error;
using OpenFeature.Model;
using Xunit;

namespace OpenFeature.Contrib.ConfigCat.Test
{
    public class ConfigCatProviderTest
    {
        [Theory]
        [AutoData]
        public void CreateConfigCatProvider_WithSdkKey_CreatesProviderInstanceSuccessfully(string sdkKey)
        {
            var configCatProvider =
                new ConfigCatProvider(sdkKey, options => { options.FlagOverrides = BuildFlagOverrides(); });

            Assert.NotNull(configCatProvider.Client);
        }

        [Theory]
        [InlineAutoData(true, false, true)]
        [InlineAutoData(false, true, false)]
        public Task GetBooleanValueAsync_ForFeature_ReturnExpectedResult(object value, bool defaultValue, bool expectedValue, string sdkKey)
        {
            return ExecuteResolveTest(value, defaultValue, expectedValue, sdkKey, (provider, key, def) => provider.ResolveBooleanValueAsync(key, def));
        }

        [Theory]
        [InlineAutoData("false", true, ErrorType.TypeMismatch)]
        public Task GetBooleanValueAsync_ForFeature_ShouldThrowException(object value, bool defaultValue, ErrorType expectedErrorType, string sdkKey)
        {
            return ExecuteResolveErrorTest(value, defaultValue, expectedErrorType, sdkKey, (provider, key, def) => provider.ResolveBooleanValueAsync(key, def));
        }

        [Theory]
        [InlineAutoData(1.0, 2.0, 1.0)]
        public Task GetDoubleValueAsync_ForFeature_ReturnExpectedResult(object value, double defaultValue, double expectedValue, string sdkKey)
        {
            return ExecuteResolveTest(value, defaultValue, expectedValue, sdkKey, (provider, key, def) => provider.ResolveDoubleValueAsync(key, def));
        }

        [Theory]
        [InlineAutoData(1, 0, ErrorType.TypeMismatch)]
        [InlineAutoData("false", 0, ErrorType.TypeMismatch)]
        [InlineAutoData(false, 0, ErrorType.TypeMismatch)]
        public Task GetDoubleValueAsync_ForFeature_ShouldThrowException(object value, double defaultValue, ErrorType expectedErrorType, string sdkKey)
        {
            return ExecuteResolveErrorTest(value, defaultValue, expectedErrorType, sdkKey, (provider, key, def) => provider.ResolveDoubleValueAsync(key, def));
        }

        [Theory]
        [InlineAutoData("some-value", "empty", "some-value")]
        public Task GetStringValueAsync_ForFeature_ReturnExpectedResult(object value, string defaultValue, string expectedValue, string sdkKey)
        {
            return ExecuteResolveTest(value, defaultValue, expectedValue, sdkKey, (provider, key, def) => provider.ResolveStringValueAsync(key, def));
        }

        [Theory]
        [InlineAutoData(1, "empty", ErrorType.TypeMismatch)]
        [InlineAutoData(false, "empty", ErrorType.TypeMismatch)]
        public Task GetStringValueAsync_ForFeature_ShouldThrowException(object value, string defaultValue, ErrorType expectedErrorType, string sdkKey)
        {
            return ExecuteResolveErrorTest(value, defaultValue, expectedErrorType, sdkKey, (provider, key, def) => provider.ResolveStringValueAsync(key, def));
        }

        [Theory]
        [InlineAutoData(1, 2, 1)]
        public Task GetIntValue_ForFeature_ReturnExpectedResult(object value, int defaultValue, int expectedValue, string sdkKey)
        {
            return ExecuteResolveTest(value, defaultValue, expectedValue, sdkKey, (provider, key, def) => provider.ResolveIntegerValueAsync(key, def));
        }

        [Theory]
        [InlineAutoData(1.0, 0, ErrorType.TypeMismatch)]
        [InlineAutoData("false", 0, ErrorType.TypeMismatch)]
        [InlineAutoData(false, 0, ErrorType.TypeMismatch)]
        public Task GetIntValue_ForFeature_ShouldThrowException(object value, int defaultValue, ErrorType expectedErrorType, string sdkKey)
        {
            return ExecuteResolveErrorTest(value, defaultValue, expectedErrorType, sdkKey, (provider, key, def) => provider.ResolveIntegerValueAsync(key, def));
        }

        [Theory]
        [AutoData]
        public async Task GetStructureValueAsync_ForFeature_ReturnExpectedResult(string sdkKey)
        {
            const string jsonValue = "{ \"key\": \"value\" }";
            var defaultValue = new Value(jsonValue);
            var configCatProvider = new ConfigCatProvider(sdkKey,
                options => { options.FlagOverrides = BuildFlagOverrides(("example-feature", defaultValue.AsString)); });

            var result = await configCatProvider.ResolveStructureValueAsync("example-feature", defaultValue);

            Assert.Equal(defaultValue.AsString, result.Value.AsString);
            Assert.Equal("example-feature", result.FlagKey);
            Assert.Equal(ErrorType.None, result.ErrorType);
        }

        private static async Task ExecuteResolveTest<T>(object value, T defaultValue, T expectedValue, string sdkKey, Func<ConfigCatProvider, string, T, Task<ResolutionDetails<T>>> resolveFunc)
        {
            var configCatProvider = new ConfigCatProvider(sdkKey,
                options => { options.FlagOverrides = BuildFlagOverrides(("example-feature", value)); });

            var result = await resolveFunc(configCatProvider, "example-feature", defaultValue);

            Assert.Equal(expectedValue, result.Value);
            Assert.Equal("example-feature", result.FlagKey);
            Assert.Equal(ErrorType.None, result.ErrorType);
        }

        private static async Task ExecuteResolveErrorTest<T>(object value, T defaultValue, ErrorType expectedErrorType, string sdkKey, Func<ConfigCatProvider, string, T, Task<ResolutionDetails<T>>> resolveFunc)
        {
            var configCatProvider = new ConfigCatProvider(sdkKey,
                options => { options.FlagOverrides = BuildFlagOverrides(("example-feature", value)); });

            var exception = await Assert.ThrowsAsync<FeatureProviderException>(() => resolveFunc(configCatProvider, "example-feature", defaultValue));

            Assert.Equal(expectedErrorType, exception.ErrorType);
        }

        private static FlagOverrides BuildFlagOverrides(params (string key, object value)[] values)
        {
            var dictionary = new Dictionary<string, object>();
            foreach (var (key, value) in values)
            {
                dictionary.Add(key, value);
            }

            return FlagOverrides.LocalDictionary(dictionary, OverrideBehaviour.LocalOnly);
        }
    }
}