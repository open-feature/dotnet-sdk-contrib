using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Reqnroll;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.RpcTest.Steps;

[Binding]
public class TestHooks
{
    public static FlagdRpcTestBedContainer FlagdTestBed { get; private set; }

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

        featureContext.Set(configuration, "Configuration");

        await FlagdTestBed.Container.StartAsync().ConfigureAwait(false);
    }

    [AfterFeature]
    public static async Task StopContainerAsync()
    {
        if (FlagdTestBed != null)
        {
            await FlagdTestBed.Container.StopAsync().ConfigureAwait(false);
            await FlagdTestBed.Container.DisposeAsync().ConfigureAwait(false);
        }
    }
}
