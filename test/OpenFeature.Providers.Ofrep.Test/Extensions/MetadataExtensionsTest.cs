using System.Text.Json;
using OpenFeature.Providers.Ofrep.Extensions;
using Xunit;

namespace OpenFeature.Providers.Ofrep.Test.Extensions;

public class MetadataExtensionsTest
{
    [Fact]
    public void ToPrimitiveTypes_ShouldConvertJsonElementString_ToString()
    {
        // Arrange
        var json = JsonSerializer.Deserialize<JsonElement>("\"hello world\"");
        var metadata = new Dictionary<string, object>
        {
            { "stringKey", json }
        };

        // Act
        var result = metadata.ToPrimitiveTypes();

        // Assert
        Assert.IsType<string>(result["stringKey"]);
        Assert.Equal("hello world", result["stringKey"]);
    }

    [Fact]
    public void ToPrimitiveTypes_ShouldConvertJsonElementNumber_ToDouble()
    {
        // Arrange
        var json = JsonSerializer.Deserialize<JsonElement>("42.5");
        var metadata = new Dictionary<string, object>
        {
            { "numberKey", json }
        };

        // Act
        var result = metadata.ToPrimitiveTypes();

        // Assert
        Assert.IsType<double>(result["numberKey"]);
        Assert.Equal(42.5, result["numberKey"]);
    }

    [Fact]
    public void ToPrimitiveTypes_ShouldConvertJsonElementInteger_ToDouble()
    {
        // Arrange
        var json = JsonSerializer.Deserialize<JsonElement>("100");
        var metadata = new Dictionary<string, object>
        {
            { "intKey", json }
        };

        // Act
        var result = metadata.ToPrimitiveTypes();

        // Assert
        Assert.IsType<double>(result["intKey"]);
        Assert.Equal(100.0, result["intKey"]);
    }

    [Fact]
    public void ToPrimitiveTypes_ShouldConvertJsonElementTrue_ToBoolTrue()
    {
        // Arrange
        var json = JsonSerializer.Deserialize<JsonElement>("true");
        var metadata = new Dictionary<string, object>
        {
            { "boolKey", json }
        };

        // Act
        var result = metadata.ToPrimitiveTypes();

        // Assert
        Assert.IsType<bool>(result["boolKey"]);
        Assert.True((bool)result["boolKey"]);
    }

    [Fact]
    public void ToPrimitiveTypes_ShouldConvertJsonElementFalse_ToBoolFalse()
    {
        // Arrange
        var json = JsonSerializer.Deserialize<JsonElement>("false");
        var metadata = new Dictionary<string, object>
        {
            { "boolKey", json }
        };

        // Act
        var result = metadata.ToPrimitiveTypes();

        // Assert
        Assert.IsType<bool>(result["boolKey"]);
        Assert.False((bool)result["boolKey"]);
    }

    [Fact]
    public void ToPrimitiveTypes_ShouldHandleNullJsonElement_AsEmptyString()
    {
        // Arrange
        var json = JsonSerializer.Deserialize<JsonElement>("null");
        var metadata = new Dictionary<string, object>
        {
            { "nullKey", json }
        };

        // Act
        var result = metadata.ToPrimitiveTypes();

        // Assert
        Assert.IsType<string>(result["nullKey"]);
        Assert.Equal(string.Empty, result["nullKey"]);
    }

    [Fact]
    public void ToPrimitiveTypes_ShouldHandleJsonElementObject_AsString()
    {
        // Arrange
        var json = JsonSerializer.Deserialize<JsonElement>("{\"nested\": \"value\"}");
        var metadata = new Dictionary<string, object>
        {
            { "objectKey", json }
        };

        // Act
        var result = metadata.ToPrimitiveTypes();

        // Assert
        Assert.IsType<string>(result["objectKey"]);
        Assert.Contains("nested", (string)result["objectKey"]);
    }

    [Fact]
    public void ToPrimitiveTypes_ShouldHandleJsonElementArray_AsString()
    {
        // Arrange
        var json = JsonSerializer.Deserialize<JsonElement>("[1, 2, 3]");
        var metadata = new Dictionary<string, object>
        {
            { "arrayKey", json }
        };

        // Act
        var result = metadata.ToPrimitiveTypes();

        // Assert
        Assert.IsType<string>(result["arrayKey"]);
        Assert.Contains("1", (string)result["arrayKey"]);
    }

