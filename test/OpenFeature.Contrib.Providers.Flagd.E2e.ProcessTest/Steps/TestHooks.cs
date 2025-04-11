using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.ProcessTest.Steps
{
    [Binding]
    public class TestHooks
    {
        private static FlagdSyncTestBedContainer _container;

        [BeforeTestRun]
        public static async Task StartContainerAsync()
        {
            _container = new FlagdSyncTestBedContainer();

            await _container.Container.StartAsync();

            var port = _container.Container.GetMappedPublicPort(9090);

            var flagdProvider = new FlagdProvider(
                FlagdConfig.Builder()
                    .WithPort(port)
                    .WithResolverType(ResolverType.IN_PROCESS)
                    .Build()
                );

            Api.Instance.SetProviderAsync("process-test-flagd", flagdProvider).Wait(500);
        }

        [AfterTestRun]
        public static async Task StopContainerAsync()
        {
            if (_container != null)
            {
                await _container.Container.StopAsync();
                await _container.Container.DisposeAsync();
            }
        }
    }
}
