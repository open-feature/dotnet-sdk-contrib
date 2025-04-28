using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using ConfigCat.Client;
using OpenFeature.Constant;
using OpenFeature.Error;
using OpenFeature.Model;
using Xunit;

namespace OpenFeature.Contrib.ConfigCat.Test;

public class ConfigCatProviderTest
{
    const string TestConfigJson =
@"
{
  ""f"": {
    ""isAwesomeFeatureEnabled"": {
      ""t"": 0,
      ""v"": {
        ""b"": true
      }
    },
    ""isPOCFeatureEnabled"": {
      ""t"": 0,
      ""r"": [
        {
          ""c"": [
            {
              ""u"": {
                ""a"": ""Email"",
                ""c"": 2,
                ""l"": [
                  ""@example.com""
                ]
              }
            }
          ],
          ""s"": {
            ""v"": {
              ""b"": true
            }
          }
        }
      ],
      ""v"": {
        ""b"": false
      }
    }
  }
}
";

    [Theory]
    [AutoData]
    public async Task CreateConfigCatProvider_WithSdkKey_CreatesProviderInstanceSuccessfully(string sdkKey)
    {
        var configCatProvider =
            new ConfigCatProvider(sdkKey, options => { options.FlagOverrides = BuildFlagOverrides(); });

        await configCatProvider.InitializeAsync(EvaluationContext.Empty);

        Assert.NotNull(configCatProvider.Client);

        await configCatProvider.ShutdownAsync();
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

        await configCatProvider.InitializeAsync(EvaluationContext.Empty);

        var result = await configCatProvider.ResolveStructureValueAsync("example-feature", defaultValue);

        Assert.Equal(defaultValue.AsString, result.Value.AsString);
        Assert.Equal("example-feature", result.FlagKey);
        Assert.Equal(ErrorType.None, result.ErrorType);

        await configCatProvider.ShutdownAsync();
    }

    [Theory]
    [InlineAutoData("alice@configcat.com", false)]
    [InlineAutoData("bob@example.com", true)]
    public async Task OpenFeatureAPI_EndToEnd_Test(string email, bool expectedValue)
    {
        var configCatProvider = new ConfigCatProvider("fake-67890123456789012/1234567890123456789012", options =>
            { options.ConfigFetcher = new FakeConfigFetcher(TestConfigJson); });

        await OpenFeature.Api.Instance.SetProviderAsync(configCatProvider);

        var client = OpenFeature.Api.Instance.GetClient();

        var evaluationContext = EvaluationContext.Builder()
            .Set("email", email)
            .Build();

        var result = await client.GetBooleanDetailsAsync("isPOCFeatureEnabled", false, evaluationContext);

        Assert.Equal(expectedValue, result.Value);
        Assert.Equal("isPOCFeatureEnabled", result.FlagKey);
        Assert.Equal(ErrorType.None, result.ErrorType);

        await OpenFeature.Api.Instance.ShutdownAsync();
    }

    private static async Task ExecuteResolveTest<T>(object value, T defaultValue, T expectedValue, string sdkKey, Func<ConfigCatProvider, string, T, Task<ResolutionDetails<T>>> resolveFunc)
    {
        var configCatProvider = new ConfigCatProvider(sdkKey,
            options => { options.FlagOverrides = BuildFlagOverrides(("example-feature", value)); });

        await configCatProvider.InitializeAsync(EvaluationContext.Empty).ConfigureAwait(false);

        var result = await resolveFunc(configCatProvider, "example-feature", defaultValue).ConfigureAwait(false);

        Assert.Equal(expectedValue, result.Value);
        Assert.Equal("example-feature", result.FlagKey);
        Assert.Equal(ErrorType.None, result.ErrorType);

        await configCatProvider.ShutdownAsync().ConfigureAwait(false);
    }

    private static async Task ExecuteResolveErrorTest<T>(object value, T defaultValue, ErrorType expectedErrorType, string sdkKey, Func<ConfigCatProvider, string, T, Task<ResolutionDetails<T>>> resolveFunc)
    {
        var configCatProvider = new ConfigCatProvider(sdkKey,
            options => { options.FlagOverrides = BuildFlagOverrides(("example-feature", value)); });

        await configCatProvider.InitializeAsync(EvaluationContext.Empty).ConfigureAwait(false);

        var exception = await Assert.ThrowsAsync<FeatureProviderException>(() => resolveFunc(configCatProvider, "example-feature", defaultValue)).ConfigureAwait(false);

        Assert.Equal(expectedErrorType, exception.ErrorType);

        await configCatProvider.ShutdownAsync().ConfigureAwait(false);
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

    private sealed class FakeConfigFetcher : IConfigCatConfigFetcher
    {
        private readonly string configJson;

        public FakeConfigFetcher(string configJson)
        {
            this.configJson = configJson;
        }

        public void Dispose() { }

        public Task<FetchResponse> FetchAsync(FetchRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new FetchResponse(HttpStatusCode.OK, reasonPhrase: null, headers: Array.Empty<KeyValuePair<string, string>>(), this.configJson));
        }
    }
}
