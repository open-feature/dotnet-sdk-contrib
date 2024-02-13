
using TechTalk.SpecFlow;


namespace OpenFeature.Contrib.Providers.Flagd.E2e.Test.Process
{
    [Binding]
    public class EvaluationStepDefinitionsRpc : EvaluationStepDefinitionsBase
    {
        public EvaluationStepDefinitionsRpc(ScenarioContext scenarioContext) : base(scenarioContext)
        {
            var flagdProvider = new FlagdProvider();
            Api.Instance.SetProviderAsync("rpc-test", flagdProvider).Wait(5000);
            client = Api.Instance.GetClient("rpc-test");
        }
    }
}
