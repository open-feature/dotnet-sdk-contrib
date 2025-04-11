using OpenFeature.Contrib.Providers.Flipt.Converters;
using OpenFeature.Model;
using System.Text.Json;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flipt.Test;

public class FliptExtensionsTest
{
    [Fact]
    public void ToStringDictionary_WithEmptyContext_ShouldReturnEmptyDictionary()
    {
        var evaluationContext = EvaluationContext.Builder().Build();
        var result = evaluationContext.ToStringDictionary();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void ToStringDictionary_WithContext_ShouldReturnADictionaryWithValues()
    {
        var evaluationContext = EvaluationContext.Builder()
            .SetTargetingKey(Guid.NewGuid().ToString())
            .Set("location", "somewhere")
            .Build();
        var result = evaluationContext.ToStringDictionary();

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("location", result.Keys);
    }

    [Fact]
    public void ToStringDictionary_WithContextAndIntegerValue_ShouldReturnADictionaryWithStringValues()
    {
        var evaluationContext = EvaluationContext.Builder()
            .SetTargetingKey(Guid.NewGuid().ToString())
            .Set("age", 23)
            .Build();
        var result = evaluationContext.ToStringDictionary();

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("age", result.Keys);

        var actual = result["age"];
        Assert.Equal("23", actual);
    }

    [Fact]
    public void ToStringDictionary_WithContextAndValuesOfStrings_ShouldReturnADictionaryWithSerializedStringValues()
    {
        var testStructure = new Structure(new Dictionary<string, Value>
        {
            { "config1", new Value("value1") },
            { "config2", new Value("value2") }
        });

        var evaluationContext = EvaluationContext.Builder()
            .SetTargetingKey(Guid.NewGuid().ToString())
            .Set("config", testStructure)
            .Build();
        var result = evaluationContext.ToStringDictionary();

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("config", result.Keys);

        var expected = JsonSerializer.Serialize(testStructure, JsonConverterExtensions.DefaultSerializerSettings);
        var actual = result["config"];
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToStringDictionary_WithContextAndMixedValueTypes_ShouldReturnADictionaryWithSerializedValues()
    {
        var testStructure = new Structure(new Dictionary<string, Value>
        {
            { "config1", new Value(1) },
            { "config2", new Value("value2") },
            { "config3", new Value(DateTime.Now) }
        });

        var evaluationContext = EvaluationContext.Builder()
            .SetTargetingKey(Guid.NewGuid().ToString())
            .Set("config", testStructure)
            .Build();
        var result = evaluationContext.ToStringDictionary();

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("config", result.Keys);

        var expected = JsonSerializer.Serialize(testStructure, JsonConverterExtensions.DefaultSerializerSettings);
        var actual = result["config"];
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToStringDictionary_WithContextWithListAndNestedList_ShouldReturnADictionaryWithSerializedValues()
    {
        var sampleDictionary = new Dictionary<string, Value>();
        sampleDictionary["config2"] = new Value([
            new Value([new Value("element1-1"), new Value("element1-2")]), new Value("element2"),
            new Value("element3")
        ]);
        sampleDictionary["config3"] = new Value(DateTime.Now);

        var testStructure = new Structure(sampleDictionary);

        var evaluationContext = EvaluationContext.Builder()
            .SetTargetingKey(Guid.NewGuid().ToString())
            .Set("config", testStructure)
            .Build();
        var result = evaluationContext.ToStringDictionary();

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("config", result.Keys);

        var expected = JsonSerializer.Serialize(testStructure, JsonConverterExtensions.DefaultSerializerSettings);
        var actual = result["config"];
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToStringDictionary_WithContextWithNestedStructure_ShouldReturnADictionaryWithSerializedValues()
    {
        var testStructure = new Structure(new Dictionary<string, Value>
        {
            {
                "config-value-struct", new Value(new Structure(new Dictionary<string, Value>
                {
                    { "nested1", new Value(1) }
                }))
            },
            { "config-value-value", new Value(new Value(DateTime.Now)) }
        });

        var evaluationContext = EvaluationContext.Builder()
            .SetTargetingKey(Guid.NewGuid().ToString())
            .Set("config", testStructure)
            .Build();
        var result = evaluationContext.ToStringDictionary();

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("config", result.Keys);

        var expected = JsonSerializer.Serialize(testStructure, JsonConverterExtensions.DefaultSerializerSettings);
        var actual = result["config"];
        Assert.Equal(expected, actual);
    }
}