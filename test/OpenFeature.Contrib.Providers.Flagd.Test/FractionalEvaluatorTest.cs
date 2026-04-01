using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Logic;
using OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess.CustomEvaluators;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.Test;


internal class FractionalEvaluationTestData
{
    public static IEnumerable<object[]> FractionalEvaluationContext()
    {
        yield return new object[] { "rachel@faas.com", "headerColor", "blue" };
        yield return new object[] { "monica@faas.com", "headerColor", "yellow" };
        yield return new object[] { "joey@faas.com", "headerColor", "red" };
        yield return new object[] { "ross@faas.com", "headerColor", "blue" };
        yield return new object[] { "ross@faas.com", "footerColor", "yellow" };
    }

    public static IEnumerable<object[]> FractionalEvaluationWithTargetingKeyContext()
    {
        yield return new object[] { "headerColor", "blue" };
        yield return new object[] { "footerColor", "green" };
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

    [Fact]
    public void EvaluateSingleBucket()
    {
        // Arrange
        RuleRegistry.AddRule("fractional", new FractionalEvaluator());

        var targetingString = @"{""fractional"": [
              [""only"", 100]
            ]}";

        var rule = JsonNode.Parse(targetingString);

        var data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "targetingKey", "user1" },
            { "$flagd", new Dictionary<string, object> { { "flagKey", "flag1" } } }
        }));

        // Act
        var result = JsonLogic.Apply(rule, data);

        // Assert
        Assert.Equal("only", result.ToString());
    }

    [Fact]
    public void EvaluateBooleanVariant()
    {
        // Arrange
        RuleRegistry.AddRule("fractional", new FractionalEvaluator());

        var targetingString = @"{""fractional"": [
              [true, 100], [false, 0]
            ]}";

        var rule = JsonNode.Parse(targetingString);

        var data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "targetingKey", "user1" },
            { "$flagd", new Dictionary<string, object> { { "flagKey", "flag1" } } }
        }));

        // Act
        var result = JsonLogic.Apply(rule, data);

        // Assert
        Assert.Equal("true", result.ToString());
    }

    [Fact]
    public void EvaluateNumberVariant()
    {
        // Arrange
        RuleRegistry.AddRule("fractional", new FractionalEvaluator());

        var targetingString = @"{""fractional"": [
              [42, 100], [0, 0]
            ]}";

        var rule = JsonNode.Parse(targetingString);

        var data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "targetingKey", "user1" },
            { "$flagd", new Dictionary<string, object> { { "flagKey", "flag1" } } }
        }));

        // Act
        var result = JsonLogic.Apply(rule, data);

        // Assert
        Assert.Equal("42", result.ToString());
    }

    [Fact]
    public void EvaluateNullVariant()
    {
        // Arrange
        RuleRegistry.AddRule("fractional", new FractionalEvaluator());

        var targetingString = @"{""fractional"": [
              [null, 100], [""x"", 0]
            ]}";

        var rule = JsonNode.Parse(targetingString);

        var data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "targetingKey", "user1" },
            { "$flagd", new Dictionary<string, object> { { "flagKey", "flag1" } } }
        }));

        // Act
        var result = JsonLogic.Apply(rule, data);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void EvaluateFloatWeightReturnsNull()
    {
        // Arrange
        RuleRegistry.AddRule("fractional", new FractionalEvaluator());

        var targetingString = @"{""fractional"": [
              [""red"", 25.5], [""blue"", 25]
            ]}";

        var rule = JsonNode.Parse(targetingString);

        var data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "targetingKey", "user1" },
            { "$flagd", new Dictionary<string, object> { { "flagKey", "flag1" } } }
        }));

        // Act
        var result = JsonLogic.Apply(rule, data);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void EvaluateNegativeWeightClampedToZero()
    {
        // Arrange
        RuleRegistry.AddRule("fractional", new FractionalEvaluator());

        // "red" has weight -1000 (clamped to 0), "blue" has weight 1
        var targetingString = @"{""fractional"": [
              [""red"", -1000], [""blue"", 1]
            ]}";

        var rule = JsonNode.Parse(targetingString);

        var data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "targetingKey", "user1" },
            { "$flagd", new Dictionary<string, object> { { "flagKey", "flag1" } } }
        }));

        // Act
        var result = JsonLogic.Apply(rule, data);

        // Assert: "red" weight is 0, so all traffic goes to "blue"
        Assert.Equal("blue", result.ToString());
    }

    [Fact]
    public void EvaluateZeroTotalWeightReturnsNull()
    {
        // Arrange
        RuleRegistry.AddRule("fractional", new FractionalEvaluator());

        var targetingString = @"{""fractional"": [
              [""red"", 0], [""blue"", 0]
            ]}";

        var rule = JsonNode.Parse(targetingString);

        var data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "targetingKey", "user1" },
            { "$flagd", new Dictionary<string, object> { { "flagKey", "flag1" } } }
        }));

        // Act
        var result = JsonLogic.Apply(rule, data);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void EvaluateEmptyArgsReturnsNull()
    {
        // Arrange
        RuleRegistry.AddRule("fractional", new FractionalEvaluator());

        var targetingString = @"{""fractional"": []}";

        var rule = JsonNode.Parse(targetingString);

        var data = JsonNode.Parse(JsonSerializer.Serialize(new Dictionary<string, object>
        {
            { "targetingKey", "user1" },
            { "$flagd", new Dictionary<string, object> { { "flagKey", "flag1" } } }
        }));

        // Act
        var result = JsonLogic.Apply(rule, data);

        // Assert
        Assert.Null(result);
    }
}
