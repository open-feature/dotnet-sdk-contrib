using System.Text.Json;
using OpenFeature.Model;
using OpenFeature.Providers.Ofrep.Extensions;
using Xunit;

namespace OpenFeature.Providers.Ofrep.Test.Extensions;

public class JsonElementExtensionsTest
{
    [Fact]
    public void ToValue_ShouldConvertString()
    {
        // Arrange
        var json = JsonSerializer.Deserialize<JsonElement>("\"hello world\"");

        // Act
        var result = json.ToValue();

        // Assert
        Assert.True(result.IsString);
        Assert.Equal("hello world", result.AsString);
    }

    [Fact]
    public void ToValue_ShouldConvertEmptyString()
    {
        // Arrange
        var json = JsonSerializer.Deserialize<JsonElement>("\"\"");

        // Act
        var result = json.ToValue();

        // Assert
        Assert.True(result.IsString);
        Assert.Equal(string.Empty, result.AsString);
    }

    [Fact]
    public void ToValue_ShouldConvertInteger()
    {
        // Arrange
        var json = JsonSerializer.Deserialize<JsonElement>("42");

        // Act
        var result = json.ToValue();

        // Assert
        Assert.True(result.IsNumber);
        Assert.Equal(42, result.AsInteger);
    }

    [Fact]
    public void ToValue_ShouldConvertDouble()
    {
        // Arrange
        var json = JsonSerializer.Deserialize<JsonElement>("3.14");

        // Act
        var result = json.ToValue();

        // Assert
        Assert.True(result.IsNumber);
        Assert.Equal(3.14, result.AsDouble);
    }

    [Fact]
    public void ToValue_ShouldConvertNegativeNumber()
    {
        // Arrange
        var json = JsonSerializer.Deserialize<JsonElement>("-42.5");

        // Act
        var result = json.ToValue();

        // Assert
        Assert.True(result.IsNumber);
        Assert.Equal(-42.5, result.AsDouble);
    }

    [Fact]
    public void ToValue_ShouldConvertTrue()
    {
        // Arrange
        var json = JsonSerializer.Deserialize<JsonElement>("true");

        // Act
        var result = json.ToValue();

        // Assert
        Assert.True(result.IsBoolean);
        Assert.True(result.AsBoolean);
    }

    [Fact]
    public void ToValue_ShouldConvertFalse()
    {
        // Arrange
        var json = JsonSerializer.Deserialize<JsonElement>("false");

        // Act
        var result = json.ToValue();

        // Assert
        Assert.True(result.IsBoolean);
        Assert.False(result.AsBoolean);
    }

    [Fact]
    public void ToValue_ShouldConvertNull()
    {
        // Arrange
        var json = JsonSerializer.Deserialize<JsonElement>("null");

        // Act
        var result = json.ToValue();

        // Assert
        Assert.True(result.IsNull);
    }

    [Fact]
    public void ToValue_ShouldConvertSimpleObject()
    {
        // Arrange
        var json = JsonSerializer.Deserialize<JsonElement>("{\"property1\": \"value1\", \"property2\": 123, \"property3\": true}");

        // Act
        var result = json.ToValue();

        // Assert
        Assert.True(result.IsStructure);
        var structure = result.AsStructure;
        Assert.NotNull(structure);
        Assert.Equal("value1", structure.GetValue("property1").AsString);
        Assert.Equal(123, structure.GetValue("property2").AsInteger);
        Assert.True(structure.GetValue("property3").AsBoolean);
    }

    [Fact]
    public void ToValue_ShouldConvertNestedObject()
    {
        // Arrange
        var json = JsonSerializer.Deserialize<JsonElement>("{\"outer\": {\"inner\": \"nested value\"}}");

        // Act
        var result = json.ToValue();

        // Assert
        Assert.True(result.IsStructure);
        var outerStructure = result.AsStructure;
        Assert.NotNull(outerStructure);
        var innerValue = outerStructure.GetValue("outer");
        Assert.True(innerValue.IsStructure);
        var innerStructure = innerValue.AsStructure;
        Assert.NotNull(innerStructure);
        Assert.Equal("nested value", innerStructure.GetValue("inner").AsString);
    }

    [Fact]
    public void ToValue_ShouldConvertSimpleArray()
    {
        // Arrange
        var json = JsonSerializer.Deserialize<JsonElement>("[1, 2, 3]");

        // Act
        var result = json.ToValue();

        // Assert
        Assert.True(result.IsList);
        var list = result.AsList;
        Assert.NotNull(list);
        Assert.Equal(3, list.Count);
        Assert.Equal(1, list[0].AsInteger);
        Assert.Equal(2, list[1].AsInteger);
        Assert.Equal(3, list[2].AsInteger);
    }

