using System.Collections.Generic;
using System.Linq;
using OpenFeature.Contrib.Providers.Flagd.E2e.Common.Utils;
using Reqnroll;
using Xunit;

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
    public void BeforeScenario(ScenarioInfo scenarioInfo, FeatureInfo featureInfo)
    {
        this.State.ProviderResolverType = ResolverType.RPC;

        var scenarioTags = scenarioInfo.Tags;
        var featureTags = featureInfo.Tags;
        var tags = new HashSet<string>(scenarioTags.Concat(featureTags));
        Skip.If(!tags.Contains("rpc"), "Skipping scenario because it is not for the rpc resolver.");
    }
}
