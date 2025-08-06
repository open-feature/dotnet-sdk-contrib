using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OpenFeature.Contrib.Providers.Flagd.E2e.Common;
using Reqnroll;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.ProcessTest.Steps;

[Binding]
[Scope(Feature = "flagd providers")]
[Scope(Feature = "flagd json evaluation")]
public class FlagdStepDefinitionsProcess : FlagdStepDefinitionsBase
{
    public FlagdStepDefinitionsProcess(ScenarioContext scenarioContext) : base(scenarioContext)
    {
    }

    [BeforeScenario]
    public static async Task BeforeScenarioAsync(ScenarioContext scenarioContext)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        Skip.If(configuration["E2E"] != "true", "Skipping test as E2E tests are disabled, enable them by updating the appsettings.json.");

        var host = TestHooks.FlagdSyncTestBed.Container.Hostname;
        var port = TestHooks.FlagdSyncTestBed.Container.GetMappedPublicPort(8015);

        var flagdProvider = new FlagdProvider(
            FlagdConfig.Builder()
                .WithHost(host)
                .WithPort(port)
                .WithResolverType(ResolverType.IN_PROCESS)
                .Build()
            );

        await Api.Instance.SetProviderAsync("process-test-flagd", flagdProvider).ConfigureAwait(false);

        var client = Api.Instance.GetClient("process-test-flagd");

        scenarioContext.Set(client, "Client");
        scenarioContext.Set(configuration, "Configuration");
    }
}
