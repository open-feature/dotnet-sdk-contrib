using System.Threading.Tasks;
using Reqnroll;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.Common;

[Binding]
public class BeforeHooks
{
    [BeforeTestRun]
    public static async Task BeforeTestRunAsync(FlagdTestBedContainer container)
    {
        await container.Container.StartAsync().ConfigureAwait(false);
    }

    [AfterTestRun]
    public static async Task AfterTestRunAsync(FlagdTestBedContainer container)
    {
        await container.Container.StopAsync().ConfigureAwait(false);
    }
}
