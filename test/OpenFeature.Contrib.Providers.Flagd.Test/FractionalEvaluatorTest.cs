using System.Collections.Generic;
using JsonLogic.Net;
using Newtonsoft.Json.Linq;
using OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess.CustomEvaluators;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.Test
{
    public class FractionalEvaluatorTest
    {

        [Fact]
        public void Evaluate()
        {
            // Arrange
            var evaluator = new JsonLogicEvaluator(EvaluateOperators.Default);
            var fractionalEvaluator = new FractionalEvaluator();
            EvaluateOperators.Default.AddOperator("fractional", fractionalEvaluator.Evaluate);

            var targetingString = @"{""fractional"": [
              {
                ""var"": [
                  ""email""
                ]
              },
              [""red"", 25], [""blue"", 25], [""green"", 25], [""yellow"", 25], 
            ]}";

            // Parse json into hierarchical structure
            var rule = JObject.Parse(targetingString);

            var data = new Dictionary<string, string> { { "email", "rachel@faas.com" } };

            // Act & Assert
            var result = evaluator.Apply(rule, data);
            Assert.Equal("yellow", result.ToString());

        }
    }
}