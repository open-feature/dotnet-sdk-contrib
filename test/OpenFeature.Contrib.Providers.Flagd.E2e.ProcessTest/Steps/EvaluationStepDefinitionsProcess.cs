using TechTalk.SpecFlow;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.Test.Process
{
    [Binding, Scope(Feature = "Flag evaluation")]
    public class EvaluationStepDefinitionsProcess : EvaluationStepDefinitionsBase
    {
        public EvaluationStepDefinitionsProcess(ScenarioContext scenarioContext) : base(scenarioContext)
        {
            client = Api.Instance.GetClient("process-test-evaluation");
        }
    }
}
