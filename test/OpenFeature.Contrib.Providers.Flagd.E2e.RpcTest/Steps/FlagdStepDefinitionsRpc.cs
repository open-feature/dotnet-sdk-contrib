using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OpenFeature.Contrib.Providers.Flagd.E2e.Common;
using Reqnroll;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.RpcTest.Steps;

[Binding]
[Scope(Feature = "flagd providers")]
[Scope(Feature = "flagd json evaluation")]
public class FlagdStepDefinitionsRpc : FlagdStepDefinitionsBase
{
    public FlagdStepDefinitionsRpc(ScenarioContext scenarioContext) : base(scenarioContext)
    {
    }

    [BeforeScenario]
    public static async Task BeforeScenarioAsync(ScenarioContext scenarioContext)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        Skip.If(configuration["E2E"] != "true");

        var host = TestHooks.FlagdTestBed.Container.Hostname;
        var port = TestHooks.FlagdTestBed.Container.GetMappedPublicPort(8013);

        var flagdProvider = new FlagdProvider(
            FlagdConfig.Builder()
                .WithHost(host)
                .WithPort(port)
                .Build()
            );

        await Api.Instance.SetProviderAsync("rpc-test-flagd", flagdProvider).ConfigureAwait(false);

        var client = Api.Instance.GetClient("rpc-test-flagd");

        scenarioContext.Set(configuration, "Configuration");
        scenarioContext.Set(client, "Client");
    }
}
