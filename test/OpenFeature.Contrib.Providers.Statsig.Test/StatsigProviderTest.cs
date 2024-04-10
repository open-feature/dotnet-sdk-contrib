using AutoFixture.Xunit2;
using OpenFeature.Constant;
using OpenFeature.Model;
using System.Threading.Tasks;
using Xunit;
using Statsig;
namespace OpenFeature.Contrib.Providers.Statsig.Test;

public class StatsigProviderTest
{
    private StatsigProvider statsigProvider;

    public StatsigProviderTest()
    {
        statsigProvider = new StatsigProvider("secret-", new StatsigServerOptions() { LocalMode = true });
    }

    [Fact]
    public async Task StatsigProvider_Initialized_HasCorrectStatusAsync()
    {
        Assert.Equal(ProviderStatus.NotReady, statsigProvider.GetStatus());
        await statsigProvider.Initialize(null);
        Assert.Equal(ProviderStatus.Ready, statsigProvider.GetStatus());
    }

    [Theory]
    [InlineAutoData(true, true)]
    [InlineAutoData(false, false)]
    public async Task GetBooleanValue_ForFeatureWithContext(bool flagValue, bool expectedValue, string userId, string flagName)
    {
        // Arrange
        await statsigProvider.Initialize(null);
        var ec = EvaluationContext.Builder().SetTargetingKey(userId).Build();
        statsigProvider.ServerDriver.OverrideGate(flagName, flagValue, userId);

        // Act & Assert
        Assert.Equal(expectedValue, statsigProvider.ResolveBooleanValue(flagName, false, ec).Result.Value);
    }

    [Theory]
    [InlineAutoData(true, false)]
    [InlineAutoData(false, false)]
    public async Task GetBooleanValue_ForFeatureWithNoContext_ReturnsDefaultValue(bool flagValue, bool defaultValue, string flagName)
    {
        // Arrange
        await statsigProvider.Initialize(null);
        statsigProvider.ServerDriver.OverrideGate(flagName, flagValue);

        // Act & Assert
        Assert.Equal(defaultValue, statsigProvider.ResolveBooleanValue(flagName, defaultValue).Result.Value);
    }

    [Theory]
    [AutoData]
    [InlineAutoData(false)]
    [InlineAutoData(true)]
    public async Task GetBooleanValue_ForFeatureWithDefault(bool defaultValue, string flagName, string userId)
    {
        // Arrange
        await statsigProvider.Initialize(null);

        var ec = EvaluationContext.Builder().SetTargetingKey(userId).Build();

        // Act
        var result = await statsigProvider.ResolveBooleanValue(flagName, defaultValue, ec);

        //Assert
        Assert.Equal(defaultValue, result.Value);
    }

    [Fact]
    public async Task TestConcurrentInitilization_DoesntThrowException()
    {
        // Arrange
        var concurrencyTestClass = new StatsigProvider();
        const int numberOfThreads = 50;

        // Act & Assert
        var tasks = new Task[numberOfThreads];
        for (int i = 0; i < numberOfThreads; i++)
        {
            tasks[i] = concurrencyTestClass.Initialize(null);
        }

        await Task.WhenAll(tasks);
    }
}
