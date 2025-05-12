using System.Collections.Immutable;
using AutoFixture;
using NSubstitute;
using OpenFeature.Constant;
using OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess;
using OpenFeature.Error;
using OpenFeature.Model;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.Test;

public class UnitTestJsonEvaluator
{
    private Fixture _fixture;
    private IJsonSchemaValidator _mockJsonSchemaValidator;

    private JsonEvaluator _jsonEvaluator;

    public UnitTestJsonEvaluator()
    {
        _fixture = new Fixture();
        _mockJsonSchemaValidator = Substitute.For<IJsonSchemaValidator>();
        _jsonEvaluator = new JsonEvaluator(_fixture.Create<string>(), _mockJsonSchemaValidator);
    }

    [Fact]
    public void TestJsonEvaluatorAddFlagConfig()
    {
        _jsonEvaluator.Sync(FlagConfigurationUpdateType.ADD, Utils.validFlagConfig);

        var result = _jsonEvaluator.ResolveBooleanValueAsync("validFlag", false);

        Assert.True(result.Value);
    }

    [Fact]
    public void TestJsonEvaluatorAddStaticStringEvaluation()
    {
        _jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

        var result = _jsonEvaluator.ResolveStringValueAsync("staticStringFlag", "");

        Assert.Equal("#CC0000", result.Value);
        Assert.Equal("red", result.Variant);
        Assert.Equal(Reason.Static, result.Reason);
    }

    [Fact]
    public void TestJsonEvaluatorDynamicBoolEvaluation()
    {
        _jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

        var attributes = ImmutableDictionary.CreateBuilder<string, Value>();
        attributes.Add("color", new Value("yellow"));

        var builder = EvaluationContext.Builder();
        builder
            .Set("color", "yellow");

        var result = _jsonEvaluator.ResolveBooleanValueAsync("targetingBoolFlag", false, builder.Build());

        Assert.True(result.Value);
        Assert.Equal("bool1", result.Variant);
        Assert.Equal(Reason.TargetingMatch, result.Reason);
    }

    [Fact]
    public void TestJsonEvaluatorDynamicBoolEvaluationUsingFlagdPropertyFlagKey()
    {
        _jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

        var attributes = ImmutableDictionary.CreateBuilder<string, Value>();
        attributes.Add("color", new Value("yellow"));

        var builder = EvaluationContext.Builder();

        var result =
            _jsonEvaluator.ResolveBooleanValueAsync("targetingBoolFlagUsingFlagdProperty", false, builder.Build());

        Assert.True(result.Value);
        Assert.Equal("bool1", result.Variant);
        Assert.Equal(Reason.TargetingMatch, result.Reason);
    }

    [Fact]
    public void TestJsonEvaluatorDynamicBoolEvaluationUsingFlagdPropertyTimestamp()
    {
        _jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

        var attributes = ImmutableDictionary.CreateBuilder<string, Value>();
        attributes.Add("color", new Value("yellow"));

        var builder = EvaluationContext.Builder();

        var result = _jsonEvaluator.ResolveBooleanValueAsync("targetingBoolFlagUsingFlagdPropertyTimestamp", false,
            builder.Build());

        Assert.True(result.Value);
        Assert.Equal("bool1", result.Variant);
        Assert.Equal(Reason.TargetingMatch, result.Reason);
    }

    [Fact]
    public void TestJsonEvaluatorDynamicBoolEvaluationSharedEvaluator()
    {
        _jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

        var builder = EvaluationContext.Builder().Set("email", "test@faas.com");

        var result =
            _jsonEvaluator.ResolveBooleanValueAsync("targetingBoolFlagUsingSharedEvaluator", false, builder.Build());

        Assert.True(result.Value);
        Assert.Equal("bool1", result.Variant);
        Assert.Equal(Reason.TargetingMatch, result.Reason);
    }

    [Fact]
    public void TestJsonEvaluatorDynamicBoolEvaluationSharedEvaluatorReturningBoolType()
    {
        _jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

        var builder = EvaluationContext.Builder().Set("email", "test@faas.com");

        var result =
            _jsonEvaluator.ResolveBooleanValueAsync("targetingBoolFlagUsingSharedEvaluatorReturningBoolType", false,
                builder.Build());

        Assert.True(result.Value);
        Assert.Equal("true", result.Variant);
        Assert.Equal(Reason.TargetingMatch, result.Reason);
    }

    [Fact]
    public void TestJsonEvaluatorDynamicBoolEvaluationWithMissingDefaultVariant()
    {
        _jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

        var builder = EvaluationContext.Builder();

        Assert.Throws<FeatureProviderException>(() =>
            _jsonEvaluator.ResolveBooleanValueAsync("targetingBoolFlagWithMissingDefaultVariant", false,
                builder.Build()));
    }

    [Fact]
    public void TestJsonEvaluatorDynamicBoolEvaluationWithUnexpectedVariantType()
    {
        _jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

        var builder = EvaluationContext.Builder();

        Assert.Throws<FeatureProviderException>(() =>
            _jsonEvaluator.ResolveBooleanValueAsync("targetingBoolFlagWithUnexpectedVariantType", false,
                builder.Build()));
    }

    [Fact]
    public void TestJsonEvaluatorDynamicStringEvaluation()
    {
        _jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

        var attributes = ImmutableDictionary.CreateBuilder<string, Value>();
        attributes.Add("color", new Value("yellow"));

        var builder = EvaluationContext.Builder();
        builder
            .Set("color", "yellow");

        var result = _jsonEvaluator.ResolveStringValueAsync("targetingStringFlag", "", builder.Build());

        Assert.Equal("my-string", result.Value);
        Assert.Equal("str1", result.Variant);
        Assert.Equal(Reason.TargetingMatch, result.Reason);
    }

