using System;
using System.Collections.Immutable;
using AutoFixture;
using OpenFeature.Constant;
using OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess;
using OpenFeature.Error;
using OpenFeature.Model;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.Test
{
    public class UnitTestJsonEvaluator
    {
        [Fact]
        public void TestJsonEvaluatorAddFlagConfig()
        {
            var fixture = new Fixture();

            var jsonEvaluator = new JsonEvaluator(fixture.Create<string>());

            jsonEvaluator.Sync(FlagConfigurationUpdateType.ADD, Utils.validFlagConfig);

            var result = jsonEvaluator.ResolveBooleanValueAsync("validFlag", false);

            Assert.True(result.Value);
        }

        [Fact]
        public void TestJsonEvaluatorAddStaticStringEvaluation()
        {
            var fixture = new Fixture();

            var jsonEvaluator = new JsonEvaluator(fixture.Create<string>());

            jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

            var result = jsonEvaluator.ResolveStringValueAsync("staticStringFlag", "");

            Assert.Equal("#CC0000", result.Value);
            Assert.Equal("red", result.Variant);
            Assert.Equal(Reason.Static, result.Reason);
        }

        [Fact]
        public void TestJsonEvaluatorDynamicBoolEvaluation()
        {
            var fixture = new Fixture();

            var jsonEvaluator = new JsonEvaluator(fixture.Create<string>());

            jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

            var attributes = ImmutableDictionary.CreateBuilder<string, Value>();
            attributes.Add("color", new Value("yellow"));

            var builder = EvaluationContext.Builder();
            builder
                .Set("color", "yellow");

            var result = jsonEvaluator.ResolveBooleanValueAsync("targetingBoolFlag", false, builder.Build());

            Assert.True(result.Value);
            Assert.Equal("bool1", result.Variant);
            Assert.Equal(Reason.TargetingMatch, result.Reason);
        }

        [Fact]
        public void TestJsonEvaluatorDynamicBoolEvaluationUsingFlagdPropertyFlagKey()
        {
            var fixture = new Fixture();

            var jsonEvaluator = new JsonEvaluator(fixture.Create<string>());

            jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

            var attributes = ImmutableDictionary.CreateBuilder<string, Value>();
            attributes.Add("color", new Value("yellow"));

            var builder = EvaluationContext.Builder();

            var result =
                jsonEvaluator.ResolveBooleanValueAsync("targetingBoolFlagUsingFlagdProperty", false, builder.Build());

            Assert.True(result.Value);
            Assert.Equal("bool1", result.Variant);
            Assert.Equal(Reason.TargetingMatch, result.Reason);
        }

        [Fact]
        public void TestJsonEvaluatorDynamicBoolEvaluationUsingFlagdPropertyTimestamp()
        {
            var fixture = new Fixture();

            var jsonEvaluator = new JsonEvaluator(fixture.Create<string>());

            jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

            var attributes = ImmutableDictionary.CreateBuilder<string, Value>();
            attributes.Add("color", new Value("yellow"));

            var builder = EvaluationContext.Builder();

            var result = jsonEvaluator.ResolveBooleanValueAsync("targetingBoolFlagUsingFlagdPropertyTimestamp", false,
                builder.Build());

            Assert.True(result.Value);
            Assert.Equal("bool1", result.Variant);
            Assert.Equal(Reason.TargetingMatch, result.Reason);
        }

        [Fact]
        public void TestJsonEvaluatorDynamicBoolEvaluationSharedEvaluator()
        {
            var fixture = new Fixture();

            var jsonEvaluator = new JsonEvaluator(fixture.Create<string>());

            jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

            var builder = EvaluationContext.Builder().Set("email", "test@faas.com");

            var result =
                jsonEvaluator.ResolveBooleanValueAsync("targetingBoolFlagUsingSharedEvaluator", false, builder.Build());

            Assert.True(result.Value);
            Assert.Equal("bool1", result.Variant);
            Assert.Equal(Reason.TargetingMatch, result.Reason);
        }

        [Fact]
        public void TestJsonEvaluatorDynamicBoolEvaluationSharedEvaluatorReturningBoolType()
        {
            var fixture = new Fixture();

            var jsonEvaluator = new JsonEvaluator(fixture.Create<string>());

            jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

            var builder = EvaluationContext.Builder().Set("email", "test@faas.com");

            var result =
                jsonEvaluator.ResolveBooleanValueAsync("targetingBoolFlagUsingSharedEvaluatorReturningBoolType", false,
                    builder.Build());

            Assert.True(result.Value);
            Assert.Equal("true", result.Variant);
            Assert.Equal(Reason.TargetingMatch, result.Reason);
        }

        [Fact]
        public void TestJsonEvaluatorDynamicBoolEvaluationWithMissingDefaultVariant()
        {
            var fixture = new Fixture();

            var jsonEvaluator = new JsonEvaluator(fixture.Create<string>());

            jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

            var builder = EvaluationContext.Builder();

            Assert.Throws<FeatureProviderException>(() =>
                jsonEvaluator.ResolveBooleanValueAsync("targetingBoolFlagWithMissingDefaultVariant", false,
                    builder.Build()));
        }

        [Fact]
        public void TestJsonEvaluatorDynamicBoolEvaluationWithUnexpectedVariantType()
        {
            var fixture = new Fixture();

            var jsonEvaluator = new JsonEvaluator(fixture.Create<string>());

            jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

            var builder = EvaluationContext.Builder();

            Assert.Throws<FeatureProviderException>(() =>
                jsonEvaluator.ResolveBooleanValueAsync("targetingBoolFlagWithUnexpectedVariantType", false,
                    builder.Build()));
        }

        [Fact]
        public void TestJsonEvaluatorDynamicStringEvaluation()
        {
            var fixture = new Fixture();

            var jsonEvaluator = new JsonEvaluator(fixture.Create<string>());

            jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

            var attributes = ImmutableDictionary.CreateBuilder<string, Value>();
            attributes.Add("color", new Value("yellow"));

            var builder = EvaluationContext.Builder();
            builder
                .Set("color", "yellow");

            var result = jsonEvaluator.ResolveStringValueAsync("targetingStringFlag", "", builder.Build());

            Assert.Equal("my-string", result.Value);
            Assert.Equal("str1", result.Variant);
            Assert.Equal(Reason.TargetingMatch, result.Reason);
        }

        [Fact]
        public void TestJsonEvaluatorDynamicFloatEvaluation()
        {
            var fixture = new Fixture();

            var jsonEvaluator = new JsonEvaluator(fixture.Create<string>());

            jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

            var attributes = ImmutableDictionary.CreateBuilder<string, Value>();
            attributes.Add("color", new Value("yellow"));

            var builder = EvaluationContext.Builder();
            builder
                .Set("color", "yellow");

            var result = jsonEvaluator.ResolveDoubleValueAsync("targetingFloatFlag", 0, builder.Build());

            Assert.Equal(100, result.Value);
            Assert.Equal("number1", result.Variant);
            Assert.Equal(Reason.TargetingMatch, result.Reason);
        }

        [Fact]
        public void TestJsonEvaluatorDynamicIntEvaluation()
        {
            var fixture = new Fixture();

            var jsonEvaluator = new JsonEvaluator(fixture.Create<string>());

            jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

            var attributes = ImmutableDictionary.CreateBuilder<string, Value>();
            attributes.Add("color", new Value("yellow"));

            var builder = EvaluationContext.Builder();
            builder
                .Set("color", "yellow");

            var result = jsonEvaluator.ResolveIntegerValueAsync("targetingNumberFlag", 0, builder.Build());

            Assert.Equal(100, result.Value);
            Assert.Equal("number1", result.Variant);
            Assert.Equal(Reason.TargetingMatch, result.Reason);
        }

        [Fact]
        public void TestJsonEvaluatorDynamicObjectEvaluation()
        {
            var fixture = new Fixture();

            var jsonEvaluator = new JsonEvaluator(fixture.Create<string>());

            jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

            var attributes = ImmutableDictionary.CreateBuilder<string, Value>();
            attributes.Add("color", new Value("yellow"));

            var builder = EvaluationContext.Builder();
            builder
                .Set("color", "yellow");

            var result = jsonEvaluator.ResolveStructureValueAsync("targetingObjectFlag", null, builder.Build());

            Assert.True(result.Value.AsStructure.AsDictionary()["key"].AsBoolean);
            Assert.Equal("object1", result.Variant);
            Assert.Equal(Reason.TargetingMatch, result.Reason);
        }

        [Fact]
        public void TestJsonEvaluatorDisabledBoolEvaluation()
        {
            var fixture = new Fixture();

            var jsonEvaluator = new JsonEvaluator(fixture.Create<string>());

            jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

            var attributes = ImmutableDictionary.CreateBuilder<string, Value>();
            attributes.Add("color", new Value("yellow"));

            var builder = EvaluationContext.Builder();
            builder
                .Set("color", "yellow");

            Assert.Throws<FeatureProviderException>(() =>
                jsonEvaluator.ResolveBooleanValueAsync("disabledFlag", false, builder.Build()));
        }

        [Fact]
        public void TestJsonEvaluatorFlagNotFoundEvaluation()
        {
            var fixture = new Fixture();

            var jsonEvaluator = new JsonEvaluator(fixture.Create<string>());

            jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

            var attributes = ImmutableDictionary.CreateBuilder<string, Value>();
            attributes.Add("color", new Value("yellow"));

            var builder = EvaluationContext.Builder();
            builder
                .Set("color", "yellow");

            Assert.Throws<FeatureProviderException>(() =>
                jsonEvaluator.ResolveBooleanValueAsync("missingFlag", false, builder.Build()));
        }

        [Fact]
        public void TestJsonEvaluatorWrongTypeEvaluation()
        {
            var fixture = new Fixture();

            var jsonEvaluator = new JsonEvaluator(fixture.Create<string>());

            jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

            var attributes = ImmutableDictionary.CreateBuilder<string, Value>();
            attributes.Add("color", new Value("yellow"));

            var builder = EvaluationContext.Builder();
            builder
                .Set("color", "yellow");

            Assert.Throws<FeatureProviderException>(() =>
                jsonEvaluator.ResolveBooleanValueAsync("staticStringFlag", false, builder.Build()));
        }

        [Fact]
        public void TestJsonEvaluatorReturnsFlagMetadata()
        {
            var fixture = new Fixture();

            var jsonEvaluator = new JsonEvaluator(fixture.Create<string>());

            jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

            var result = jsonEvaluator.ResolveBooleanValueAsync("metadata-flag", false);
            Assert.NotNull(result.FlagMetadata);
            Assert.Equal("1.0.2", result.FlagMetadata.GetString("string"));
            Assert.Equal(2, result.FlagMetadata.GetInt("integer"));
            Assert.Equal(true, result.FlagMetadata.GetBool("boolean"));
            Assert.Equal(.1, result.FlagMetadata.GetDouble("float"));
        }

        [Fact]
        public void TestJsonEvaluatorAddsFlagSetMetadataToFlagWithoutMetdata()
        {
            var fixture = new Fixture();

            var jsonEvaluator = new JsonEvaluator(fixture.Create<string>());

            jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.metadataFlags);

            var result = jsonEvaluator.ResolveBooleanValueAsync("without-metadata-flag", false);
            Assert.NotNull(result.FlagMetadata);
            Assert.Equal("1.0.3", result.FlagMetadata.GetString("string"));
            Assert.Equal(3, result.FlagMetadata.GetInt("integer"));
            Assert.Equal(false, result.FlagMetadata.GetBool("boolean"));
            Assert.Equal(.2, result.FlagMetadata.GetDouble("float"));
        }

        [Fact]
        public void TestJsonEvaluatorFlagMetadataOverwritesFlagSetMetadata()
        {
            var fixture = new Fixture();

            var jsonEvaluator = new JsonEvaluator(fixture.Create<string>());

            jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.metadataFlags);

            var result = jsonEvaluator.ResolveBooleanValueAsync("metadata-flag", false);

            Assert.NotNull(result.FlagMetadata);
            Assert.Equal("1.0.2", result.FlagMetadata.GetString("string"));
            Assert.Equal(2, result.FlagMetadata.GetInt("integer"));
            Assert.Equal(true, result.FlagMetadata.GetBool("boolean"));
            Assert.Equal(.1, result.FlagMetadata.GetDouble("float"));
        }

        [Fact]
        public void TestJsonEvaluatorThrowsOnInvalidFlagSetMetadata()
        {
            var fixture = new Fixture();

            var jsonEvaluator = new JsonEvaluator(fixture.Create<string>());

            Assert.Throws<ParseErrorException>(() => jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.invalidFlagSetMetadata));
        }

        [Fact]
        public void TestJsonEvaluatorThrowsOnInvalidFlagMetadata()
        {
            var fixture = new Fixture();

            var jsonEvaluator = new JsonEvaluator(fixture.Create<string>());

            Assert.Throws<ParseErrorException>(() => jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.invalidFlagMetadata));
        }
    }
}
