using System.Collections.Generic;
using System.Linq;
using OpenFeature.Contrib.Providers.Flagd.E2e.Common.Utils;
using Reqnroll;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.ProcessTest;

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
        Skip.If(!tags.Contains("in-process"), "Skipping scenario because it is not for the in-process resolver.");

        // TODO: https://github.com/open-feature/dotnet-sdk-contrib/issues/478
        Skip.If(tags.Contains("sync-port"), "Skipping sync-port as it is not supported.");
    }
}
