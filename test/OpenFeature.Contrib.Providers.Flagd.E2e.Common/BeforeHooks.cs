using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Reqnroll;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.Common;

[Binding]
public class BeforeHooks
{
    private readonly IConfiguration _configuration;

    public BeforeHooks(IConfiguration configuration)
    {
        this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    [BeforeTestRun]
    public static async Task BeforeTestRunAsync(FlagdTestBedContainer container)
    {
        await container.Container.StartAsync().ConfigureAwait(false);
    }

    [AfterTestRun]
    public static async Task AfterTestRunAsync(FlagdTestBedContainer container)
    {
        await container.Container.StopAsync().ConfigureAwait(false);
        await container.Container.DisposeAsync().ConfigureAwait(false);
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        Skip.If(this._configuration["E2E"] != "true", "Skipping test as E2E tests are disabled, enable them by updating the appsettings.json.");
    }
}
