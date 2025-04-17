using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Logic;
using Json.More;
using OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess.CustomEvaluators;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.Test
{

    internal class FractionalEvaluationTestData
    {
        public static IEnumerable<object[]> FractionalEvaluationContext()
        {
            yield return new object[] { "rachel@faas.com", "headerColor", "yellow" };
            yield return new object[] { "monica@faas.com", "headerColor", "blue" };
            yield return new object[] { "joey@faas.com", "headerColor", "red" };
            yield return new object[] { "ross@faas.com", "headerColor", "green" };
            yield return new object[] { "ross@faas.com", "footerColor", "red" };
        }

        public static IEnumerable<object[]> FractionalEvaluationWithTargetingKeyContext()
        {
            yield return new object[] { "headerColor", "yellow" };
            yield return new object[] { "footerColor", "yellow" };
        }
    }
    public class FractionalEvaluatorTest
    {
        [Theory]
        [MemberData(nameof(FractionalEvaluationTestData.FractionalEvaluationContext), MemberType = typeof(FractionalEvaluationTestData))]
        public void Evaluate(string email, string flagKey, string expected)
        {
            // Arrange
            RuleRegistry.AddRule("fractional", new FractionalEvaluator());

            var targetingString = @"{""fractional"": [
              {
                ""cat"": [
                    { ""var"":""$flagd.flagKey"" },
                    { ""var"":""email"" }
                ]
              },
              [""red"", 25], [""blue"", 25], [""green"", 25], [""yellow"", 25]
            ]}";

            var rule = JsonNode.Parse(targetingString);

            var data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, object>
            {
                { "email", email },
                { "$flagd", new Dictionary<string, object> { { "flagKey", flagKey } } }
            }));

            // Act
            var result = JsonLogic.Apply(rule, data);

            // Assert
            Assert.Equal(expected, result.ToString());
        }

        [Theory]
        [MemberData(nameof(FractionalEvaluationTestData.FractionalEvaluationContext), MemberType = typeof(FractionalEvaluationTestData))]
        public void EvaluateUsingRelativeWeights(string email, string flagKey, string expected)
        {
            // Arrange
            RuleRegistry.AddRule("fractional", new FractionalEvaluator());

            var targetingString = @"{""fractional"": [
                    {
                        ""cat"": [
                            { ""var"":""$flagd.flagKey"" },
                            { ""var"":""email"" }
                        ]
                    },
                    [""red"", 5], [""blue"", 5], [""green"", 5], [""yellow"", 5]
                ]}";

            var rule = JsonNode.Parse(targetingString);

            var data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, object>
            {
                { "email", email },
                { "$flagd", new Dictionary<string, object> { { "flagKey", flagKey } } }
            }));

            // Act
            var result = JsonLogic.Apply(rule, data);

            // Assert
            Assert.Equal(expected, result.ToString());
        }

        [Theory]
        [MemberData(nameof(FractionalEvaluationTestData.FractionalEvaluationContext), MemberType = typeof(FractionalEvaluationTestData))]
        public void EvaluateUsingDefaultWeights(string email, string flagKey, string expected)
        {
            // Arrange
            RuleRegistry.AddRule("fractional", new FractionalEvaluator());

            var targetingString = @"{""fractional"": [
            {
                ""cat"": [
                    { ""var"":""$flagd.flagKey"" },
                    { ""var"":""email"" }
                ]
            },
            [""red""], [""blue""], [""green""], [""yellow""]
            ]}";

            var rule = JsonNode.Parse(targetingString);

            var data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, object>
            {
                { "email", email },
                { "$flagd", new Dictionary<string, object> { { "flagKey", flagKey } } }
            }));

            // Act
            var result = JsonLogic.Apply(rule, data);

            // Assert
            Assert.Equal(expected, result.ToString());
        }

        [Theory]
        [MemberData(nameof(FractionalEvaluationTestData.FractionalEvaluationWithTargetingKeyContext), MemberType = typeof(FractionalEvaluationTestData))]
        public void EvaluateUsingTargetingKey(string flagKey, string expected)
        {
            // Arrange
            RuleRegistry.AddRule("fractional", new FractionalEvaluator());

            var targetingString = @"{""fractional"": [
              [""red"", 25], [""blue"", 25], [""green"", 25], [""yellow"", 25]
            ]}";

            var rule = JsonNode.Parse(targetingString);

            var data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, object>
            {
                { "targetingKey", "myKey" },
                { "$flagd", new Dictionary<string, object> { { "flagKey", flagKey } } }
            }));

            // Act
            var result = JsonLogic.Apply(rule, data);

            // Assert
            Assert.Equal(expected, result.ToString());
        }
    }
}