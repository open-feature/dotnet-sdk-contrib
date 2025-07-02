using System;
using System.Collections.Generic;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using OpenFeature.Providers.GOFeatureFlag.converters;
using OpenFeature.Model;
using Xunit;

namespace OpenFeature.Providers.GOFeatureFlag.Test.converters;

public class SerializationTest
{
    [Fact]
    public void ToStringDictionary_WithEmptyContext_ShouldReturnEmptyDictionary()
    {
        var evaluationContext = EvaluationContext.Builder().Build();
        var want = JObject.Parse("{\"context\":{}}");
        var request = new Dictionary<string, object> { { "context", evaluationContext.AsDictionary() } };
        var got = JObject.Parse(JsonSerializer.Serialize(request, JsonConverterExtensions.DefaultSerializerSettings));
        Assert.True(JToken.DeepEquals(want, got), "unexpected json");
    }

    [Fact]
    public void ToStringDictionary_WithContext_ShouldReturnADictionaryWithValues()
    {
        var evaluationContext = EvaluationContext.Builder()
            .SetTargetingKey("828c9b62-94c4-4ef3-bddc-e024bfa51a67")
            .Set("location", "somewhere")
            .Build();

        var request = new Dictionary<string, object> { { "context", evaluationContext.AsDictionary() } };
        var got = JObject.Parse(JsonSerializer.Serialize(request, JsonConverterExtensions.DefaultSerializerSettings));
        var want = JObject.Parse(
            "{\"context\":{\"location\":\"somewhere\",\"targetingKey\":\"828c9b62-94c4-4ef3-bddc-e024bfa51a67\"}}");
        Assert.True(JToken.DeepEquals(want, got), "unexpected json");
    }

    [Fact]
    public void ToStringDictionary_WithContextAndIntegerValue_ShouldReturnADictionaryWithStringValues()
    {
        var evaluationContext = EvaluationContext.Builder()
            .SetTargetingKey("828c9b62-94c4-4ef3-bddc-e024bfa51a67")
            .Set("age", 23)
            .Build();
        var request = new Dictionary<string, object> { { "context", evaluationContext.AsDictionary() } };
        var got = JObject.Parse(JsonSerializer.Serialize(request, JsonConverterExtensions.DefaultSerializerSettings));
        var want = JObject.Parse(
            "{\"context\":{\"age\":23,\"targetingKey\":\"828c9b62-94c4-4ef3-bddc-e024bfa51a67\"}}");
        Assert.True(JToken.DeepEquals(want, got), "unexpected json");
    }

    [Fact]
    public void ToStringDictionary_WithContextAndValuesOfStrings_ShouldReturnADictionaryWithSerializedStringValues()
    {
        var testStructure = new Structure(new Dictionary<string, Value>
        {
            { "config1", new Value("value1") }, { "config2", new Value("value2") }
        });

        var evaluationContext = EvaluationContext.Builder()
            .SetTargetingKey("828c9b62-94c4-4ef3-bddc-e024bfa51a67")
            .Set("config", testStructure)
            .Build();
        var request = new Dictionary<string, object> { { "context", evaluationContext.AsDictionary() } };
        var got = JObject.Parse(JsonSerializer.Serialize(request, JsonConverterExtensions.DefaultSerializerSettings));
        var want = JObject.Parse(
            "{\"context\":{\"config\":{\"config1\":\"value1\", \"config2\":\"value2\"},\"targetingKey\":\"828c9b62-94c4-4ef3-bddc-e024bfa51a67\"}}");
        Assert.True(JToken.DeepEquals(want, got), "unexpected json");
    }

    [Fact]
    public void ToStringDictionary_WithContextAndMixedValueTypes_ShouldReturnADictionaryWithSerializedValues()
    {
        var dateTime = new DateTime(2025, 9, 1);
        var testStructure = new Structure(new Dictionary<string, Value>
        {
            { "config1", new Value(1) }, { "config2", new Value("value2") }, { "config3", new Value(dateTime) }
        });

        var evaluationContext = EvaluationContext.Builder()
            .SetTargetingKey("828c9b62-94c4-4ef3-bddc-e024bfa51a67")
            .Set("config", testStructure)
            .Build();

        var request = new Dictionary<string, object> { { "context", evaluationContext.AsDictionary() } };
        var got = JObject.Parse(JsonSerializer.Serialize(request, JsonConverterExtensions.DefaultSerializerSettings));
        var want = JObject.Parse(
            "{\"context\":{\"config\":{\"config3\":\"2025-09-01T00:00:00\",\"config2\":\"value2\",\"config1\":1},\"targetingKey\":\"828c9b62-94c4-4ef3-bddc-e024bfa51a67\"}}");
        Assert.True(JToken.DeepEquals(want, got), "unexpected json");
    }

    [Fact]
    public void ToStringDictionary_WithContextWithListAndNestedList_ShouldReturnADictionaryWithSerializedValues()
    {
        var sampleDictionary = new Dictionary<string, Value>();
        sampleDictionary["config2"] = new Value([
            new Value([new Value("element1-1"), new Value("element1-2")]),
            new Value("element2"),
            new Value("element3")
        ]);
        sampleDictionary["config3"] = new Value(new DateTime(2025, 9, 1));

        var testStructure = new Structure(sampleDictionary);

        var evaluationContext = EvaluationContext.Builder()
            .SetTargetingKey("828c9b62-94c4-4ef3-bddc-e024bfa51a67")
            .Set("config", testStructure)
            .Build();

        var request = new Dictionary<string, object> { { "context", evaluationContext.AsDictionary() } };
        var got = JObject.Parse(JsonSerializer.Serialize(request, JsonConverterExtensions.DefaultSerializerSettings));
        var want = JObject.Parse(
            "{\"context\":{\"config\":{\"config2\":[[\"element1-1\",\"element1-2\"],\"element2\",\"element3\"],\"config3\":\"2025-09-01T00:00:00\"},\"targetingKey\":\"828c9b62-94c4-4ef3-bddc-e024bfa51a67\"}}");
        Assert.True(JToken.DeepEquals(want, got), "unexpected json");
    }

    [Fact]
    public void ToStringDictionary_WithContextWithNestedStructure_ShouldReturnADictionaryWithSerializedValues()
    {
        var testStructure = new Structure(new Dictionary<string, Value>
        {
            {
                "config-value-struct",
                new Value(new Structure(new Dictionary<string, Value> { { "nested1", new Value(1) } }))
            },
            { "config-value-value", new Value(new Value(new DateTime(2025, 9, 1))) }
        });

        var evaluationContext = EvaluationContext.Builder()
            .SetTargetingKey("828c9b62-94c4-4ef3-bddc-e024bfa51a67")
            .Set("config", testStructure)
            .Build();
        var request = new Dictionary<string, object> { { "context", evaluationContext.AsDictionary() } };
        var got = JObject.Parse(JsonSerializer.Serialize(request, JsonConverterExtensions.DefaultSerializerSettings));
        var want = JObject.Parse(
            "{\"context\":{\"config\":{\"config-value-struct\":{\"nested1\":1},\"config-value-value\":\"2025-09-01T00:00:00\"},\"targetingKey\":\"828c9b62-94c4-4ef3-bddc-e024bfa51a67\"}}");
        Assert.True(JToken.DeepEquals(want, got), "unexpected json");
    }
}
