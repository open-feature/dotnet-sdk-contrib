using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Xunit;

namespace OpenFeature.Contrib.Providers.FeatureManagement.Test;

public class FeatureManagementProviderSimpleFlagTest
{
    [Theory]
    [MemberData(nameof(TestData.BooleanSimple), MemberType = typeof(TestData))]
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
}
