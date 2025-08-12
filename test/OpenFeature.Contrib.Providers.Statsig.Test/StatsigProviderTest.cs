using System.Threading.Tasks;
using AutoFixture.Xunit2;
using OpenFeature.Model;
using Statsig;
using Xunit;

namespace OpenFeature.Contrib.Providers.Statsig.Test;

public class StatsigProviderTest
{
    private StatsigProvider statsigProvider;

    public StatsigProviderTest()
    {
        statsigProvider = new StatsigProvider("secret-", new StatsigServerOptions() { LocalMode = true });
    }

    [Theory]
    [InlineAutoData(true, true)]
    [InlineAutoData(false, false)]
    public async Task GetBooleanValueAsync_ForFeatureWithContext(bool flagValue, bool expectedValue, string userId, string flagName)
    {
        // Arrange
        await statsigProvider.InitializeAsync(null);
        var ec = EvaluationContext.Builder().SetTargetingKey(userId).Build();
        statsigProvider.ServerDriver.OverrideGate(flagName, flagValue, userId);

        // Act
        var result = await statsigProvider.ResolveBooleanValueAsync(flagName, false, ec);

        // Assert
        Assert.Equal(expectedValue, result.Value);
    }

    [Theory]
    [InlineAutoData(true, false)]
    [InlineAutoData(false, false)]
    public async Task GetBooleanValueAsync_ForFeatureWithNoContext_ReturnsDefaultValue(bool flagValue, bool defaultValue, string flagName)
    {
        // Arrange
        await statsigProvider.InitializeAsync(null);
        statsigProvider.ServerDriver.OverrideGate(flagName, flagValue);

        // Act
        var result = await statsigProvider.ResolveBooleanValueAsync(flagName, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result.Value);
    }

    [Theory]
    [AutoData]
    [InlineAutoData(false)]
    [InlineAutoData(true)]
    public async Task GetBooleanValueAsync_ForFeatureWithDefault(bool defaultValue, string flagName, string userId)
    {
        // Arrange
        await statsigProvider.InitializeAsync(null);

        var ec = EvaluationContext.Builder().SetTargetingKey(userId).Build();

        // Act
        var result = await statsigProvider.ResolveBooleanValueAsync(flagName, defaultValue, ec);

        //Assert
        Assert.Equal(defaultValue, result.Value);
    }

    [Fact]
    public async Task TestConcurrentInitialization_DoesntThrowException()
    {
        // Arrange
        var concurrencyTestClass = new StatsigProvider();
        const int numberOfThreads = 50;

        // Act & Assert
        var tasks = new Task[numberOfThreads];
        for (int i = 0; i < numberOfThreads; i++)
        {
            tasks[i] = concurrencyTestClass.InitializeAsync(null);
        }

        await Task.WhenAll(tasks);
    }
}