    [Fact]
    public void TestJsonEvaluatorDynamicFloatEvaluation()
    {
        _jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

        var attributes = ImmutableDictionary.CreateBuilder<string, Value>();
        attributes.Add("color", new Value("yellow"));

        var builder = EvaluationContext.Builder();
        builder
            .Set("color", "yellow");

        var result = _jsonEvaluator.ResolveDoubleValueAsync("targetingFloatFlag", 0, builder.Build());

        Assert.Equal(100, result.Value);
        Assert.Equal("number1", result.Variant);
        Assert.Equal(Reason.TargetingMatch, result.Reason);
    }

    [Fact]
    public void TestJsonEvaluatorDynamicIntEvaluation()
    {
        _jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

        var attributes = ImmutableDictionary.CreateBuilder<string, Value>();
        attributes.Add("color", new Value("yellow"));

        var builder = EvaluationContext.Builder();
        builder
            .Set("color", "yellow");

        var result = _jsonEvaluator.ResolveIntegerValueAsync("targetingNumberFlag", 0, builder.Build());

        Assert.Equal(100, result.Value);
        Assert.Equal("number1", result.Variant);
        Assert.Equal(Reason.TargetingMatch, result.Reason);
    }

    [Fact]
    public void TestJsonEvaluatorDynamicObjectEvaluation()
    {
        _jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

        var attributes = ImmutableDictionary.CreateBuilder<string, Value>();
        attributes.Add("color", new Value("yellow"));

        var builder = EvaluationContext.Builder();
        builder
            .Set("color", "yellow");

        var result = _jsonEvaluator.ResolveStructureValueAsync("targetingObjectFlag", null, builder.Build());

        Assert.True(result.Value.AsStructure.AsDictionary()["key"].AsBoolean);
        Assert.Equal("object1", result.Variant);
        Assert.Equal(Reason.TargetingMatch, result.Reason);
    }

    [Fact]
    public void TestJsonEvaluatorDisabledBoolEvaluation()
    {
        _jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

        var attributes = ImmutableDictionary.CreateBuilder<string, Value>();
        attributes.Add("color", new Value("yellow"));

        var builder = EvaluationContext.Builder();
        builder
            .Set("color", "yellow");

        Assert.Throws<FeatureProviderException>(() =>
            _jsonEvaluator.ResolveBooleanValueAsync("disabledFlag", false, builder.Build()));
    }

    [Fact]
    public void TestJsonEvaluatorFlagNotFoundEvaluation()
    {
        _jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

        var attributes = ImmutableDictionary.CreateBuilder<string, Value>();
        attributes.Add("color", new Value("yellow"));

        var builder = EvaluationContext.Builder();
        builder
            .Set("color", "yellow");

        Assert.Throws<FeatureProviderException>(() =>
            _jsonEvaluator.ResolveBooleanValueAsync("missingFlag", false, builder.Build()));
    }

    [Fact]
    public void TestJsonEvaluatorWrongTypeEvaluation()
    {
        _jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

        var attributes = ImmutableDictionary.CreateBuilder<string, Value>();
        attributes.Add("color", new Value("yellow"));

        var builder = EvaluationContext.Builder();
        builder
            .Set("color", "yellow");

        Assert.Throws<FeatureProviderException>(() =>
            _jsonEvaluator.ResolveBooleanValueAsync("staticStringFlag", false, builder.Build()));
    }

    [Fact]
    public void TestJsonEvaluatorReturnsFlagMetadata()
    {
        _jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

        var result = _jsonEvaluator.ResolveBooleanValueAsync("metadata-flag", false);

        Assert.NotNull(result.FlagMetadata);
        Assert.Equal("1.0.2", result.FlagMetadata.GetString("string"));
        Assert.Equal(2, result.FlagMetadata.GetDouble("integer"));
        Assert.Equal(true, result.FlagMetadata.GetBool("boolean"));
        Assert.Equal(.1, result.FlagMetadata.GetDouble("float"));
    }

    [Fact]
    public void TestJsonEvaluatorAddsFlagSetMetadataToFlagWithoutMetadata()
    {
        _jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.metadataFlags);

        var result = _jsonEvaluator.ResolveBooleanValueAsync("without-metadata-flag", false);

        Assert.NotNull(result.FlagMetadata);
        Assert.Equal("1.0.3", result.FlagMetadata.GetString("string"));
        Assert.Equal(3, result.FlagMetadata.GetDouble("integer"));
        Assert.Equal(false, result.FlagMetadata.GetBool("boolean"));
        Assert.Equal(.2, result.FlagMetadata.GetDouble("float"));
    }

    [Fact]
    public void TestJsonEvaluatorFlagMetadataOverwritesFlagSetMetadata()
    {
        _jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.metadataFlags);

        var result = _jsonEvaluator.ResolveBooleanValueAsync("metadata-flag", false);

        Assert.NotNull(result.FlagMetadata);
        Assert.Equal("1.0.2", result.FlagMetadata.GetString("string"));
        Assert.Equal(2, result.FlagMetadata.GetDouble("integer"));
        Assert.Equal(true, result.FlagMetadata.GetBool("boolean"));
        Assert.Equal(.1, result.FlagMetadata.GetDouble("float"));
    }

    [Fact]
    public void TestJsonEvaluatorThrowsOnInvalidFlagSetMetadata()
    {
        Assert.Throws<ParseErrorException>(() => _jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.invalidFlagSetMetadata));
    }

    [Fact]
    public void TestJsonEvaluatorThrowsOnInvalidFlagMetadata()
    {
        Assert.Throws<ParseErrorException>(() => _jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.invalidFlagMetadata));
    }
}
