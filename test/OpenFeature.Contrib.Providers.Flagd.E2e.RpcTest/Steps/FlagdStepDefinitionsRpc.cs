using OpenFeature.Contrib.Providers.Flagd.E2e.Test;
using Reqnroll;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.RpcTest.Steps;

[Binding]
[Scope(Feature = "flagd providers")]
[Scope(Feature = "flagd json evaluation")]
public class FlagdStepDefinitionsRpc : FlagdStepDefinitionsBase
{
    static FlagdStepDefinitionsRpc()
    {
        var host = TestHooks.FlagdTestBed.Container.Hostname;
        var port = TestHooks.FlagdTestBed.Container.GetMappedPublicPort(8013);

        var flagdProvider = new FlagdProvider(
            FlagdConfig.Builder()
                .WithHost(host)
                .WithPort(port)
                .Build()
            );

        Api.Instance.SetProviderAsync("rpc-test-flagd", flagdProvider).Wait(5000);
    }

    public FlagdStepDefinitionsRpc(ScenarioContext scenarioContext) : base(scenarioContext)
    {
        client = Api.Instance.GetClient("rpc-test-flagd");
    }
}
