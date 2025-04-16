using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.RpcTest.Steps
{
    [Binding]
    public class TestHooks
    {
        private static FlagdTestBedContainer _container;

        [BeforeTestRun]
        public static async Task StartContainerAsync()
        {
            _container = new FlagdTestBedContainer();

            await _container.Container.StartAsync();

            var host = _container.Container.Hostname;
            var port = _container.Container.GetMappedPublicPort(8013);

            var flagdProvider = new FlagdProvider(
                FlagdConfig.Builder()
                    .WithHost(host)
                    .WithPort(port)
                    .WithResolverType(ResolverType.RPC)
                    .Build()
                );

            await Api.Instance.SetProviderAsync("rpc-test-evaluation", flagdProvider);
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
