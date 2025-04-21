using OpenFeature.Contrib.Providers.Flagd.E2e.Test;
using Reqnroll;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.RpcTest.Steps
{
    [Binding, Scope(Feature = "Flag evaluation")]
    public class EvaluationStepDefinitionsRpc : EvaluationStepDefinitionsBase
    {
        static EvaluationStepDefinitionsRpc()
        {
            var host = TestHooks.FlagdTestBed.Container.Hostname;
            var port = TestHooks.FlagdTestBed.Container.GetMappedPublicPort(8013);

            var flagdProvider = new FlagdProvider(
                FlagdConfig.Builder()
                    .WithHost(host)
                    .WithPort(port)
                    .Build()
                );

            Api.Instance.SetProviderAsync("rpc-test-evaluation", flagdProvider).Wait(5000);
        }

        public EvaluationStepDefinitionsRpc(ScenarioContext scenarioContext) : base(scenarioContext)
        {
            client = Api.Instance.GetClient("rpc-test-evaluation");
        }
    }
}
