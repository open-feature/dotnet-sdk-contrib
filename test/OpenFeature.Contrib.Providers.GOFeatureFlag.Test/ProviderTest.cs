using System;
using System.Threading.Tasks;
using OpenFeature.Contrib.Providers.GOFeatureFlag.v2;
using OpenFeature.Contrib.Providers.GOFeatureFlag.v2.exception;
using Xunit;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.Test;

public class ProviderTest
{
    private static readonly string baseUrl = "http://gofeatureflag.org";

    [Collection("Common")]
    public class CommonTest
    {
        [Fact]
        public async Task getMetadata_validate_name()
        {
            var goFeatureFlagProvider = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
            {
                Timeout = new TimeSpan(19 * TimeSpan.TicksPerHour), Endpoint = baseUrl
            });
            await Api.Instance.SetProviderAsync(goFeatureFlagProvider);
            Assert.Equal("GO Feature Flag Provider", Api.Instance.GetProvider().GetMetadata().Name);
        }
    }

    [Collection("Constructor")]
    public class ConstructorTest
    {
        [Fact]
        private void constructor_options_null()
        {
            Assert.Throws<InvalidOption>(() => new GoFeatureFlagProvider(null));
        }

        [Fact]
        private void constructor_options_empty()
        {
            Assert.Throws<InvalidOption>(() => new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions()));
        }

        [Fact]
        private void constructor_options_empty_endpoint()
        {
            Assert.Throws<InvalidOption>(() =>
                new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions { Endpoint = "" }));
        }

        [Fact]
        private void constructor_options_only_timeout()
        {
            Assert.Throws<InvalidOption>(() => new GoFeatureFlagProvider(
                    new GoFeatureFlagProviderOptions { Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond) }
                )
            );
        }

        [Fact]
        private void constructor_options_valid_endpoint()
        {
            var exception = Record.Exception(() =>
                new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions { Endpoint = baseUrl }));
            Assert.Null(exception);
        }
    }
}
