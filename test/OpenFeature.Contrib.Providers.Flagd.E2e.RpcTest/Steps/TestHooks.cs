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

#if NET8_0_OR_GREATER
        var version = await File.ReadAllTextAsync("flagd-testbed-version.txt").ConfigureAwait(false);
#else
        var version = File.ReadAllText("flagd-testbed-version.txt");
#endif

        FlagdTestBed = new FlagdRpcTestBedContainer(version.Trim());
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
