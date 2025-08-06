using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Reqnroll;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.ProcessTest.Steps;

[Binding]
public class TestHooks
{
    public static FlagdSyncTestBedContainer FlagdSyncTestBed { get; private set; }

    [BeforeFeature]
    public static async Task StartContainerAsync(FeatureContext featureContext)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        if (configuration["E2E"] != "true")
        {
            featureContext.Set(true, "IgnoreTest");
            return;
        }

        featureContext.Set(false, "IgnoreTest");
        featureContext.Set(configuration, "Configuration");

        FlagdSyncTestBed = new FlagdSyncTestBedContainer();
        await FlagdSyncTestBed.Container.StartAsync().ConfigureAwait(false);
    }

    [AfterFeature]
    public static async Task StopContainerAsync()
    {
        if (FlagdSyncTestBed != null)
        {
            await FlagdSyncTestBed.Container.StopAsync().ConfigureAwait(false);
            await FlagdSyncTestBed.Container.DisposeAsync().ConfigureAwait(false);
        }
    }
}
