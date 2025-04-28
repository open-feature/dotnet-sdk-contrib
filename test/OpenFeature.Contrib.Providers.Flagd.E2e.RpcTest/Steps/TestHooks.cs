using System.Threading.Tasks;
using Reqnroll;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.RpcTest.Steps;

[Binding]
public class TestHooks
{
    public static FlagdRpcTestBedContainer FlagdTestBed { get; private set; }

    [BeforeTestRun]
    public static async Task StartContainerAsync()
    {
        FlagdTestBed = new FlagdRpcTestBedContainer();

        await FlagdTestBed.Container.StartAsync();
    }

    [AfterTestRun]
    public static async Task StopContainerAsync()
    {
        if (FlagdTestBed != null)
        {
            await FlagdTestBed.Container.StopAsync();
            await FlagdTestBed.Container.DisposeAsync();
        }
    }
}
