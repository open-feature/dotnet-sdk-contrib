using System.Threading.Tasks;
using Reqnroll;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.ProcessTest.Steps;

[Binding]
public class TestHooks
{
    public static FlagdSyncTestBedContainer FlagdSyncTestBed { get; private set; }

    [BeforeTestRun]
    public static async Task StartContainerAsync()
    {
        FlagdSyncTestBed = new FlagdSyncTestBedContainer();

        await FlagdSyncTestBed.Container.StartAsync().ConfigureAwait(false);
    }

    [AfterTestRun]
    public static async Task StopContainerAsync()
    {
        if (FlagdSyncTestBed != null)
        {
            await FlagdSyncTestBed.Container.StopAsync().ConfigureAwait(false);
            await FlagdSyncTestBed.Container.DisposeAsync().ConfigureAwait(false);
        }
    }
}
