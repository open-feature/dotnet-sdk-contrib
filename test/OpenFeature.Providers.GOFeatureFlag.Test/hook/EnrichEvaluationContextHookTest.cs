using System.Collections.Generic;
using System.Threading.Tasks;
using OpenFeature.Constant;
using OpenFeature.Model;
using OpenFeature.Providers.GOFeatureFlag.Hooks;
using OpenFeature.Providers.GOFeatureFlag.Models;
using Xunit;

namespace OpenFeature.Providers.GOFeatureFlag.Test.hook;

public class EnrichEvaluationContextHookTest
{
    [Fact(DisplayName = "Should return the same context if no metadata provided")]
    public async Task ShouldReturnSameContextIfNoMetadataProvided()
    {
        var ctx = EvaluationContext.Builder().SetTargetingKey("xxx").Build();
        var hook = new EnrichEvaluationContextHook(null);
        var hookContext = new HookContext<string>(
            "testFlagKey",
            "default",
            FlagValueType.Boolean,
            new ClientMetadata("testClient", "1.0.0"),
            new Metadata("testProvider"),
            ctx
        );

        var res = await hook.BeforeAsync(hookContext);
        Assert.Equivalent(hookContext.EvaluationContext, res);
    }

    [Fact(DisplayName = "Should return the same context if metadata is empty")]
    public async Task ShouldReturnSameContextIfMetadataEmpty()
    {
        var ctx = EvaluationContext.Builder().SetTargetingKey("xxx").Build();
        var hook = new EnrichEvaluationContextHook(new ExporterMetadata());
        var hookContext = new HookContext<string>(
            "testFlagKey",
            "default",
            FlagValueType.Boolean,
            new ClientMetadata("testClient", "1.0.0"),
            new Metadata("testProvider"),
            ctx
        );
        var res = await hook.BeforeAsync(hookContext);
        Assert.Equivalent(hookContext.EvaluationContext, res);
    }

    [Fact(DisplayName = "Should add the exporter metadata to the evaluation context")]
    public async Task ShouldAddExporterMetadataToEvaluationContext()
    {
        var ctx = EvaluationContext.Builder().SetTargetingKey("xxx").Build();
        var metadata = new ExporterMetadata();
        metadata.Add("key1", "value1");
        metadata.Add("key2", "value2");
        var hook = new EnrichEvaluationContextHook(metadata);
        var hookContext = new HookContext<string>(
            "testFlagKey",
            "default",
            FlagValueType.Boolean,
            new ClientMetadata("testClient", "1.0.0"),
            new Metadata("testProvider"),
            ctx
        );
        var res = await hook.BeforeAsync(hookContext);
        var m = new Structure(new Dictionary<string, Value>
        {
            { "key1", new Value("value1") }, { "key2", new Value("value2") }
        });
        var want = EvaluationContext.Builder()
            .SetTargetingKey("xxx")
            .Set("gofeatureflag", m)
            .Build();
        Assert.Equivalent(want, res);
    }
}
