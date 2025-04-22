using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Logic;
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
            RuleRegistry.AddRule("sem_ver", new SemVerRule());

            var targetingString = @"{""sem_ver"": [
              { ""var"": ""version"" },
              ""="",
              ""1.0.0""
            ]}";

            var rule = JsonNode.Parse(targetingString);

            var data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, string>
            {
                { "version", "1.0.0" }
            }));

            // Act & Assert
            var result = JsonLogic.Apply(rule, data);
            Assert.True(result.GetValue<bool>());

            data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, string>
            {
                { "version", "1.0.1" }
            }));

            result = JsonLogic.Apply(rule, data);
            Assert.False(result.GetValue<bool>());
        }

        [Fact]
        public void EvaluateVersionNotEqual()
        {
            // Arrange
            RuleRegistry.AddRule("sem_ver", new SemVerRule());

            var targetingString = @"{""sem_ver"": [
              { ""var"": ""version"" },
              ""!="",
              ""1.0.0""
            ]}";

            var rule = JsonNode.Parse(targetingString);

            var data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, string>
            {
                { "version", "1.0.0" }
            }));

            // Act & Assert
            var result = JsonLogic.Apply(rule, data);
            Assert.False(result.GetValue<bool>());

            data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, string>
            {
                { "version", "1.0.1" }
            }));

            result = JsonLogic.Apply(rule, data);
            Assert.True(result.GetValue<bool>());
        }

        [Fact]
        public void EvaluateVersionLess()
        {
            // Arrange
            RuleRegistry.AddRule("sem_ver", new SemVerRule());

            var targetingString = @"{""sem_ver"": [
              { ""var"": ""version"" },
              ""<"",
              ""1.0.2""
            ]}";

            var rule = JsonNode.Parse(targetingString);

            var data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, string>
            {
                { "version", "1.0.1" }
            }));

            // Act & Assert
            var result = JsonLogic.Apply(rule, data);
            Assert.True(result.GetValue<bool>());

            data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, string>
            {
                { "version", "1.0.2" }
            }));

            result = JsonLogic.Apply(rule, data);
            Assert.False(result.GetValue<bool>());
        }

        [Fact]
        public void EvaluateVersionLessOrEqual()
        {
            // Arrange
            RuleRegistry.AddRule("sem_ver", new SemVerRule());

            var targetingString = @"{""sem_ver"": [
              { ""var"": ""version"" },
              ""<="",
              ""1.0.2""
            ]}";

            var rule = JsonNode.Parse(targetingString);

            var data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, string>
            {
                { "version", "1.0.1" }
            }));

            // Act & Assert
            var result = JsonLogic.Apply(rule, data);
            Assert.True(result.GetValue<bool>());

            data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, string>
            {
                { "version", "1.0.2" }
            }));

            result = JsonLogic.Apply(rule, data);
            Assert.True(result.GetValue<bool>());

            data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, string>
            {
                { "version", "1.0.3" }
            }));

            result = JsonLogic.Apply(rule, data);
            Assert.False(result.GetValue<bool>());
        }

        [Fact]
        public void EvaluateVersionGreater()
        {
            // Arrange
            RuleRegistry.AddRule("sem_ver", new SemVerRule());

            var targetingString = @"{""sem_ver"": [
              { ""var"": ""version"" },
              "">"",
              ""1.0.2""
            ]}";

            var rule = JsonNode.Parse(targetingString);

            var data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, string>
            {
                { "version", "1.0.3" }
            }));

            // Act & Assert
            var result = JsonLogic.Apply(rule, data);
            Assert.True(result.GetValue<bool>());

            data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, string>
            {
                { "version", "1.0.2" }
            }));

            result = JsonLogic.Apply(rule, data);
            Assert.False(result.GetValue<bool>());
        }

        [Fact]
        public void EvaluateVersionGreaterOrEqual()
        {
            // Arrange
            RuleRegistry.AddRule("sem_ver", new SemVerRule());

            var targetingString = @"{""sem_ver"": [
                      { ""var"": ""version"" },
                      "">="",
                      ""1.0.2""
                    ]}";

            var rule = JsonNode.Parse(targetingString);

            var data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, string>
            {
                { "version", "1.0.2" }
            }));

            // Act & Assert
            var result = JsonLogic.Apply(rule, data);
            Assert.True(result.GetValue<bool>());

            data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, string>
            {
                { "version", "1.0.3" }
            }));

            result = JsonLogic.Apply(rule, data);
            Assert.True(result.GetValue<bool>());

            data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, string>
            {
                { "version", "1.0.1" }
            }));

            result = JsonLogic.Apply(rule, data);
            Assert.False(result.GetValue<bool>());
        }

        [Fact]
        public void EvaluateVersionMatchMajor()
        {
            // Arrange
            RuleRegistry.AddRule("sem_ver", new SemVerRule());

            var targetingString = @"{""sem_ver"": [
              { ""var"": ""version"" },
              ""^"",
              ""1.0.0""
            ]}";

            var rule = JsonNode.Parse(targetingString);

            var data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, string>
            {
                { "version", "1.0.3" }
            }));

            // Act & Assert
            var result = JsonLogic.Apply(rule, data);
            Assert.True(result.GetValue<bool>());

            data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, string>
            {
                { "version", "2.0.0" }
            }));

            result = JsonLogic.Apply(rule, data);
            Assert.False(result.GetValue<bool>());
        }

        [Fact]
        public void EvaluateVersionMatchMinor()
        {
            // Arrange
            RuleRegistry.AddRule("sem_ver", new SemVerRule());

            var targetingString = @"{""sem_ver"": [
              { ""var"": ""version"" },
              ""~"",
              ""1.3.0""
            ]}";

            var rule = JsonNode.Parse(targetingString);

            var data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, string>
            {
                { "version", "1.3.3" }
            }));

            // Act & Assert
            var result = JsonLogic.Apply(rule, data);
            Assert.True(result.GetValue<bool>());

            data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, string>
            {
                { "version", "2.3.0" }
            }));

            result = JsonLogic.Apply(rule, data);
            Assert.False(result.GetValue<bool>());
        }

        [Fact]
        public void EvaluateVersionTooFewArguments()
        {
            // Arrange
            RuleRegistry.AddRule("sem_ver", new SemVerRule());

            var targetingString = @"{""sem_ver"": [
              { ""var"": ""version"" },
              ""~""
            ]}";

            var rule = JsonNode.Parse(targetingString);

            var data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, string>
            {
                { "version", "1.3.3" }
            }));

            // Act & Assert
            var result = JsonLogic.Apply(rule, data);
            Assert.False(result.GetValue<bool>());
        }

        [Fact]
        public void EvaluateVersionNotAValidVersion()
        {
            // Arrange
            RuleRegistry.AddRule("sem_ver", new SemVerRule());

            var targetingString = @"{""sem_ver"": [
              { ""var"": ""version"" },
              ""~"",
              ""test""
            ]}";

            var rule = JsonNode.Parse(targetingString);

            var data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, string>
            {
                { "version", "1.3.3" }
            }));

            // Act & Assert
            var result = JsonLogic.Apply(rule, data);
            Assert.False(result.GetValue<bool>());
        }
    }
}