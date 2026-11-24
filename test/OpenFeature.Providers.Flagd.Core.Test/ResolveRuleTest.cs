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

    [Theory]
    [InlineData(1, 0, "on", "SPLIT_A", "a")]
    [InlineData(0, 1, "off", "SPLIT_B", "b")]
    public void FractionalBuckets_SelectedBucketResolveResultWins(int weightA, int weightB, string variant, string reason, string details)
    {
        var rule = @"{""fractional"": [{""var"": ""id""},
            [{""resolve"": [""on"", ""SPLIT_A"", [[""details"", ""a""], [""onlyA"", ""a""]]]}, WA],
            [{""resolve"": [""off"", ""SPLIT_B"", [[""details"", ""b""], [""onlyB"", ""b""]]]}, WB]
        ]}".Replace("WA", weightA.ToString()).Replace("WB", weightB.ToString());
        var result = Evaluate(rule, EvaluationContext.Builder().Set("id", "u1").Build());

        Assert.Equal(variant, result.Variant);
        Assert.Equal(reason, result.Reason);
        Assert.Equal(details, result.FlagMetadata.GetString("details"));
        Assert.Equal(weightA == 1, result.FlagMetadata.GetString("onlyA") != null);
        Assert.Equal(weightB == 1, result.FlagMetadata.GetString("onlyB") != null);
    }

    [Fact]
    public void ShorthandFractionalBucket_PassesThroughResolveResult()
    {
        var rule = @"{""fractional"": [[{""resolve"": [""on"", ""SPLIT""]}, 1], [""off"", 0]]}";
        var result = Evaluate(rule, EvaluationContext.Builder().SetTargetingKey("u1").Build());

        Assert.Equal("on", result.Variant);
        Assert.Equal("SPLIT", result.Reason);
    }

    [Fact]
    public void TerminalIfInFractionalBucket_PassesThroughResolveResult()
    {
        var rule = @"{""fractional"": [{""var"": ""id""}, [{""if"": [true, {""resolve"": [""on"", ""IN_IF""]}, ""off""]}, 1]]}";
        var result = Evaluate(rule, EvaluationContext.Builder().Set("id", "u1").Build());

        Assert.Equal("on", result.Variant);
        Assert.Equal("IN_IF", result.Reason);
    }

    [Fact]
    public void FractionalInIfBranch_PassesThroughResolveResult()
    {
        var rule = @"{""if"": [true, {""fractional"": [{""var"": ""id""}, [{""resolve"": [""on"", ""SPLIT""]}, 1]]}, ""off""]}";
        var result = Evaluate(rule, EvaluationContext.Builder().Set("id", "u1").Build());

        Assert.Equal("on", result.Variant);
        Assert.Equal("SPLIT", result.Reason);
    }

    // matches flagd: a resolve in a weight expression doesn't replace the selected bucket's result
    [Theory]
    [InlineData("1")]
    [InlineData(@"{""if"": [{""resolve"": [""off"", ""WEIGHT""]}, 1, 0]}")]
    public void ResolveInFractionalWeight_KeepsBucketResolveResult(string weight)
    {
        var rule = @"{""fractional"": [{""var"": ""id""}, [{""resolve"": [""on"", ""SELECTED""]}, WEIGHT]]}".Replace("WEIGHT", weight);
        var result = Evaluate(rule, EvaluationContext.Builder().Set("id", "u1").Build());

        Assert.Equal("on", result.Variant);
        Assert.Equal("SELECTED", result.Reason);
    }

    // a resolve elsewhere in the rule must not turn a context sentinel string in a bucket into a resolve result
    [Theory]
    [InlineData(@"{""fractional"": [{""resolve"": [""on"", ""X""]}, [{""var"": ""x""}, 1]]}")]
    [InlineData(@"{""fractional"": [{""var"": ""id""}, [{""if"": [{""resolve"": [""on"", ""X""]}, {""var"": ""x""}, ""off""]}, 1]]}")]
    public void FractionalBucketWithContextSentinel_IsNotPromoted(string rule)
    {
        var ex = Assert.Throws<FeatureProviderException>(() =>
            Evaluate(rule, EvaluationContext.Builder().Set("id", "u1").Set("x", "\0resolve").Build()));

        Assert.Equal(ErrorType.General, ex.ErrorType);
    }

    [Fact]
    public void BooleanVariant_CoercedLikePlainTargetingResult()
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
    public void ComputedReservedMetadataKey_IsDropped()
    {
        var result = Evaluate(@"{""resolve"": [""on"", null, [[{""var"": ""k""}, ""x""]]]}",
            EvaluationContext.Builder().Set("k", "flagSetId").Build());

        Assert.Equal("on", result.Variant);
        Assert.Equal("set", result.FlagMetadata.GetString("flagSetId"));
    }

    // matches flagd: reserved reasons are ignored, exact match only
    [Theory]
    [InlineData("ERROR")]
    [InlineData("DISABLED")]
    [InlineData("DEFAULT")]
    [InlineData("FALLBACK")]
    [InlineData("STATIC")]
    [InlineData("UNKNOWN")]
    [InlineData("CACHED")]
    [InlineData("STALE")]
    public void ReservedReason_KeepsTargetingMatch(string reason)
    {
        var literal = Evaluate(@"{""resolve"": [""on"", ""REASON""]}".Replace("REASON", reason));
        Assert.Equal("on", literal.Variant);
        Assert.Equal(Reason.TargetingMatch, literal.Reason);

        var computed = Evaluate(@"{""resolve"": [""on"", {""var"": ""r""}]}", EvaluationContext.Builder().Set("r", reason).Build());
        Assert.Equal(Reason.TargetingMatch, computed.Reason);
    }

    [Theory]
    [InlineData("error")]
    [InlineData("Default")]
    [InlineData("ERROR ")]
    public void NearReservedReason_IsKept(string reason)
    {
        var result = Evaluate(@"{""resolve"": [""on"", {""var"": ""r""}]}", EvaluationContext.Builder().Set("r", reason).Build());

        Assert.Equal(reason, result.Reason);
    }

    [Theory]
    [InlineData(@"{""+"": [1, 2]}", 3.0)]
    [InlineData(@"{""*"": [1.5, 2]}", 3.0)]
    [InlineData(@"{""*"": [1.5, 3]}", 4.5)]
    public void ComputedNumberMetadata_IsDouble(string expr, double expected)
    {
        var result = Evaluate(@"{""resolve"": [""on"", ""R"", [[""n"", EXPR]]]}".Replace("EXPR", expr));

        Assert.Equal("on", result.Variant);
        Assert.Equal("R", result.Reason);
        Assert.Equal(expected, result.FlagMetadata.GetDouble("n"));
    }

    [Fact]
    public void LiteralNumberAndBoolMetadata_KeepTheirTypes()
    {
        var result = Evaluate(@"{""resolve"": [""on"", null, [[""n"", 2.5], [""b"", true]]]}");

        Assert.Equal(2.5, result.FlagMetadata.GetDouble("n"));
        Assert.True(result.FlagMetadata.GetBool("b"));
    }

    [Fact]
    public void EmptyMetadata_KeepsFlagMetadata()
    {
        var result = Evaluate(@"{""resolve"": [""on"", ""R"", []]}");

        Assert.Equal("on", result.Variant);
        Assert.Equal("R", result.Reason);
        Assert.Equal("flag-team", result.FlagMetadata.GetString("team"));
    }

    // matches flagd: a missing variant is a GENERAL error, like a plain targeting result naming one
    [Theory]
    [InlineData(@"{""resolve"": [""nope"", ""R"", [[""details"", ""x""]]]}")]
    [InlineData(@"{""resolve"": ["""", ""FALLBACK""]}")]
    [InlineData(@"{""resolve"": [{""var"": ""empty""}, ""FALLBACK""]}")]
    [InlineData(@"{""resolve"": [false]}")] // coerced to "false", which this flag doesn't define
    public void MissingOrEmptyVariant_ReturnsGeneralError(string targeting)
    {
        var ex = Assert.Throws<FeatureProviderException>(() =>
            Evaluate(targeting, EvaluationContext.Builder().Set("empty", "").Build()));

        Assert.Equal(ErrorType.General, ex.ErrorType);
    }

    // matches flagd: a nested rule that throws is a targeting parse error, not a malformed resolve
    [Theory]
    [InlineData(@"{""resolve"": [{""substr"": [1]}]}")]
    [InlineData(@"{""resolve"": [""on"", {""substr"": [1]}]}")]
    [InlineData(@"{""resolve"": [""on"", null, [[""details"", {""substr"": [1]}]]]}")]
    [InlineData(@"{""resolve"": [{""resolve"": [""on"", ""INNER""]}, {""substr"": [1]}]}")]
    public void ThrowingNestedRule_ReturnsParseErrorAndLeavesNoPendingResult(string targeting)
    {
        var ex = Assert.Throws<FeatureProviderException>(() => Evaluate(targeting));
        Assert.Equal(ErrorType.ParseError, ex.ErrorType);

        var next = Evaluate(@"{""if"": [true, ""on"", ""off""]}");
        Assert.Equal("on", next.Variant);
        Assert.Equal(Reason.TargetingMatch, next.Reason);
    }

    [Theory]
    [InlineData(@"{""resolve"": [""on"", null, [[""share"", {""/"": [1, 0]}]]]}", "non-finite metadata value")]
    [InlineData(@"{""resolve"": [{""var"": ""obj""}]}", "object variant")]
    [InlineData(@"{""resolve"": [""on"", {""var"": ""obj""}]}", "non-string reason")]
    [InlineData(@"{""resolve"": [""on"", null, [[1, ""x""]]]}", "non-string metadata key")]
    [InlineData(@"{""resolve"": [""on"", null, [[ ""only-key"" ]]]}", "malformed pair")]
    [InlineData(@"{""resolve"": ""on""}", "non-array args")]
    [InlineData(@"{""resolve"": [""on"", null, [], ""extra""]}", "more than 3 args")]
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

    [Fact]
    public void ResolveInIfCondition_IsNotPromotedByContextValueLookingLikeTheMarker()
    {
        // the condition's resolve is not terminal, so "\0resolve" is just a plain (missing) variant name
        var ex = Assert.Throws<FeatureProviderException>(() =>
            Evaluate(@"{""if"": [{""resolve"": [""on"", ""X""]}, {""var"": ""x""}, ""off""]}",
                EvaluationContext.Builder().Set("x", "\0resolve").Build()));

        Assert.Equal(ErrorType.General, ex.ErrorType);
    }
}
