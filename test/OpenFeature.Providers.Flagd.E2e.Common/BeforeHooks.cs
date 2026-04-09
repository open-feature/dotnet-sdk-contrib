using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OpenFeature.Providers.Flagd.E2e.Common.Utils;
using Reqnroll;
using Xunit;

namespace OpenFeature.Providers.Flagd.E2e.Common;

[Binding]
public class BeforeHooks
{
    [BeforeTestRun]
    public static async Task BeforeTestRunAsync()
    {
#if NET8_0_OR_GREATER
        var version = await File.ReadAllTextAsync("flagd-testbed-version.txt").ConfigureAwait(false);
#else
        var version = File.ReadAllText("flagd-testbed-version.txt");
#endif
        var container = new FlagdTestBedContainer(version.Trim());
        await container.Container.StartAsync().ConfigureAwait(false);

        SharedContext.Container = container;
    }

    [AfterTestRun]
    public static async Task AfterTestRunAsync()
    {
        await SharedContext.Container.Container.StopAsync().ConfigureAwait(false);
        await SharedContext.Container.Container.DisposeAsync().ConfigureAwait(false);

        SharedContext.Container = null;
    }

    [BeforeScenario]
    public void BeforeScenario(ScenarioContext scenarioContext)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        Skip.If(configuration["E2E"] != "true", "Skipping test as E2E tests are disabled, enable them by updating the appsettings.json.");

        // Skip deprecated tests
        Skip.If(scenarioContext.ScenarioInfo.Tags.Contains("deprecated"), "Skipping deprecated test scenario.");
    }
}
