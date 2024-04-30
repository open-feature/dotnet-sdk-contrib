using System;
using TechTalk.SpecFlow;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.Test.Process
{
    [Binding]
    [Scope(Feature = "flagd providers")]
    [Scope(Feature = "flagd json evaluation")]
    public class FlagdStepDefinitionsProcess : FlagdStepDefinitionsBase
    {
        static FlagdStepDefinitionsProcess()
        {
            var flagdProvider = new FlagdProvider(FlagdConfig.Builder().WithPort(9090).WithResolverType(ResolverType.IN_PROCESS).Build());
            Api.Instance.SetProviderAsync("process-test-flagd", flagdProvider).Wait(5000);
        }
        public FlagdStepDefinitionsProcess(ScenarioContext scenarioContext) : base(scenarioContext)
        {
            client = Api.Instance.GetClient("process-test-flagd");
        }
    }
}
