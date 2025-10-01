using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Reqnroll;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.RpcTest.Steps;

[Binding]
public class TestHooks
{
    public static FlagdRpcTestBedContainer FlagdTestBed { get; private set; }

    [BeforeTestRun]
    public static async Task StartContainerAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        if (configuration["E2E"] != "true")
        {
            return;
        }

        var version = File.ReadAllText("flagd-testbed-version.txt")
            .Trim();

        FlagdTestBed = new FlagdRpcTestBedContainer(version);
        await FlagdTestBed.Container.StartAsync().ConfigureAwait(false);
    }

    [AfterTestRun]
    public static async Task StopContainerAsync()
    {
        if (FlagdTestBed != null)
        {
            await FlagdTestBed.Container.StopAsync().ConfigureAwait(false);
            await FlagdTestBed.Container.DisposeAsync().ConfigureAwait(false);
        }
    }
}
