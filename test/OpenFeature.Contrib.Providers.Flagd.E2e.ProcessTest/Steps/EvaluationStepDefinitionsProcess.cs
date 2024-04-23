using System;
using TechTalk.SpecFlow;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.Test.Process
{
    [Binding, Scope(Feature = "Flag evaluation")]
    public class EvaluationStepDefinitionsProcess : EvaluationStepDefinitionsBase
    {
        static EvaluationStepDefinitionsProcess()
        {
            var flagdProvider = new FlagdProvider(FlagdConfig.Builder().WithPort(9090).WithResolverType(ResolverType.IN_PROCESS).Build());
            Api.Instance.SetProviderAsync("process-test-evaluation", flagdProvider).Wait(5000);
        }
        public EvaluationStepDefinitionsProcess(ScenarioContext scenarioContext) : base(scenarioContext)
        {
            client = Api.Instance.GetClient("process-test-evaluation");
        }
    }
}
