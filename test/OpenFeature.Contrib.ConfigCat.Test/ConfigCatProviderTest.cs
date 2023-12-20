using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using ConfigCat.Client;
using OpenFeature.Constant;
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
        [InlineAutoData(true, false, true, ErrorType.None)]
        [InlineAutoData(false, true, false, ErrorType.None)]
        [InlineAutoData("false", true, true, ErrorType.TypeMismatch)]
        public Task GetBooleanValue_ForFeature_ReturnExpectedResult(object value, bool defaultValue, bool expectedValue, ErrorType expectedErrorType, string sdkKey)
        {
            return ExecuteResolveTest(value, defaultValue, expectedValue, expectedErrorType, sdkKey, (provider, key, def) => provider.ResolveBooleanValue(key, def));
        }

        [Theory]
        [InlineAutoData(1.0, 2.0, 1.0, ErrorType.None)]
        [InlineAutoData(1, 0, 0, ErrorType.TypeMismatch)]
        [InlineAutoData("false", 0, 0, ErrorType.TypeMismatch)]
        [InlineAutoData(false, 0, 0, ErrorType.TypeMismatch)]
        public Task GetDoubleValue_ForFeature_ReturnExpectedResult(object value, double defaultValue, double expectedValue, ErrorType expectedErrorType, string sdkKey)
        {
            return ExecuteResolveTest(value, defaultValue, expectedValue, expectedErrorType, sdkKey, (provider, key, def) => provider.ResolveDoubleValue(key, def));
        }

        [Theory]
        [InlineAutoData("some-value", "empty", "some-value", ErrorType.None)]
        [InlineAutoData(1, "empty", "empty", ErrorType.TypeMismatch)]
        [InlineAutoData(false, "empty", "empty", ErrorType.TypeMismatch)]
        public Task GetStringValue_ForFeature_ReturnExpectedResult(object value, string defaultValue, string expectedValue, ErrorType expectedErrorType, string sdkKey)
        {
            return ExecuteResolveTest(value, defaultValue, expectedValue, expectedErrorType, sdkKey, (provider, key, def) => provider.ResolveStringValue(key, def));
        }

        [Theory]
        [InlineAutoData(1, 2, 1, ErrorType.None)]
        [InlineAutoData(1.0, 0, 0, ErrorType.TypeMismatch)]
        [InlineAutoData("false", 0, 0, ErrorType.TypeMismatch)]
        [InlineAutoData(false, 0, 0, ErrorType.TypeMismatch)]
        public Task GetIntValue_ForFeature_ReturnExpectedResult(object value, int defaultValue, int expectedValue, ErrorType expectedErrorType, string sdkKey)
        {
            return ExecuteResolveTest(value, defaultValue, expectedValue, expectedErrorType, sdkKey, (provider, key, def) => provider.ResolveIntegerValue(key, def));
        }

        private static async Task ExecuteResolveTest<T>(object value, T defaultValue, T expectedValue, ErrorType expectedErrorType, string sdkKey, Func<ConfigCatProvider, string, T, Task<ResolutionDetails<T>>> resolveFunc)
        {
            var configCatProvider = new ConfigCatProvider(sdkKey,
                options => { options.FlagOverrides = BuildFlagOverrides(("example-feature", value)); });

            var result = await resolveFunc(configCatProvider, "example-feature", defaultValue);

            Assert.Equal(expectedValue, result.Value);
            Assert.Equal("example-feature", result.FlagKey);
            Assert.Equal(expectedErrorType, result.ErrorType);
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