
using TechTalk.SpecFlow;


namespace OpenFeature.Contrib.Providers.Flagd.E2e.Test.Process
{
    [Binding, Scope(Feature = "Flag evaluation")]
    public class EvaluationStepDefinitionsRpc : EvaluationStepDefinitionsBase
    {
        static EvaluationStepDefinitionsRpc()
        {
            var flagdProvider = new FlagdProvider();
            Api.Instance.SetProviderAsync("rpc-test-evaluation", flagdProvider).Wait(5000);
        }
        public EvaluationStepDefinitionsRpc(ScenarioContext scenarioContext) : base(scenarioContext)
        {
            client = Api.Instance.GetClient("rpc-test-evaluation");
        }
    }
}
