using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Logic;
using OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess.CustomEvaluators;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.Test;

public class StringEvaluatorTest
{
    [Fact]
    public void StartsWith()
    {
        // Arrange
        RuleRegistry.AddRule("starts_with", new StartsWithRule());

        var targetingString = @"{""starts_with"": [
              { ""var"": ""color"" },
              ""yellow""
            ]}";

        var rule = JsonNode.Parse(targetingString);

        var data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, string>
        {
            { "color", "yellowcolor" }
        }));

        // Act & Assert
        var result = JsonLogic.Apply(rule, data);
        Assert.True(result.GetValue<bool>());

        data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, string>
        {
            { "color", "blue" }
        }));

        result = JsonLogic.Apply(rule, data);
        Assert.False(result.GetValue<bool>());
    }

    [Fact]
    public void EndsWith()
    {
        // Arrange
        RuleRegistry.AddRule("ends_with", new EndsWithRule());

        var targetingString = @"{""ends_with"": [
              { ""var"": ""color"" },
              ""purple""
            ]}";

        var rule = JsonNode.Parse(targetingString);

        var data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, string>
        {
            { "color", "deep-purple" }
        }));

        // Act & Assert
        var result = JsonLogic.Apply(rule, data);
        Assert.True(result.GetValue<bool>());

        data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, string>
        {
            { "color", "purple-nightmare" }
        }));

        result = JsonLogic.Apply(rule, data);
        Assert.False(result.GetValue<bool>());
    }

    [Fact]
    public void NonStringTypeInRule()
    {
        // Arrange
        RuleRegistry.AddRule("ends_with", new EndsWithRule());

        var targetingString = @"{""ends_with"": [
              { ""var"": ""color"" },
              1
            ]}";

        var rule = JsonNode.Parse(targetingString);

        var data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, string>
        {
            { "color", "deep-purple" }
        }));

        // Act & Assert
        var result = JsonLogic.Apply(rule, data);
        Assert.False(result.GetValue<bool>());
    }

    [Fact]
    public void NonStringTypeInData()
    {
        // Arrange
        RuleRegistry.AddRule("ends_with", new EndsWithRule());

        var targetingString = @"{""ends_with"": [
              { ""var"": ""color"" },
              ""green""
            ]}";

        var rule = JsonNode.Parse(targetingString);

        var data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "color", 5 }
        }));

        // Act & Assert
        var result = JsonLogic.Apply(rule, data);
        Assert.False(result.GetValue<bool>());
    }

    [Fact]
    public void EndsWithNotEnoughArguments()
    {
        // Arrange
        RuleRegistry.AddRule("ends_with", new EndsWithRule());

        var targetingString = @"{""ends_with"": [
              { ""var"": ""color"" }
            ]}";

        var rule = JsonNode.Parse(targetingString);

        var data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "color", 5 }
        }));

        // Act & Assert
        var result = JsonLogic.Apply(rule, data);
        Assert.False(result.GetValue<bool>());
    }

    [Fact]
    public void StartsWithNotEnoughArguments()
    {
        // Arrange
        RuleRegistry.AddRule("starts_with", new StartsWithRule());

        var targetingString = @"{""starts_with"": [
              { ""var"": ""color"" }
            ]}";

        var rule = JsonNode.Parse(targetingString);

        var data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "color", 5 }
        }));

        // Act & Assert
        var result = JsonLogic.Apply(rule, data);
        Assert.False(result.GetValue<bool>());
    }
}
