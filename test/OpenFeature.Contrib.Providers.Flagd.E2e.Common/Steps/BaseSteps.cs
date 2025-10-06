using System;
using System.Threading.Tasks;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.Common.Steps;

public class BaseSteps
{
    protected TestContext Context { get; }

    public BaseSteps(TestContext testContext)
    {
        this.Context = testContext ?? throw new ArgumentNullException(nameof(testContext));
    }

    protected async Task<FeatureClient> CreateFeatureClientAsync()
    {
        var api = Api.Instance;
        var resolverType = this.Context.ProviderResolverType;

        var port = resolverType switch
        {
            ResolverType.IN_PROCESS => BeforeHooks.Container.Container.GetMappedPublicPort(8015),
            ResolverType.RPC => BeforeHooks.Container.Container.GetMappedPublicPort(8013),
            _ => throw new ArgumentException($"Unknown resolver type: {resolverType}")
        };

        var host = BeforeHooks.Container.Container.Hostname;
        var flagdProvider = new FlagdProvider(
            FlagdConfig.Builder()
                .WithHost(host)
                .WithPort(port)
                .WithResolverType(resolverType)
                .Build()
            );

        await api.SetProviderAsync(flagdProvider);

        return api.GetClient("openfeatureclient");
    }
}
