using System.Collections.Generic;
using JsonLogic.Net;
using Newtonsoft.Json.Linq;
using OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess.CustomEvaluators;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.Test
{
    public class StringEvaluatorTest
    {

        [Fact]
        public void StartsWith()
        {
            // Arrange
            var evaluator = new JsonLogicEvaluator(EvaluateOperators.Default);
            var stringEvaluator = new StringEvaluator();
            EvaluateOperators.Default.AddOperator("starts_with", stringEvaluator.StartsWith);

            var targetingString = @"{""starts_with"": [
              {
                ""var"": [
                  ""color""
                ]
              },
              ""yellow""
            ]}";

            // Parse json into hierarchical structure
            var rule = JObject.Parse(targetingString);

            var data = new Dictionary<string, string> { { "color", "yellowcolor" } };

            // Act & Assert
            var result = evaluator.Apply(rule, data);
            Assert.True(result.IsTruthy());

            data.Clear();
            data.Add("color", "blue");

            result = evaluator.Apply(rule, data);
            Assert.False(result.IsTruthy());
        }

        [Fact]
        public void EndsWith()
        {
            // Arrange
            var evaluator = new JsonLogicEvaluator(EvaluateOperators.Default);
            var stringEvaluator = new StringEvaluator();
            EvaluateOperators.Default.AddOperator("ends_with", stringEvaluator.EndsWith);

            var targetingString = @"{""ends_with"": [
                        {
                          ""var"": [
                            ""color""
                          ]
                        },
                        ""purple""
                      ]}";

            // Parse json into hierarchical structure
            var rule = JObject.Parse(targetingString);

            var data = new Dictionary<string, string> { { "color", "deep-purple" } };

            // Act & Assert
            var result = evaluator.Apply(rule, data);
            Assert.True(result.IsTruthy());

            data.Clear();
            data.Add("color", "purple-nightmare");

            result = evaluator.Apply(rule, data);
            Assert.False(result.IsTruthy());
        }

        [Fact]
        public void NonStringTypeInRule()
        {
            // Arrange
            var evaluator = new JsonLogicEvaluator(EvaluateOperators.Default);
            var stringEvaluator = new StringEvaluator();
            EvaluateOperators.Default.AddOperator("ends_with", stringEvaluator.EndsWith);

            var targetingString = @"{""ends_with"": [
                        {
                          ""var"": [
                            ""color""
                          ]
                        },
                        1
                      ]}";

            // Parse json into hierarchical structure
            var rule = JObject.Parse(targetingString);

            var data = new Dictionary<string, string> { { "color", "deep-purple" } };

            // Act & Assert
            var result = evaluator.Apply(rule, data);
            Assert.False(result.IsTruthy());
        }

        [Fact]
        public void NonStringTypeInData()
        {
            // Arrange
            var evaluator = new JsonLogicEvaluator(EvaluateOperators.Default);
            var stringEvaluator = new StringEvaluator();
            EvaluateOperators.Default.AddOperator("ends_with", stringEvaluator.EndsWith);

            var targetingString = @"{""ends_with"": [
                        {
                          ""var"": [
                            ""color""
                          ]
                        },
                        ""green""
                      ]}";

            // Parse json into hierarchical structure
            var rule = JObject.Parse(targetingString);

            var data = new Dictionary<string, int> { { "color", 5 } };

            // Act & Assert
            var result = evaluator.Apply(rule, data);
            Assert.False(result.IsTruthy());
        }

        [Fact]
        public void EndsWithNotEnoughArguments()
        {
            // Arrange
            var evaluator = new JsonLogicEvaluator(EvaluateOperators.Default);
            var stringEvaluator = new StringEvaluator();
            EvaluateOperators.Default.AddOperator("ends_with", stringEvaluator.EndsWith);

            var targetingString = @"{""ends_with"": [
                        {
                          ""var"": [
                            ""color""
                          ]
                        }
                      ]}";

            // Parse json into hierarchical structure
            var rule = JObject.Parse(targetingString);

            var data = new Dictionary<string, int> { { "color", 5 } };

            // Act & Assert
            var result = evaluator.Apply(rule, data);
            Assert.False(result.IsTruthy());
        }

        [Fact]
        public void StartsWithNotEnoughArguments()
        {
            // Arrange
            var evaluator = new JsonLogicEvaluator(EvaluateOperators.Default);
            var stringEvaluator = new StringEvaluator();
            EvaluateOperators.Default.AddOperator("starts_with", stringEvaluator.EndsWith);

            var targetingString = @"{""starts_with"": [
                        {
                          ""var"": [
                            ""color""
                          ]
                        }
                      ]}";

            // Parse json into hierarchical structure
            var rule = JObject.Parse(targetingString);

            var data = new Dictionary<string, int> { { "color", 5 } };

            // Act & Assert
            var result = evaluator.Apply(rule, data);
            Assert.False(result.IsTruthy());
        }
    }
}