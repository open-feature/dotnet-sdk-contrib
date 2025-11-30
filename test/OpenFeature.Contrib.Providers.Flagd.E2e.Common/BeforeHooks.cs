using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OpenFeature.Contrib.Providers.Flagd.E2e.Common.Utils;
using Reqnroll;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.Common;

[Binding]
public class BeforeHooks
{
    [BeforeTestRun]
    public static async Task BeforeTestRunAsync()
    {
#if NET8_0_OR_GREATER
        var version = await File.ReadAllTextAsync("flagd-testbed-version.txt");
#else
        var version = File.ReadAllText("flagd-testbed-version.txt");
#endif
        var container = new FlagdTestBedContainer(version.Trim());
        await container.Container.StartAsync();

        SharedContext.Container = container;
    }

    [AfterTestRun]
    public static async Task AfterTestRunAsync()
    {
        await SharedContext.Container.Container.StopAsync();
        await SharedContext.Container.Container.DisposeAsync();

        SharedContext.Container = null;
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        Skip.If(configuration["E2E"] != "true", "Skipping test as E2E tests are disabled, enable them by updating the appsettings.json.");
    }
}
