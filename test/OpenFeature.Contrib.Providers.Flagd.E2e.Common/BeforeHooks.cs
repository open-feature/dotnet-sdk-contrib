using System.Threading.Tasks;
using Reqnroll;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.Common;

[Binding]
public class BeforeHooks
{
    private readonly FlagdTestBedContainer _container;

    public BeforeHooks(FlagdTestBedContainer container)
    {
        this._container = container;
    }

    [BeforeScenario]
    public async Task BeforeTestRunAsync()
    {
        await this._container.Container.StartAsync().ConfigureAwait(false);
    }

    [AfterScenario]
    public async Task AfterTestRunAsync()
    {
        await this._container.Container.StopAsync().ConfigureAwait(false);
    }
}
