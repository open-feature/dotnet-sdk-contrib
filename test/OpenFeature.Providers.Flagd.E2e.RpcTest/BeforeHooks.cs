using System.Collections.Generic;
using System.Linq;
using OpenFeature.Providers.Flagd.E2e.Common.Utils;
using Reqnroll;
using Xunit;

namespace OpenFeature.Providers.Flagd.E2e.RpcTest;

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
        Skip.If(tags.Contains("operator-errors"), "Skipping operator-errors test; flagd server does not yet fall back to default on operator errors.");
        Skip.If(tags.Contains("semver-edge-cases"), "Skipping semver-edge-cases; flagd server not updated.");
        Skip.If(tags.Contains("evaluator-refs-whitespace"), "Skipping evaluator-refs-whitespace; flagd server not updated.");
        Skip.If(tags.Contains("non-existent-evaluator-ref"), "Skipping non-existent-evaluator-ref; flagd server not updated.");
        Skip.If(tags.Contains("fractional-single-entry"), "Skipping fractional-single-entry; flagd server not updated.");
    }
}
