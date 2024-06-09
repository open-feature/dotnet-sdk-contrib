using AutoFixture;
using Flipt.Evaluation;
using FluentAssertions;
using OpenFeature.Constant;
using OpenFeature.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flipt.Test
{
    public class FliptConverterTest
    {
        private readonly IFixture _fixture = new Fixture();

        public static IEnumerable<object[]> ReasonTestData =>
            [
                [EvaluationReason.UnknownEvaluationReason, Reason.Unknown],
                [EvaluationReason.FlagDisabledEvaluationReason, Reason.Disabled],
                [EvaluationReason.MatchEvaluationReason, Reason.TargetingMatch],
                [EvaluationReason.DefaultEvaluationReason, Reason.Default]
            ];

        public static IEnumerable<object[]> EmptyContextData =>
            [
                [null],
                [EvaluationContext.Empty]
            ];

        [Theory]
        [MemberData(nameof(ReasonTestData))]
        public void ConvertReason_ShouldReturnExpectedValue(EvaluationReason fliptReason, string expected)
        {
            // Act
            var result = FliptConverter.ConvertReason(fliptReason);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(EmptyContextData))]
        public void CreateRequest_EmptyContext_ShouldCreateRequestWithoutContext(EvaluationContext context)
        {
            // Assert
            var flagKey = _fixture.Create<string>();
            var config = _fixture.Create<FliptProviderConfiguration>();

            // Act
            var result = FliptConverter.CreateRequest(flagKey, context, config);

            // Assert
            result.Context.Should().BeEmpty();
            result.NamespaceKey.Should().Be(config.Namespace);
            result.FlagKey.Should().Be(flagKey);
        }

        [Fact]
        public void CreateRequest_ContextHasStructureValue_ShouldIgnoreStructureValue()
        {
            // Arrange
            var flagKey = _fixture.Create<string>();
            var valueKey = _fixture.Create<string>();
            var config = _fixture.Create<FliptProviderConfiguration>();
            var structureValue = Structure
                .Builder()
                .Set(_fixture.Create<string>(), _fixture.Create<string>())
                .Build();
            var context = EvaluationContext
                .Builder()
                .Set(valueKey, structureValue)
                .Build();

            // Act
            var result = FliptConverter.CreateRequest(flagKey, context, config);

            // Assert
            result.Context.Should().NotContain(v => v.Key == valueKey);
        }

        [Fact]
        public void CreateRequest_ContextHasEmptyValue_ShouldIgnoreEmptyValue()
        {
            // Arrange
            var flagKey = _fixture.Create<string>();
            var valueKey = _fixture.Create<string>();
            var config = _fixture.Create<FliptProviderConfiguration>();
            var emptyValue = new Value();
            var context = EvaluationContext
                .Builder()
                .Set(valueKey, emptyValue)
                .Build();

            // Act
            var result = FliptConverter.CreateRequest(flagKey, context, config);

            // Assert
            result.Context.Should().NotContain(v => v.Key == valueKey);
        }

        [Fact]
        public void CreateRequest_ContextHasListValue_ShouldIgnoreListValue()
        {
            // Arrange
            var flagKey = _fixture.Create<string>();
            var valueKey = _fixture.Create<string>();
            var config = _fixture.Create<FliptProviderConfiguration>();
            var listValue = new Value(_fixture
                .CreateMany<string>()
                .Select(v => new Value(v))
                .ToList());
            var context = EvaluationContext
                .Builder()
                .Set(valueKey, listValue)
                .Build();

            // Act
            var result = FliptConverter.CreateRequest(flagKey, context, config);

            // Assert
            result.Context.Should().NotContain(v => v.Key == valueKey);
        }

        [Fact]
        public void CreateRequest_HasNonStringTargetingKeyValue_ShouldNotSetEntityId()
        {
            // Arrange
            var flagKey = _fixture.Create<string>();
            var config = _fixture.Create<FliptProviderConfiguration>();
            var entityId = _fixture.Create<int>();
            var context = EvaluationContext
                .Builder()
                .Set(config.TargetingKey, entityId)
                .Build();

            // Act
            var result = FliptConverter.CreateRequest(flagKey, context, config);

            // Assert
            result.EntityId.Should().BeEmpty();
        }

        [Fact]
        public void CreateRequest_HasStringTargetingKeyValue_ShouldSetEntityId()
        {
            // Arrange
            var flagKey = _fixture.Create<string>();
            var config = _fixture.Create<FliptProviderConfiguration>();
            var entityId = _fixture.Create<string>();
            var context = EvaluationContext
                .Builder()
                .Set(config.TargetingKey, entityId)
                .Build();

            // Act
            var result = FliptConverter.CreateRequest(flagKey, context, config);

            // Assert
            result.EntityId.Should().Be(entityId);
        }

        [Fact]
        public void CreateRequest_HasNonStringRequestIdValue_ShouldNotSetRequestId()
        {
            // Arrange
            var flagKey = _fixture.Create<string>();
            var config = _fixture.Create<FliptProviderConfiguration>();
            var requestId = _fixture.Create<int>();
            var context = EvaluationContext
                .Builder()
                .Set(config.RequestIdKey, requestId)
                .Build();

            // Act
            var result = FliptConverter.CreateRequest(flagKey, context, config);

            // Assert
            result.RequestId.Should().BeEmpty();
        }

        [Fact]
        public void CreateRequest_HasStringRequestIdValue_ShouldSetRequestId()
        {
            // Arrange
            var flagKey = _fixture.Create<string>();
            var config = _fixture.Create<FliptProviderConfiguration>();
            var requestId = _fixture.Create<string>();
            var context = EvaluationContext
                .Builder()
                .Set(config.RequestIdKey, requestId)
                .Build();

            // Act
            var result = FliptConverter.CreateRequest(flagKey, context, config);

            // Assert
            result.RequestId.Should().Be(requestId);
        }

        [Fact]
        public void CreateRequest_StringValue_ShouldIncludeInRequestContnext()
        {
            // Arrange
            var flagKey = _fixture.Create<string>();
            var config = _fixture.Create<FliptProviderConfiguration>();
            var valueKey = _fixture.Create<string>();
            var stringValue = _fixture.Create<string>();
            var context = EvaluationContext
                .Builder()
                .Set(valueKey, stringValue)
                .Build();

            // Act
            var result = FliptConverter.CreateRequest(flagKey, context, config);

            // Assert
            result.Context.Should().Contain(v => v.Key == valueKey && v.Value == stringValue);
        }

        [Fact]
        public void CreateRequest_BooleanValue_ShouldIncludeInRequestContnext()
        {
            // Arrange
            var flagKey = _fixture.Create<string>();
            var config = _fixture.Create<FliptProviderConfiguration>();
            var key = _fixture.Create<string>();
            var value = _fixture.Create<bool>();
            var context = EvaluationContext
                .Builder()
                .Set(key, value)
                .Build();

            // Act
            var result = FliptConverter.CreateRequest(flagKey, context, config);

            // Assert
            result.Context.Should().Contain(v => v.Key == key && v.Value == value.ToString());
        }

        [Fact]
        public void CreateRequest_NumberValue_ShouldIncludeInRequestContnext()
        {
            // Arrange
            var flagKey = _fixture.Create<string>();
            var config = _fixture.Create<FliptProviderConfiguration>();
            var key = _fixture.Create<string>();
            var value = _fixture.Create<double>();
            var context = EvaluationContext
                .Builder()
                .Set(key, value)
                .Build();

            // Act
            var result = FliptConverter.CreateRequest(flagKey, context, config);

            // Assert
            result.Context.Should().Contain(v => v.Key == key && v.Value == value.ToString());
        }

        [Fact]
        public void CreateRequest_DateTimeValue_ShouldIncludeInRequestContnext()
        {
            // Arrange
            var flagKey = _fixture.Create<string>();
            var config = _fixture.Create<FliptProviderConfiguration>();
            var key = _fixture.Create<string>();
            var value = _fixture.Create<DateTime>();
            var context = EvaluationContext
                .Builder()
                .Set(key, value)
                .Build();

            // Act
            var result = FliptConverter.CreateRequest(flagKey, context, config);

            // Assert
            result.Context.Should().Contain(v => v.Key == key && v.Value == value.ToString("o"));
        }

        [Fact]
        public void CreateRequest_HasRequestIdAndHasActivity_ShouldIgnoreActivity()
        {
            // Arrange
            var activity = new Activity(_fixture.Create<string>());
            activity.Start();
            var flagKey = _fixture.Create<string>();
            var config = _fixture.Create<FliptProviderConfiguration>();
            var requestId = _fixture.Create<string>();
            var context = EvaluationContext
                .Builder()
                .Set(config.RequestIdKey, requestId)
                .Build();

            // Act
            var result = FliptConverter.CreateRequest(flagKey, context, config);

            // Assert
            result.RequestId.Should().Be(requestId);
        }

        [Fact]
        public void CreateRequest_HasNotRequestIdAndHasActivity_ShouldSetRequestIdAsActivityId()
        {
            // Arrange
            var activity = new Activity(_fixture.Create<string>());
            activity.Start();
            var flagKey = _fixture.Create<string>();
            var config = _fixture.Create<FliptProviderConfiguration>();
            var context = EvaluationContext
                .Builder()
                .Set(_fixture.Create<string>(), _fixture.Create<string>())
                .Build();

            // Act
            var result = FliptConverter.CreateRequest(flagKey, context, config);

            // Assert
            result.RequestId.Should().Be(activity.Id);
        }
    }
}
