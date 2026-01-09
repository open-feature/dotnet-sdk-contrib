using System;
using System.Net.Http;
using System.Threading.Tasks;
using OpenFeature.Contrib.Providers.Flagd.E2e.Common.Utils;
using Reqnroll;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.Common.Steps;

[Binding]
public class ProviderSteps
{
    private readonly State _state;

    public ProviderSteps(State state)
    {
        this._state = state;
    }

    [Given(@"a stable flagd provider")]
    public async Task GivenAStableProvider()
    {
        var api = this._state.Api = Api.Instance;
        var config = "default";

        var resolverType = this._state.ProviderResolverType
            ?? throw new ArgumentException("Resolver type not set in state");

        var port = resolverType switch
        {
            ResolverType.IN_PROCESS => SharedContext.Container.Container.GetMappedPublicPort(8015),
            ResolverType.RPC => SharedContext.Container.Container.GetMappedPublicPort(8013),
            _ => throw new ArgumentException($"Unknown resolver type: {resolverType}")
        };

        var host = SharedContext.Container.Container.Hostname;
        var flagdProvider = new FlagdProvider(
            FlagdConfig.Builder()
                .WithHost(host)
                .WithPort(port)
                .WithResolverType(resolverType)
                .Build()
            );

        using var client = new HttpClient();
        var httpPort = SharedContext.Container.Container.GetMappedPublicPort(8080);
        client.BaseAddress = new Uri($"http://{host}:{httpPort}");

        var content = new StringContent(string.Empty);
        using var result = await client.PostAsync($"start?config={config}", content);

        Assert.True(result.IsSuccessStatusCode, "Failed to start flagd");

        await Task.Delay(50); // Wait for flagd to be ready

        await api.SetProviderAsync(flagdProvider);

        this._state.Client = Api.Instance.GetClient("TestClient", "1.0.0");
    }

    [When("the connection is lost")]
    public async Task WhenTheConnectionIsLost()
    {
        var hostname = SharedContext.Container.Container.Hostname;
        var port = SharedContext.Container.Container.GetMappedPublicPort(8080);

        using var client = new HttpClient();
        client.BaseAddress = new Uri($"http://{hostname}:{port}");

        var content = new StringContent(string.Empty);
        using var result = await client.PostAsync("stop", content);

        Assert.True(result.IsSuccessStatusCode, "Failed to stop flagd");
    }

    [When("the connection is lost for {int}s")]
    public async Task WhenTheConnectionIsLostFor(int seconds)
    {
        var hostname = SharedContext.Container.Container.Hostname;
        var port = SharedContext.Container.Container.GetMappedPublicPort(8080);

        using var client = new HttpClient();
        client.BaseAddress = new Uri($"http://{hostname}:{port}");

        var content = new StringContent(string.Empty);
        using var result = await client.PostAsync($"restart?seconds={seconds}", content);

        Assert.True(result.IsSuccessStatusCode, "Failed to restart flagd");
    }

    [When("the flag was modified")]
    public async Task TheFlagWasModified()
    {
        var hostname = SharedContext.Container.Container.Hostname;
        var port = SharedContext.Container.Container.GetMappedPublicPort(8080);

        using var client = new HttpClient();
        client.BaseAddress = new Uri($"http://{hostname}:{port}");

        var content = new StringContent(string.Empty);
        using var result = await client.PostAsync($"change", content);

        Assert.True(result.IsSuccessStatusCode, "Failed to modify flags");
    }
}
