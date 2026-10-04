using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Testing;
using OpenFeature.Constant;
using OpenFeature.Error;
using OpenFeature.Model;
using Xunit;

namespace OpenFeature.Providers.Flagd.Core.Test;

public class FlagdCoreTests
{
    private const string TwoFlags = @"{
  ""metadata"": { ""version"": ""v1"", ""count"": 3, ""enabled"": true },
  ""flags"": {
    ""boolFlag"": { ""state"": ""ENABLED"", ""variants"": { ""on"": true, ""off"": false }, ""defaultVariant"": ""on"" },
    ""stringFlag"": { ""state"": ""ENABLED"", ""variants"": { ""red"": ""#CC0000"" }, ""defaultVariant"": ""red"" }
  }
}";

    [Fact]
    public void ParameterlessConstructor_CreatesUsableEmptyCore()
    {
        var core = new FlagdCore();

        Assert.Empty(core.GetFlagSetMetadata());
        var ex = Assert.Throws<FeatureProviderException>(() => core.ResolveBoolean("missing", false));
        Assert.Equal(ErrorType.FlagNotFound, ex.ErrorType);
    }

    [Fact]
    public void Constructor_WithNullOptions_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new FlagdCore(null));
    }

    [Fact]
    public void SetConfigurations_WithNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new FlagdCore().SetConfigurations(null));
    }

    [Fact]
    public void SetConfigurations_InitialLoad_ReturnsAllKeys()
    {
        var core = new FlagdCore();

        var changed = core.SetConfigurations(TwoFlags);

        Assert.Equal(new[] { "boolFlag", "stringFlag" }, changed.OrderBy(k => k));
    }

    [Fact]
    public void GetFlagKeys_ReturnsAllLoadedKeys()
    {
        var core = new FlagdCore();
        Assert.Empty(core.GetFlagKeys());

        core.SetConfigurations(TwoFlags);
        core.SetConfigurations(TwoFlags);

        Assert.Equal(new[] { "boolFlag", "stringFlag" }, core.GetFlagKeys().OrderBy(k => k));
    }

    [Fact]
    public void SetConfigurations_SameConfigurationTwice_ReturnsNoChangedKeys()
    {
        var core = new FlagdCore();
        core.SetConfigurations(TwoFlags);

        Assert.Empty(core.SetConfigurations(TwoFlags));
    }

    [Fact]
    public void SetConfigurations_ReturnsAddedUpdatedAndRemovedKeys()
    {
        var core = new FlagdCore();
        core.SetConfigurations(TwoFlags);

        var changed = core.SetConfigurations(@"{
  ""metadata"": { ""version"": ""v1"", ""count"": 3, ""enabled"": true },
  ""flags"": {
    ""boolFlag"": { ""state"": ""ENABLED"", ""variants"": { ""on"": true, ""off"": false }, ""defaultVariant"": ""off"" },
    ""stringFlag"": { ""state"": ""ENABLED"", ""variants"": { ""red"": ""#CC0000"" }, ""defaultVariant"": ""red"" },
    ""newFlag"": { ""state"": ""ENABLED"", ""variants"": { ""on"": true }, ""defaultVariant"": ""on"" }
  }
}");

        Assert.Equal(new[] { "boolFlag", "newFlag" }, changed.OrderBy(k => k));

        changed = core.SetConfigurations(@"{
  ""metadata"": { ""version"": ""v1"", ""count"": 3, ""enabled"": true },
  ""flags"": {
    ""boolFlag"": { ""state"": ""ENABLED"", ""variants"": { ""on"": true, ""off"": false }, ""defaultVariant"": ""off"" }
  }
}");

        Assert.Equal(new[] { "newFlag", "stringFlag" }, changed.OrderBy(k => k));
    }

    [Fact]
    public void SetConfigurations_FlagSetMetadataChange_ReturnsAllKeys()
    {
        var core = new FlagdCore();
        core.SetConfigurations(TwoFlags);

        var changed = core.SetConfigurations(TwoFlags.Replace(@"""version"": ""v1""", @"""version"": ""v2"""));

        Assert.Equal(new[] { "boolFlag", "stringFlag" }, changed.OrderBy(k => k));
        Assert.Equal("v2", core.GetFlagSetMetadata()["version"]);
    }

    [Fact]
    public void SetConfigurations_SharedEvaluatorChange_ReturnsReferencingKeys()
    {
        const string config = @"{
  ""$evaluators"": { ""rule"": { ""=="": [1, 1] } },
  ""flags"": {
    ""usesRule"": { ""state"": ""ENABLED"", ""variants"": { ""on"": true, ""off"": false }, ""defaultVariant"": ""off"",
                  ""targeting"": { ""if"": [{ ""$ref"": ""rule"" }, ""on""] } },
    ""static"": { ""state"": ""ENABLED"", ""variants"": { ""on"": true }, ""defaultVariant"": ""on"" }
  }
}";
        var core = new FlagdCore();
        core.SetConfigurations(config);

        var changed = core.SetConfigurations(config.Replace(@"[1, 1]", @"[1, 2]"));

        Assert.Equal(new[] { "usesRule" }, changed);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData(@"{ ""flags"": { ""f"": { ""variants"": ""not-an-object"" } } }")]
    [InlineData(@"{ ""flags"": { ""f"": null } }")]
    [InlineData("[]")]
    public void SetConfigurations_InvalidConfiguration_ThrowsParseErrorException(string json)
    {
        Assert.Throws<ParseErrorException>(() => new FlagdCore().SetConfigurations(json));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{ not json")]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData(@"{ ""flags"": { ""f"": null } }")]
    public async Task SetConfigurations_InvalidConfigurationAfterInitialize_ThrowsParseErrorAndKeepsFlags(string json)
    {
        var core = new FlagdCore();
        await core.InitializeAsync();
        core.SetConfigurations(TwoFlags);

        Assert.Throws<ParseErrorException>(() => core.SetConfigurations(json));

        Assert.True(core.ResolveBoolean("boolFlag", false).Value);
        Assert.Equal("v1", core.GetFlagSetMetadata()["version"]);
    }

    [Fact]
    public async Task Resolve_ConcurrentWithSetConfigurations_SeesOneConsistentConfiguration()
    {
        static string Config(string version) => @"{
  ""metadata"": { ""version"": """ + version + @""" },
  ""flags"": { ""f"": { ""state"": ""ENABLED"", ""variants"": { ""v"": """ + version + @""" }, ""defaultVariant"": ""v"" } }
}";
        var configs = new[] { Config("a"), Config("b") };
        var core = new FlagdCore();
        core.SetConfigurations(configs[0]);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var writer = Task.Run(() =>
        {
            for (var i = 0; i < 2000; i++)
            {
                core.SetConfigurations(configs[i % 2]);
            }

            cts.Cancel();
        });
        var readers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
            {
                var details = core.ResolveString("f", "default");
                // the flag value and the flag-set metadata merged into it must come from the same configuration
                Assert.Equal(details.Value, details.FlagMetadata.GetString("version"));
            }
        })).ToArray();

        await Task.WhenAll(readers.Concat(new[] { writer }));
    }

    [Fact]
    public void SetConfigurations_InvalidMetadata_ThrowsParseErrorException()
    {
        Assert.Throws<ParseErrorException>(() => new FlagdCore().SetConfigurations(Utils.invalidFlagSetMetadata));
        Assert.Throws<ParseErrorException>(() => new FlagdCore().SetConfigurations(Utils.invalidFlagMetadata));
    }

    [Fact]
    public void SetConfigurations_InvalidConfiguration_KeepsPreviousFlagsAndMetadata()
    {
        var core = new FlagdCore();
        core.SetConfigurations(TwoFlags);

        Assert.Throws<ParseErrorException>(() => core.SetConfigurations("{ not json"));
        Assert.Throws<ParseErrorException>(() => core.SetConfigurations(Utils.invalidFlagSetMetadata));

        Assert.True(core.ResolveBoolean("boolFlag", false).Value);
        Assert.Equal("#CC0000", core.ResolveString("stringFlag", "x").Value);
        Assert.Equal("v1", core.GetFlagSetMetadata()["version"]);
    }

    [Fact]
    public void GetFlagSetMetadata_ReturnsTypedValues()
    {
        var core = new FlagdCore();
        core.SetConfigurations(TwoFlags);

        var metadata = core.GetFlagSetMetadata();

        Assert.Equal("v1", metadata["version"]);
        Assert.Equal(3.0, metadata["count"]);
        Assert.Equal(true, metadata["enabled"]);
    }

    [Fact]
    public void GetFlagSetMetadata_IsReplacedByNewConfiguration()
    {
        var core = new FlagdCore();
        core.SetConfigurations(TwoFlags);

        core.SetConfigurations(Utils.validFlagConfig);

        Assert.Empty(core.GetFlagSetMetadata());
    }

    [Fact]
    public void Resolve_TypedPassthrough_ReturnsResolutionDetails()
    {
        var core = new FlagdCore();
        core.SetConfigurations(Utils.flags);

        var boolDetails = core.ResolveBoolean("staticBoolFlag", false);
        Assert.True(boolDetails.Value);
        Assert.Equal("on", boolDetails.Variant);
        Assert.Equal(Reason.Static, boolDetails.Reason);

        Assert.Equal("#CC0000", core.ResolveString("staticStringFlag", "").Value);
        Assert.Equal(1, core.ResolveInteger("staticIntFlag", 0).Value);
        Assert.Equal(1.0, core.ResolveDouble("staticFloatFlag", 0).Value);
        Assert.Equal(123, core.ResolveStructure("staticObjectFlag", new Value()).Value.AsStructure["abc"].AsInteger);
    }

    [Fact]
    public void Resolve_WithTargeting_UsesEvaluationContext()
    {
        var core = new FlagdCore();
        core.SetConfigurations(Utils.flags);

        var yellow = EvaluationContext.Builder().Set("color", "yellow").Build();

        var matched = core.ResolveBoolean("targetingBoolFlag", false, yellow);
        Assert.True(matched.Value);
        Assert.Equal(Reason.TargetingMatch, matched.Reason);

        Assert.False(core.ResolveBoolean("targetingBoolFlag", true).Value);
    }

    [Fact]
    public void Resolve_FlagMetadata_MergesFlagSetMetadata()
    {
        var core = new FlagdCore();
        core.SetConfigurations(Utils.metadataFlags);

        var details = core.ResolveBoolean("metadata-flag", false);

        Assert.Equal("1.0.2", details.FlagMetadata.GetString("string"));
    }

    [Fact]
    public void Resolve_Errors_AreSurfacedAsFeatureProviderExceptions()
    {
        var core = new FlagdCore();
        core.SetConfigurations(Utils.flags);

        Assert.Equal(ErrorType.FlagNotFound,
            Assert.Throws<FeatureProviderException>(() => core.ResolveBoolean("nope", false)).ErrorType);
        Assert.Equal(ErrorType.TypeMismatch,
            Assert.Throws<FeatureProviderException>(() => core.ResolveBoolean("staticStringFlag", false)).ErrorType);
        Assert.Equal(Reason.Disabled, core.ResolveBoolean("disabledFlag", false).Reason);
    }

    [Fact]
    public async Task InitializeAsync_EnablesSchemaValidationWarnings()
    {
        var logger = new FakeLogger();
        var core = new FlagdCore(new FlagdCoreOptions { Logger = logger });

        await core.InitializeAsync();
        core.SetConfigurations(Utils.invalidFlagConfig);

        Assert.Contains(logger.Collector.GetSnapshot(), r => r.Message.Contains("Schema Validation errors"));
    }

    [Fact]
    public void SetConfigurations_WithoutInitializeAsync_DoesNotValidateAgainstSchema()
    {
        var logger = new FakeLogger();
        var core = new FlagdCore(new FlagdCoreOptions { Logger = logger });

        core.SetConfigurations(Utils.invalidFlagConfig);

        Assert.Empty(logger.Collector.GetSnapshot());
    }
}
