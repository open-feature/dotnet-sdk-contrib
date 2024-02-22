using System.Collections.Generic;
using JsonLogic.Net;
using Newtonsoft.Json.Linq;
using OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess.CustomEvaluators;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.Test
{
    public class SemVerEvaluatorTest
    {

        [Fact]
        public void EvaluateVersionEqual()
        {
            // Arrange
            var evaluator = new JsonLogicEvaluator(EvaluateOperators.Default);
            var semVerEvaluator = new SemVerEvaluator();
            EvaluateOperators.Default.AddOperator("sem_ver", semVerEvaluator.Evaluate);

            var targetingString = @"{""sem_ver"": [
              {
                ""var"": [
                  ""version""
                ]
              },
              ""="",
              ""1.0.0""
            ]}";

            // Parse json into hierarchical structure
            var rule = JObject.Parse(targetingString);

            var data = new Dictionary<string, string> { { "version", "1.0.0" } };

            // Act & Assert
            var result = evaluator.Apply(rule, data);
            Assert.True(result.IsTruthy());

            data.Clear();
            data.Add("version", "1.0.1");

            result = evaluator.Apply(rule, data);
            Assert.False(result.IsTruthy());
        }
        
        [Fact]
        public void EvaluateVersionNotEqual()
        {
            // Arrange
            var evaluator = new JsonLogicEvaluator(EvaluateOperators.Default);
            var semVerEvaluator = new SemVerEvaluator();
            EvaluateOperators.Default.AddOperator("sem_ver", semVerEvaluator.Evaluate);

            var targetingString = @"{""sem_ver"": [
              {
                ""var"": [
                  ""version""
                ]
              },
              ""!="",
              ""1.0.0""
            ]}";

            // Parse json into hierarchical structure
            var rule = JObject.Parse(targetingString);

            var data = new Dictionary<string, string> { { "version", "1.0.0" } };

            // Act & Assert
            var result = evaluator.Apply(rule, data);
            Assert.False(result.IsTruthy());

            data.Clear();
            data.Add("version", "1.0.1");

            result = evaluator.Apply(rule, data);
            Assert.True(result.IsTruthy());
        }
        
        [Fact]
        public void EvaluateVersionLess()
        {
            // Arrange
            var evaluator = new JsonLogicEvaluator(EvaluateOperators.Default);
            var semVerEvaluator = new SemVerEvaluator();
            EvaluateOperators.Default.AddOperator("sem_ver", semVerEvaluator.Evaluate);

            var targetingString = @"{""sem_ver"": [
              {
                ""var"": [
                  ""version""
                ]
              },
              ""<"",
              ""1.0.2""
            ]}";

            // Parse json into hierarchical structure
            var rule = JObject.Parse(targetingString);

            var data = new Dictionary<string, string> { { "version", "1.0.1" } };

            // Act & Assert
            var result = evaluator.Apply(rule, data);
            Assert.True(result.IsTruthy());

            data.Clear();
            data.Add("version", "1.0.2");

            result = evaluator.Apply(rule, data);
            Assert.False(result.IsTruthy());
        }
        
        [Fact]
        public void EvaluateVersionLessOrEqual()
        {
            // Arrange
            var evaluator = new JsonLogicEvaluator(EvaluateOperators.Default);
            var semVerEvaluator = new SemVerEvaluator();
            EvaluateOperators.Default.AddOperator("sem_ver", semVerEvaluator.Evaluate);

            var targetingString = @"{""sem_ver"": [
              {
                ""var"": [
                  ""version""
                ]
              },
              ""<="",
              ""1.0.2""
            ]}";

            // Parse json into hierarchical structure
            var rule = JObject.Parse(targetingString);

            var data = new Dictionary<string, string> { { "version", "1.0.1" } };

            // Act & Assert
            var result = evaluator.Apply(rule, data);
            Assert.True(result.IsTruthy());

            data.Clear();
            data.Add("version", "1.0.2");

            result = evaluator.Apply(rule, data);
            Assert.True(result.IsTruthy());
            
            data.Clear();
            data.Add("version", "1.0.3");

            result = evaluator.Apply(rule, data);
            Assert.False(result.IsTruthy());
        }
        
        [Fact]
        public void EvaluateVersionGreater()
        {
            // Arrange
            var evaluator = new JsonLogicEvaluator(EvaluateOperators.Default);
            var semVerEvaluator = new SemVerEvaluator();
            EvaluateOperators.Default.AddOperator("sem_ver", semVerEvaluator.Evaluate);

            var targetingString = @"{""sem_ver"": [
              {
                ""var"": [
                  ""version""
                ]
              },
              "">"",
              ""1.0.2""
            ]}";

            // Parse json into hierarchical structure
            var rule = JObject.Parse(targetingString);

            var data = new Dictionary<string, string> { { "version", "1.0.3" } };

            // Act & Assert
            var result = evaluator.Apply(rule, data);
            Assert.True(result.IsTruthy());

            data.Clear();
            data.Add("version", "1.0.2");

            result = evaluator.Apply(rule, data);
            Assert.False(result.IsTruthy());
        }

        [Fact]
        public void EvaluateVersionGreaterOrEqual()
        {
            // Arrange
            var evaluator = new JsonLogicEvaluator(EvaluateOperators.Default);
            var semVerEvaluator = new SemVerEvaluator();
            EvaluateOperators.Default.AddOperator("sem_ver", semVerEvaluator.Evaluate);
        
            var targetingString = @"{""sem_ver"": [
                      {
                        ""var"": [
                          ""version""
                        ]
                      },
                      "">="",
                      ""1.0.2""
                    ]}";
        
            // Parse json into hierarchical structure
            var rule = JObject.Parse(targetingString);
        
            var data = new Dictionary<string, string> { { "version", "1.0.2" } };
        
            // Act & Assert
            var result = evaluator.Apply(rule, data);
            Assert.True(result.IsTruthy());
        
            data.Clear();
            data.Add("version", "1.0.3");
        
            result = evaluator.Apply(rule, data);
            Assert.True(result.IsTruthy());
        
            data.Clear();
            data.Add("version", "1.0.1");
            
            result = evaluator.Apply(rule, data);
            Assert.False(result.IsTruthy());
        }
        
        [Fact]
        public void EvaluateVersionMatchMajor()
        {
            // Arrange
            var evaluator = new JsonLogicEvaluator(EvaluateOperators.Default);
            var semVerEvaluator = new SemVerEvaluator();
            EvaluateOperators.Default.AddOperator("sem_ver", semVerEvaluator.Evaluate);

            var targetingString = @"{""sem_ver"": [
              {
                ""var"": [
                  ""version""
                ]
              },
              ""^"",
              ""1.0.0""
            ]}";

            // Parse json into hierarchical structure
            var rule = JObject.Parse(targetingString);

            var data = new Dictionary<string, string> { { "version", "1.0.3" } };

            // Act & Assert
            var result = evaluator.Apply(rule, data);
            Assert.True(result.IsTruthy());

            data.Clear();
            data.Add("version", "2.0.0");

            result = evaluator.Apply(rule, data);
            Assert.False(result.IsTruthy());
        }
        
        [Fact]
        public void EvaluateVersionMatchMinor()
        {
            // Arrange
            var evaluator = new JsonLogicEvaluator(EvaluateOperators.Default);
            var semVerEvaluator = new SemVerEvaluator();
            EvaluateOperators.Default.AddOperator("sem_ver", semVerEvaluator.Evaluate);

            var targetingString = @"{""sem_ver"": [
              {
                ""var"": [
                  ""version""
                ]
              },
              ""~"",
              ""1.3.0""
            ]}";

            // Parse json into hierarchical structure
            var rule = JObject.Parse(targetingString);

            var data = new Dictionary<string, string> { { "version", "1.3.3" } };

            // Act & Assert
            var result = evaluator.Apply(rule, data);
            Assert.True(result.IsTruthy());

            data.Clear();
            data.Add("version", "2.3.0");

            result = evaluator.Apply(rule, data);
            Assert.False(result.IsTruthy());
        }
    }
}