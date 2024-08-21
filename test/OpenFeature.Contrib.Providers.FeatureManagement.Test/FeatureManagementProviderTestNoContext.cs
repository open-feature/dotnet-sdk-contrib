using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Xunit;

namespace OpenFeature.Contrib.Providers.FeatureManagement.Test
{
    public class FeatureManagementProviderTestNoContext
    {
        [Theory]
        [MemberData(nameof(TestData.BooleanNoContext), MemberType = typeof(TestData))]
        public async Task BooleanValue_ShouldReturnExpected(string key, bool defaultValue, bool expectedValue)
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.enabled.json")
                .Build();

            var provider = new FeatureManagementProvider(configuration);

            // Act
            // Invert the expected value to ensure that the value is being read from the configuration
            var result = await provider.ResolveBooleanValueAsync(key, defaultValue);

            // Assert
            Assert.Equal(expectedValue, result.Value);
        }

        [Theory]
        [MemberData(nameof(TestData.DoubleNoContext), MemberType = typeof(TestData))]
        public async Task DoubleValue_ShouldReturnExpected(string key, double defaultValue, double expectedValue)
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.enabled.json")
                .Build();
            var provider = new FeatureManagementProvider(configuration);

            // Act
            // Using 0.0 for the default to verify the value is being read from the configuration
            var result = await provider.ResolveDoubleValueAsync(key, defaultValue);

            // Assert
            Assert.Equal(expectedValue, result.Value);
        }

        [Theory]
        [MemberData(nameof(TestData.IntegerNoContext), MemberType = typeof(TestData))]
        public async Task IntegerValue_ShouldReturnExpected(string key, int defaultValue, int expectedValue)
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.enabled.json")
                .Build();
            var provider = new FeatureManagementProvider(configuration);

            // Act
            // Using 0 for the default to verify the value is being read from the configuration
            var result = await provider.ResolveIntegerValueAsync(key, defaultValue);

            // Assert
            Assert.Equal(expectedValue, result.Value);
        }

        [Theory]
        [MemberData(nameof(TestData.StringNoContext), MemberType = typeof(TestData))]
        public async Task StringValue_ShouldReturnExpected(string key, string defaultValue, string expectedValue)
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.enabled.json")
                .Build();
            var provider = new FeatureManagementProvider(configuration);

            // Act
            // Using 0 for the default to verify the value is being read from the configuration
            var result = await provider.ResolveStringValueAsync(key, defaultValue);

            // Assert
            Assert.Equal(expectedValue, result.Value);
        }

        [Theory]
        [MemberData(nameof(TestData.StructureNoContext), MemberType = typeof(TestData))]
        public async Task StructureValue_ShouldReturnExpected(string key)
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.enabled.json")
                .Build();
            var provider = new FeatureManagementProvider(configuration);

            // Act
            // Using 0 for the default to verify the value is being read from the configuration
            var result = await provider.ResolveStructureValueAsync(key, null);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.True(result.Value.IsStructure);
            Assert.Equal(2, result.Value.AsStructure.Count);
        }
    }
}
