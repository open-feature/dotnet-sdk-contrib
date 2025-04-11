using TechTalk.SpecFlow;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.Test.Process
{
    [Binding, Scope(Feature = "Flag evaluation")]
    public class EvaluationStepDefinitionsRpc : EvaluationStepDefinitionsBase
    {
        public EvaluationStepDefinitionsRpc(ScenarioContext scenarioContext) : base(scenarioContext)
        {
            client = Api.Instance.GetClient("rpc-test-evaluation");
        }
    }
}
