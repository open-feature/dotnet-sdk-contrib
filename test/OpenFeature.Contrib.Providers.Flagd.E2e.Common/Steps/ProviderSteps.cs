using System;
using System.Threading.Tasks;
using OpenFeature.Contrib.Providers.Flagd.E2e.Common.Utils;
using Reqnroll;

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
        var api = this._state.Api ?? Api.Instance;
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

        await api.SetProviderAsync(flagdProvider);

        this._state.Client = Api.Instance.GetClient("TestClient", "1.0.0");
    }
}
