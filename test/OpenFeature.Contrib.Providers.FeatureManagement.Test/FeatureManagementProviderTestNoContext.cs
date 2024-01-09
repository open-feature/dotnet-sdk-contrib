using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Xunit;

namespace OpenFeature.Contrib.Providers.FeatureManagement.Test
{
    public class FeatureManagementProviderTestNoContext
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task MissingFlagKey_ShouldReturnDefault(bool defaultValue)
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.enabled.json")
                .Build();

            var provider = new FeatureManagementProvider(configuration);

            // Act
            var result = await provider.ResolveBooleanValue("MissingFlagKey", defaultValue);

            // Assert
            Assert.Equal(defaultValue, result.Value);
        }

        [Theory]
        [InlineData("Flag_Boolean_AlwaysOn", true)]
        [InlineData("Flag_Boolean_AlwaysOff", false)]
        public async Task BooleanValue_ShouldReturnExpected(string key, bool expected)
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.enabled.json")
                .Build();

            var provider = new FeatureManagementProvider(configuration);

            // Act
            // Invert the expected value to ensure that the value is being read from the configuration
            var result = await provider.ResolveBooleanValue(key, !expected);

            // Assert
            Assert.Equal(expected, result.Value);
        }

        [Theory]
        [InlineData("Flag_Double_AlwaysOn", 1.0)]
        [InlineData("Flag_Double_AlwaysOff", -1.0)]
        public async Task DoubleValue_ShouldReturnExpected(string key, double expected)
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.enabled.json")
                .Build();
            var provider = new FeatureManagementProvider(configuration);

            // Act
            // Using 0.0 for the default to verify the value is being read from the configuration
            var result = await provider.ResolveDoubleValue(key, 0.0f);

            // Assert
            Assert.Equal(expected, result.Value);
        }

        [Theory]
        [InlineData("Flag_Integer_AlwaysOn", 1)]
        [InlineData("Flag_Integer_AlwaysOff", -1)]
        public async Task IntegerValue_ShouldReturnExpected(string key, int expected)
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.enabled.json")
                .Build();
            var provider = new FeatureManagementProvider(configuration);

            // Act
            // Using 0 for the default to verify the value is being read from the configuration
            var result = await provider.ResolveIntegerValue(key, 0);

            // Assert
            Assert.Equal(expected, result.Value);
        }

        [Theory]
        [InlineData("Flag_String_AlwaysOn", "FlagEnabled")]
        [InlineData("Flag_String_AlwaysOff", "FlagDisabled")]
        public async Task StringValue_ShouldReturnExpected(string key, string expected)
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.enabled.json")
                .Build();
            var provider = new FeatureManagementProvider(configuration);

            // Act
            // Using 0 for the default to verify the value is being read from the configuration
            var result = await provider.ResolveStringValue(key, "DefaultValue");

            // Assert
            Assert.Equal(expected, result.Value);
        }

        [Theory]
        [InlineData("Flag_Structure_AlwaysOn")]
        public async Task StructureValue_ShouldReturnExpected(string key)
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.enabled.json")
                .Build();
            var provider = new FeatureManagementProvider(configuration);

            // Act
            // Using 0 for the default to verify the value is being read from the configuration
            var result = await provider.ResolveStructureValue(key, null);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.True(result.Value.IsStructure);
            Assert.Equal(2, result.Value.AsStructure.Count);
        }
    }
}
