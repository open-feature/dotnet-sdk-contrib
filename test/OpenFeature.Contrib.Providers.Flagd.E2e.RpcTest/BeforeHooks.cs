using OpenFeature.Contrib.Providers.Flagd.E2e.Common.Utils;
using Reqnroll;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.RpcTest;

[Binding]
public class BeforeHooks
{
    private State State { get; set; }

    public BeforeHooks(State state)
    {
        this.State = state;
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        this.State.ProviderResolverType = ResolverType.RPC;
    }
}
