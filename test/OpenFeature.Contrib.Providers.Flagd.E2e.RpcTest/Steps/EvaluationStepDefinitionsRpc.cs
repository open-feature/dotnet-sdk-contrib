using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OpenFeature.Contrib.Providers.Flagd.E2e.Common;
using Reqnroll;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.RpcTest.Steps;

[Binding, Scope(Feature = "Flag evaluation")]
public class EvaluationStepDefinitionsRpc : EvaluationStepDefinitionsBase
{
    public EvaluationStepDefinitionsRpc(ScenarioContext scenarioContext) : base(scenarioContext)
    {
    }

    [BeforeScenario]
    public static async Task BeforeScenarioAsync(ScenarioContext scenarioContext, FeatureContext featureContext)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        Skip.If(configuration["E2E"] != "true", "Skipping test as E2E tests are disabled, enable them by updating the appsettings.json.");

        var host = TestHooks.FlagdTestBed.Container.Hostname;
        var port = TestHooks.FlagdTestBed.Container.GetMappedPublicPort(8013);

        var flagdProvider = new FlagdProvider(
            FlagdConfig.Builder()
                .WithHost(host)
                .WithPort(port)
                .Build()
            );

        await Api.Instance.SetProviderAsync("rpc-test-evaluation", flagdProvider).ConfigureAwait(false);

        var client = Api.Instance.GetClient("rpc-test-evaluation");

        scenarioContext.Set(client, "Client");
        scenarioContext.Set(configuration, "Configuration");
    }
}
