using System;
using TechTalk.SpecFlow;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.Test.Process
{
    [Binding]
    public class EvaluationStepDefinitionsProcess : EvaluationStepDefinitionsBase
    {
        public EvaluationStepDefinitionsProcess(ScenarioContext scenarioContext) : base(scenarioContext)
        {
            var flagdProvider = new FlagdProvider(FlagdConfig.Builder().WithPort(9090).WithResolverType(ResolverType.IN_PROCESS).Build());
            Api.Instance.SetProviderAsync("process-test", flagdProvider).Wait(5000);
            client = Api.Instance.GetClient("process-test");
        }
    }
}