    [Fact]
    public void ToPrimitiveTypes_ShouldPreservePrimitiveString()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            { "stringKey", "already a string" }
        };

        // Act
        var result = metadata.ToPrimitiveTypes();

        // Assert
        Assert.IsType<string>(result["stringKey"]);
        Assert.Equal("already a string", result["stringKey"]);
    }

    [Fact]
    public void ToPrimitiveTypes_ShouldPreservePrimitiveDouble()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            { "doubleKey", 3.14 }
        };

        // Act
        var result = metadata.ToPrimitiveTypes();

        // Assert
        Assert.IsType<double>(result["doubleKey"]);
        Assert.Equal(3.14, result["doubleKey"]);
    }

    [Fact]
    public void ToPrimitiveTypes_ShouldPreservePrimitiveInt()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            { "intKey", 42 }
        };

        // Act
        var result = metadata.ToPrimitiveTypes();

        // Assert
        Assert.IsType<int>(result["intKey"]);
        Assert.Equal(42, result["intKey"]);
    }

    [Fact]
    public void ToPrimitiveTypes_ShouldPreservePrimitiveBool()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            { "boolKey", true }
        };

        // Act
        var result = metadata.ToPrimitiveTypes();

        // Assert
        Assert.IsType<bool>(result["boolKey"]);
        Assert.True((bool)result["boolKey"]);
    }

    [Fact]
    public void ToPrimitiveTypes_ShouldHandleMixedTypes()
    {
        // Arrange
        var jsonString = JsonSerializer.Deserialize<JsonElement>("\"json string\"");
        var jsonNumber = JsonSerializer.Deserialize<JsonElement>("99.9");
        var jsonBool = JsonSerializer.Deserialize<JsonElement>("true");

        var metadata = new Dictionary<string, object>
        {
            { "jsonString", jsonString },
            { "jsonNumber", jsonNumber },
            { "jsonBool", jsonBool },
            { "primitiveString", "native string" },
            { "primitiveInt", 123 },
            { "primitiveBool", false }
        };

        // Act
        var result = metadata.ToPrimitiveTypes();

        // Assert
        Assert.Equal("json string", result["jsonString"]);
        Assert.Equal(99.9, result["jsonNumber"]);
        Assert.True((bool)result["jsonBool"]);
        Assert.Equal("native string", result["primitiveString"]);
        Assert.Equal(123, result["primitiveInt"]);
        Assert.False((bool)result["primitiveBool"]);
    }

    [Fact]
    public void ToPrimitiveTypes_ShouldReturnEmptyDictionary_WhenInputIsEmpty()
    {
        // Arrange
        var metadata = new Dictionary<string, object>();

        // Act
        var result = metadata.ToPrimitiveTypes();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ToPrimitiveTypes_ShouldHandleEmptyString()
    {
        // Arrange
        var json = JsonSerializer.Deserialize<JsonElement>("\"\"");
        var metadata = new Dictionary<string, object>
        {
            { "emptyString", json }
        };

        // Act
        var result = metadata.ToPrimitiveTypes();

        // Assert
        Assert.IsType<string>(result["emptyString"]);
        Assert.Equal(string.Empty, result["emptyString"]);
    }

    [Fact]
    public void ToPrimitiveTypes_ShouldHandleNegativeNumbers()
    {
        // Arrange
        var json = JsonSerializer.Deserialize<JsonElement>("-42.5");
        var metadata = new Dictionary<string, object>
        {
            { "negativeNumber", json }
        };

        // Act
        var result = metadata.ToPrimitiveTypes();

        // Assert
        Assert.IsType<double>(result["negativeNumber"]);
        Assert.Equal(-42.5, result["negativeNumber"]);
    }

    [Fact]
    public void ToPrimitiveTypes_ShouldHandleZero()
    {
        // Arrange
        var json = JsonSerializer.Deserialize<JsonElement>("0");
        var metadata = new Dictionary<string, object>
        {
            { "zero", json }
        };

        // Act
        var result = metadata.ToPrimitiveTypes();

        // Assert
        Assert.IsType<double>(result["zero"]);
        Assert.Equal(0.0, result["zero"]);
    }
}
