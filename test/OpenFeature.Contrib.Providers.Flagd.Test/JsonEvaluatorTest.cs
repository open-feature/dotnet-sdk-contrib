using System;
using System.Collections.Immutable;
using AutoFixture;
using OpenFeature.Constant;
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

            var result = jsonEvaluator.ResolveBooleanValue("validFlag", false);

            Assert.True(result.Value);

        }

        [Fact]
        public void TestJsonEvaluatorAddStaticStringEvaluation()
        {
            var fixture = new Fixture();

            var jsonEvaluator = new JsonEvaluator(fixture.Create<string>());

            jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, Utils.flags);

            var result = jsonEvaluator.ResolveStringValue("staticStringFlag", "");

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

            var result = jsonEvaluator.ResolveBooleanValue("targetingBoolFlag", false, builder.Build());

            Assert.True(result.Value);
            Assert.Equal("bool1", result.Variant);
            Assert.Equal(Reason.TargetingMatch, result.Reason);
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

            var result = jsonEvaluator.ResolveStringValue("targetingStringFlag", "", builder.Build());

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

            var result = jsonEvaluator.ResolveDoubleValue("targetingFloatFlag", 0, builder.Build());

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

            var result = jsonEvaluator.ResolveIntegerValue("targetingNumberFlag", 0, builder.Build());

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

            var result = jsonEvaluator.ResolveStructureValue("targetingObjectFlag", null, builder.Build());

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

            Assert.Throws<FlagNotFoundException>(() => jsonEvaluator.ResolveBooleanValue("disabledFlag", false, builder.Build()));
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

            Assert.Throws<FlagNotFoundException>(() => jsonEvaluator.ResolveBooleanValue("missingFlag", false, builder.Build()));
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

            Assert.Throws<TypeMismatchException>(() => jsonEvaluator.ResolveBooleanValue("staticStringFlag", false, builder.Build()));
        }
    }
}
