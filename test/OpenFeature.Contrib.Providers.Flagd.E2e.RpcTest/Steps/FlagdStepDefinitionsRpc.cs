
using TechTalk.SpecFlow;


namespace OpenFeature.Contrib.Providers.Flagd.E2e.Test.Process
{
    [Binding]
    [Scope(Feature = "flagd providers")]
    [Scope(Feature = "flagd json evaluation")]
    public class FlagdStepDefinitionsRpc : FlagdStepDefinitionsBase
    {

        static FlagdStepDefinitionsRpc()
        {
            var flagdProvider = new FlagdProvider();
            Api.Instance.SetProviderAsync("rpc-test-flagd", flagdProvider).Wait(5000);
        }
        public FlagdStepDefinitionsRpc(ScenarioContext scenarioContext) : base(scenarioContext)
        {
            client = Api.Instance.GetClient("rpc-test-flagd");
        }
    }
}
