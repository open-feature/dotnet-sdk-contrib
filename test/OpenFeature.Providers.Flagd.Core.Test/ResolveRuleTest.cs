using NSubstitute;
using OpenFeature.Constant;
using OpenFeature.Error;
using OpenFeature.Model;
using OpenFeature.Providers.Flagd.Core;
using Xunit;

namespace OpenFeature.Providers.Flagd.Core.Test;

public class ResolveRuleTest
{
    private readonly JsonEvaluator _evaluator;

    private const string FlagConfig = @"{
        ""metadata"": { ""flagSetId"": ""set"", ""version"": ""1"", ""team"": ""set-team"", ""owner"": ""set-owner"" },
        ""flags"": {
            ""f"": {
                ""state"": ""ENABLED"",
                ""defaultVariant"": ""off"",
                ""variants"": { ""on"": ""ON"", ""off"": ""OFF"", ""true"": ""TRUE"" },
                ""targeting"": {TARGETING},
                ""metadata"": { ""team"": ""flag-team"" }
            }
        }
    }";

    public ResolveRuleTest()
    {
        var mockValidator = Substitute.For<IJsonSchemaValidator>();
        _evaluator = new JsonEvaluator(mockValidator);
    }

    private ResolutionDetails<string> Evaluate(string targeting, EvaluationContext ctx = null)
    {
        var config = FlagConfig.Replace("{TARGETING}", targeting);
        _evaluator.Sync(FlagConfigurationUpdateType.ALL, config);
        return _evaluator.ResolveStringValueAsync("f", "x", ctx);
    }

    [Fact]
    public void ResolvesVariantReasonAndMetadata_RuleMetadataWinsOverFlagAndFlagSetMetadata()
    {
        var result = Evaluate(@"{""resolve"": [""on"", ""ALLOWLIST"", [[""details"", ""pilot""], [""team"", ""rule-team""]]]}");

        Assert.Equal("on", result.Variant);
        Assert.Equal("ON", result.Value);
        Assert.Equal("ALLOWLIST", result.Reason);
        Assert.Equal("set", result.FlagMetadata.GetString("flagSetId"));
        Assert.Equal("1", result.FlagMetadata.GetString("version"));
        Assert.Equal("set-owner", result.FlagMetadata.GetString("owner"));
        Assert.Equal("rule-team", result.FlagMetadata.GetString("team"));
        Assert.Equal("pilot", result.FlagMetadata.GetString("details"));
    }

    [Fact]
    public void MissingReason_KeepsTargetingMatch()
    {
        var result = Evaluate(@"{""resolve"": [""on""]}");

        Assert.Equal("on", result.Variant);
        Assert.Equal(Reason.TargetingMatch, result.Reason);
    }

    [Fact]
    public void NullReason_KeepsTargetingMatch()
    {
        var result = Evaluate(@"{""resolve"": [""on"", null, [[""details"", ""x""]]]}");

        Assert.Equal(Reason.TargetingMatch, result.Reason);
        Assert.Equal("x", result.FlagMetadata.GetString("details"));
    }

    [Fact]
    public void IfBranches_DistinguishesSameVariantByReason()
    {
        var rule = @"{""if"": [
            {""=="": [{""var"": ""tier""}, ""gold""]},  {""resolve"": [""on"", ""TIER""]},
            {""=="": [{""var"": ""beta""}, true]},      {""resolve"": [""on"", ""BETA""]},
            {""resolve"": [""off"", ""FALLTHROUGH""]}
        ]}";

        var gold = Evaluate(rule, EvaluationContext.Builder().Set("tier", "gold").Build());
        Assert.Equal("on", gold.Variant);
        Assert.Equal("TIER", gold.Reason);

        var beta = Evaluate(rule, EvaluationContext.Builder().Set("beta", true).Build());
        Assert.Equal("on", beta.Variant);
        Assert.Equal("BETA", beta.Reason);

        var fallthrough = Evaluate(rule, EvaluationContext.Builder().Build());
        Assert.Equal("off", fallthrough.Variant);
        Assert.Equal("FALLTHROUGH", fallthrough.Reason);
    }

    [Fact]
    public void FractionalBucket_PassesThroughResolveResult()
    {
        // The hash for id=u1 with key f puts this into the weight-1 bucket.
        var rule = @"{""fractional"": [{""var"": ""id""}, [{""resolve"": [""on"", ""SPLIT"", [[""details"", ""treatment""]]]}, 1]]}";
        var result = Evaluate(rule, EvaluationContext.Builder().Set("id", "u1").Build());

        Assert.Equal("on", result.Variant);
        Assert.Equal("SPLIT", result.Reason);
        Assert.Equal("treatment", result.FlagMetadata.GetString("details"));
    }

    [Fact]
    public void BooleanVariant_CoeercedLikePlainTargetingResult()
    {
        var result = Evaluate(@"{""resolve"": [true]}");

        Assert.Equal("true", result.Variant);
        Assert.Equal("TRUE", result.Value);
    }

    [Fact]
    public void ComputedBooleanVariant_Coerced()
    {
        var result = Evaluate(@"{""resolve"": [{""=="": [1, 1]}]}");

        Assert.Equal("true", result.Variant);
    }

    [Fact]
    public void ReservedMetadataKeys_AreDropped()
    {
        var rule = @"{""resolve"": [""on"", null, [[""flagSetId"", ""evil""], [""version"", ""9""]]]}";
        var result = Evaluate(rule);

        // the reserved keys from flag-set metadata must not be overwritten
        Assert.Equal("set", result.FlagMetadata.GetString("flagSetId"));
        Assert.Equal("1", result.FlagMetadata.GetString("version"));
    }

    [Fact]
    public void MissingVariant_ReturnsError()
    {
        var ex = Assert.Throws<FeatureProviderException>(() =>
            Evaluate(@"{""resolve"": [""nope"", ""R"", [[""details"", ""x""]]]}"));

        Assert.Equal(ErrorType.General, ex.ErrorType);
    }

    [Theory]
    [InlineData(@"{""resolve"": [""on"", null, [[""share"", {""/"": [1, 0]}]]]}", "non-finite metadata value")]
    [InlineData(@"{""resolve"": [{""var"": ""obj""}]}", "object variant")]
    [InlineData(@"{""resolve"": [""on"", {""var"": ""obj""}]}", "non-string reason")]
    [InlineData(@"{""resolve"": [""on"", null, [[1, ""x""]]]}", "non-string metadata key")]
    [InlineData(@"{""resolve"": [""on"", null, [[ ""only-key"" ]]]}", "malformed pair")]
    public void MalformedArgs_FallBackToDefaultVariant(string targeting, string _)
    {
        var ctx = EvaluationContext.Builder().Set("obj", new Value(new Structure(new System.Collections.Generic.Dictionary<string, Value>()))).Build();
        var result = Evaluate(targeting, ctx);

        Assert.Equal("off", result.Variant);
        Assert.Equal(Reason.Default, result.Reason);
    }

    [Fact]
    public void CannotBeForgedFromEvaluationContextData()
    {
        var ex = Assert.Throws<FeatureProviderException>(() =>
            Evaluate(@"{""var"": ""forged""}", EvaluationContext.Builder().Set("forged", new Value(new Structure(new System.Collections.Generic.Dictionary<string, Value> {
                { "variant", new Value("on") },
                { "reason", new Value("FORGED") }
            }))).Build()));

        Assert.Equal(ErrorType.ParseError, ex.ErrorType);
    }

    [Fact]
    public void NonTerminalResolve_DoesNotLeakIntoResultOrNextEvaluation()
    {
        var result = Evaluate(@"{""if"": [{""resolve"": [""on"", ""LEAKED""]}, ""true"", ""off""]}");

        Assert.Equal("true", result.Variant);
        Assert.Equal(Reason.TargetingMatch, result.Reason);

        var next = Evaluate(@"{""if"": [true, ""off"", ""on""]}");
        Assert.Equal("off", next.Variant);
        Assert.Equal(Reason.TargetingMatch, next.Reason);
    }
}
