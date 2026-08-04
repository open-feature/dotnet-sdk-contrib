using System.Collections.Generic;
using System.Linq;
using OpenFeature.Providers.Flagd.E2e.Common.Utils;
using Reqnroll;
using Xunit;

namespace OpenFeature.Providers.Flagd.E2e.ProcessTest;

[Binding]
public class BeforeHooks
{
    private State State { get; set; }

    public BeforeHooks(State state)
    {
        this.State = state;
    }

    [BeforeScenario(Order = 1)]
    public void BeforeScenario(ScenarioInfo scenarioInfo, FeatureInfo featureInfo)
    {
        this.State.ProviderResolverType = ResolverType.IN_PROCESS;

        var scenarioTags = scenarioInfo.Tags;
        var featureTags = featureInfo.Tags;
        var tags = new HashSet<string>(scenarioTags.Concat(featureTags));
        Skip.If(!tags.Contains("in-process"), "Skipping scenario because it does not have required tag.");
        Skip.If(tags.Contains("fractional-v1"), "Skipping legacy fractional bucketing test; v2 algorithm is implemented.");
        // TODO: remove once the CBOR-based fractional hashing from the flagd
        // "fractional-non-string-rand-units" ADR is implemented (see open-feature/dotnet-sdk-contrib#516).
        Skip.If(tags.Contains("fractional-v3"), "Skipping fractional v3 (CBOR-encoded hashing input) test; v2 algorithm is implemented.");
    }
}
