using OpenFeature.Contrib.Providers.Flagd.E2e.Test;
using TechTalk.SpecFlow;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.ProcessTest.Steps
{
    [Binding, Scope(Feature = "Flag evaluation")]
    public class EvaluationStepDefinitionsProcess : EvaluationStepDefinitionsBase
    {
        static EvaluationStepDefinitionsProcess()
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

            Api.Instance.SetProviderAsync("process-test-evaluation", flagdProvider).Wait(5000);
        }

        public EvaluationStepDefinitionsProcess(ScenarioContext scenarioContext) : base(scenarioContext)
        {
            client = Api.Instance.GetClient("process-test-evaluation");
        }
    }
}
