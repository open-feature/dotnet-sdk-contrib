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
        Skip.If(!tags.Contains("rpc"), "Skipping scenario because it does not have required tag.");
        Skip.If(tags.Contains("fractional-v1"), "Skipping legacy fractional bucketing test; v2 algorithm is implemented.");
        Skip.If(tags.Contains("fractional-v2"), "Skipping fractional-v2 test; flagd server does not support v2 bucketing yet.");
        Skip.If(tags.Contains("fractional-nested"), "Skipping fractional-nested test; flagd server does not support nested fractional yet.");
    }
}