    [Fact]
    public void ToValue_ShouldConvertMixedArray()
    {
        // Arrange
        var json = JsonSerializer.Deserialize<JsonElement>("[\"hello\", 42, true, null]");

        // Act
        var result = json.ToValue();

        // Assert
        Assert.True(result.IsList);
        var list = result.AsList;
        Assert.NotNull(list);
        Assert.Equal(4, list.Count);
        Assert.Equal("hello", list[0].AsString);
        Assert.Equal(42, list[1].AsInteger);
        Assert.True(list[2].AsBoolean);
        Assert.True(list[3].IsNull);
    }

    [Fact]
    public void ToValue_ShouldConvertArrayOfObjects()
    {
        // Arrange
        var json = JsonSerializer.Deserialize<JsonElement>("[{\"name\": \"Alice\"}, {\"name\": \"Bob\"}]");

        // Act
        var result = json.ToValue();

        // Assert
        Assert.True(result.IsList);
        var list = result.AsList;
        Assert.NotNull(list);
        Assert.Equal(2, list.Count);
        Assert.True(list[0].IsStructure);
        Assert.Equal("Alice", list[0].AsStructure?.GetValue("name").AsString);
        Assert.True(list[1].IsStructure);
        Assert.Equal("Bob", list[1].AsStructure?.GetValue("name").AsString);
    }

    [Fact]
    public void ToValue_ShouldConvertNestedArrays()
    {
        // Arrange
        var json = JsonSerializer.Deserialize<JsonElement>("[[1, 2], [3, 4]]");

        // Act
        var result = json.ToValue();

        // Assert
        Assert.True(result.IsList);
        var outerList = result.AsList;
        Assert.NotNull(outerList);
        Assert.Equal(2, outerList.Count);
        Assert.True(outerList[0].IsList);
        var innerList = outerList[0].AsList;
        Assert.NotNull(innerList);
        Assert.Equal(1, innerList[0].AsInteger);
        Assert.Equal(2, innerList[1].AsInteger);
    }

    [Fact]
    public void ToValue_ShouldConvertComplexObject()
    {
        // Arrange - matches the issue's example
        var json = JsonSerializer.Deserialize<JsonElement>("{\"property1\": \"value1\", \"property2\": 123, \"property3\": true, \"nested\": {\"a\": 1}, \"array\": [1, 2, 3]}");

        // Act
        var result = json.ToValue();

        // Assert
        Assert.True(result.IsStructure);
        var structure = result.AsStructure;
        Assert.NotNull(structure);
        Assert.Equal("value1", structure.GetValue("property1").AsString);
        Assert.Equal(123, structure.GetValue("property2").AsInteger);
        Assert.True(structure.GetValue("property3").AsBoolean);

        var nested = structure.GetValue("nested").AsStructure;
        Assert.NotNull(nested);
        Assert.Equal(1, nested.GetValue("a").AsInteger);

        var array = structure.GetValue("array").AsList;
        Assert.NotNull(array);
        Assert.Equal(3, array.Count);
    }

    [Fact]
    public void ToValue_ShouldHandleEmptyObject()
    {
        // Arrange
        var json = JsonSerializer.Deserialize<JsonElement>("{}");

        // Act
        var result = json.ToValue();

        // Assert
        Assert.True(result.IsStructure);
        var structure = result.AsStructure;
        Assert.NotNull(structure);
        Assert.Equal(0, structure.Count);
    }

    [Fact]
    public void ToValue_ShouldHandleEmptyArray()
    {
        // Arrange
        var json = JsonSerializer.Deserialize<JsonElement>("[]");

        // Act
        var result = json.ToValue();

        // Assert
        Assert.True(result.IsList);
        var list = result.AsList;
        Assert.NotNull(list);
        Assert.Empty(list);
    }

    [Fact]
    public void ToValue_ShouldHandleZero()
    {
        // Arrange
        var json = JsonSerializer.Deserialize<JsonElement>("0");

        // Act
        var result = json.ToValue();

        // Assert
        Assert.True(result.IsNumber);
        Assert.Equal(0, result.AsInteger);
    }

    [Fact]
    public void ToValue_ShouldHandleLargeInteger()
    {
        // Arrange - number that fits in int32
        var json = JsonSerializer.Deserialize<JsonElement>("2147483647");

        // Act
        var result = json.ToValue();

        // Assert
        Assert.True(result.IsNumber);
        Assert.Equal(2147483647, result.AsInteger);
    }

    [Fact]
    public void ToValue_ShouldHandleVeryLargeNumber()
    {
        // Arrange - number too large for int32, should fall back to double
        var json = JsonSerializer.Deserialize<JsonElement>("9999999999999");

        // Act
        var result = json.ToValue();

        // Assert
        Assert.True(result.IsNumber);
        Assert.Equal(9999999999999.0, result.AsDouble);
    }
}
