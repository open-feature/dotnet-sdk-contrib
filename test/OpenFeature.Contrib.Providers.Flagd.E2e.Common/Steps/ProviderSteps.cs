using System;
using System.IO;
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

    [Given(@"a {} flagd provider")]
    public async Task GivenAStableProvider(string providerType)
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
        var builder = FlagdConfig.Builder()
            .WithHost(host)
            .WithPort(port)
            .WithResolverType(resolverType);

        switch (providerType.ToLowerInvariant())
        {
            case "stable":
                {
                    config = "default";
                    break;
                }
            case "ssl":
                {
                    var path = Path.Combine(AppContext.BaseDirectory, "custom-root-cert.crt");
                    builder.WithCertificatePath(path);

                    config = "ssl";
                    break;
                }
            case "metadata":
                {
                    config = "metadata";
                    break;
                }
            case "syncpayload":
                {
                    config = "sync-payload";
                    break;
                }
            default:
                throw new NotImplementedException("Provider type not supported.");
        }

        await StartFlagdTestBedAsync(config, host).ConfigureAwait(false);

        await Task.Delay(50); // Wait for flagd to be ready

        var flagdProvider = new FlagdProvider(builder.Build());
        await api.SetProviderAsync(flagdProvider).ConfigureAwait(false);

        this._state.Client = Api.Instance.GetClient("TestClient", "1.0.0");
    }

    private static async Task StartFlagdTestBedAsync(string config, string host)
    {
        using var result = await SendRequestToFlagdContainerAsync($"start?config={config}").ConfigureAwait(false);

        Assert.True(result.IsSuccessStatusCode, "Failed to start flagd");

        await Task.Delay(50); // Wait for flagd to be ready
    }

    [When("the connection is lost")]
    public async Task WhenTheConnectionIsLost()
    {
        using var result = await SendRequestToFlagdContainerAsync("stop").ConfigureAwait(false);

        Assert.True(result.IsSuccessStatusCode, "Failed to stop flagd");

        await Task.Delay(50); // Wait for flagd to be ready
    }

    [When("the connection is lost for {int}s")]
    public async Task WhenTheConnectionIsLostFor(int seconds)
    {
        using var result = await SendRequestToFlagdContainerAsync($"restart?seconds={seconds}").ConfigureAwait(false);

        Assert.True(result.IsSuccessStatusCode, "Failed to restart flagd");

        await Task.Delay(50); // Wait for flagd to be ready
    }

    [When("the flag was modified")]
    public async Task TheFlagWasModified()
    {
        using var result = await SendRequestToFlagdContainerAsync("change").ConfigureAwait(false);

        Assert.True(result.IsSuccessStatusCode, "Failed to modify flags");

        await Task.Delay(50); // Wait for flagd to be ready
    }

    private static async Task<HttpResponseMessage> SendRequestToFlagdContainerAsync(string requestUri)
    {
        var hostname = SharedContext.Container.Container.Hostname;
        var port = SharedContext.Container.Container.GetMappedPublicPort(8080);

        using var client = new HttpClient();
        client.BaseAddress = new Uri($"http://{hostname}:{port}");

        HttpResponseMessage result;
        var counter = 0;
        do
        {
            var content = new StringContent(string.Empty);
            result = await client.PostAsync(requestUri, content).ConfigureAwait(false);

            if (result.IsSuccessStatusCode)
            {
                break;
            }

            await Task.Delay(100); // Wait before retrying
        }
        while (counter < 10);

        return result;
    }
}
