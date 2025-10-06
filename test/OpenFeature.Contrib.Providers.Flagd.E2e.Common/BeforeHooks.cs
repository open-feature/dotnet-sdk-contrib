using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Reqnroll;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.Common;

[Binding]
public class BeforeHooks
{
    internal static FlagdTestBedContainer Container;

    private readonly IConfiguration _configuration;

    public BeforeHooks(IConfiguration configuration)
    {
        this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

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

        Container = container;
    }

    [AfterTestRun]
    public static async Task AfterTestRunAsync()
    {
        await Container.Container.StopAsync().ConfigureAwait(false);
        await Container.Container.DisposeAsync().ConfigureAwait(false);

        Container = null;
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        Skip.If(this._configuration["E2E"] != "true", "Skipping test as E2E tests are disabled, enable them by updating the appsettings.json.");
    }
}
