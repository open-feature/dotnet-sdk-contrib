using TechTalk.SpecFlow;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.Test.Process
{
    [Binding]
    [Scope(Feature = "flagd providers")]
    [Scope(Feature = "flagd json evaluation")]
    public class FlagdStepDefinitionsProcess : FlagdStepDefinitionsBase
    {
        public FlagdStepDefinitionsProcess(ScenarioContext scenarioContext) : base(scenarioContext)
        {
            client = Api.Instance.GetClient("process-test-flagd");
        }
    }
}
