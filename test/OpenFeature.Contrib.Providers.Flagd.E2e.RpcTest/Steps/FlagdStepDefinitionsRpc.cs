using TechTalk.SpecFlow;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.Test.Process
{
    [Binding]
    [Scope(Feature = "flagd providers")]
    [Scope(Feature = "flagd json evaluation")]
    public class FlagdStepDefinitionsRpc : FlagdStepDefinitionsBase
    {
        public FlagdStepDefinitionsRpc(ScenarioContext scenarioContext) : base(scenarioContext)
        {
            client = Api.Instance.GetClient("rpc-test-flagd");
        }
    }
}
