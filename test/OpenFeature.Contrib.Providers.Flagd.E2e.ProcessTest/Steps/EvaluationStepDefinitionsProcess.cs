using System.Threading.Tasks;
using OpenFeature.Contrib.Providers.Flagd.E2e.Common;
using Reqnroll;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.ProcessTest.Steps;

[Binding, Scope(Feature = "Flag evaluation")]
public class EvaluationStepDefinitionsProcess : EvaluationStepDefinitionsBase
{
    public EvaluationStepDefinitionsProcess(ScenarioContext scenarioContext) : base(scenarioContext)
    {
    }

    [BeforeScenario]
    public static async Task BeforeScenarioAsync(ScenarioContext scenarioContext, FeatureContext featureContext)
    {
        var ignoreTest = featureContext.Get<bool>("IgnoreTest");
        Skip.If(ignoreTest, "Skipping test as E2E tests are disabled, enable them by updating the appsettings.json");

        var host = TestHooks.FlagdSyncTestBed.Container.Hostname;
        var port = TestHooks.FlagdSyncTestBed.Container.GetMappedPublicPort(8015);

        var flagdProvider = new FlagdProvider(
            FlagdConfig.Builder()
                .WithHost(host)
                .WithPort(port)
                .WithResolverType(ResolverType.IN_PROCESS)
                .Build()
            );

        await Api.Instance.SetProviderAsync("process-test-evaluation", flagdProvider).ConfigureAwait(false);

        var client = Api.Instance.GetClient("process-test-evaluation");

        scenarioContext.Set(client, "Client");
    }
}
