using OpenFeature.Contrib.Providers.Flagd.E2e.Common;
using Reqnroll;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.ProcessTest.Steps;

[Binding]
[Scope(Feature = "flagd providers")]
[Scope(Feature = "flagd json evaluation")]
public class FlagdStepDefinitionsProcess : FlagdStepDefinitionsBase
{
    static FlagdStepDefinitionsProcess()
    {
        var host = TestHooks.FlagdSyncTestBed.Container.Hostname;
        var port = TestHooks.FlagdSyncTestBed.Container.GetMappedPublicPort(8015);

        var flagdProvider = new FlagdProvider(
            FlagdConfig.Builder()
                .WithHost(host)
                .WithPort(port)
                .WithResolverType(ResolverType.IN_PROCESS)
                .Build()
            );

        Api.Instance.SetProviderAsync("process-test-flagd", flagdProvider).Wait(5000);
    }

    public FlagdStepDefinitionsProcess(ScenarioContext scenarioContext) : base(scenarioContext)
    {
        client = Api.Instance.GetClient("process-test-flagd");
    }
}
