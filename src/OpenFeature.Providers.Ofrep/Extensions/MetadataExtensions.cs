using System.Text.Json;

namespace OpenFeature.Providers.Ofrep.Extensions;

/// <summary>
/// Extension methods for handling metadata conversion.
/// </summary>
internal static class MetadataExtensions
{
    /// <summary>
    /// Converts a dictionary with JsonElement values to a dictionary with primitive types.
    /// </summary>
    /// <param name="metadata">The metadata dictionary with potential JsonElement values.</param>
    /// <returns>A new dictionary with primitive type values (string, double, bool).</returns>
    internal static Dictionary<string, object> ToPrimitiveTypes(this Dictionary<string, object> metadata)
    {
        return metadata.ToDictionary(
            kvp => kvp.Key,
            kvp => ExtractPrimitiveValue(kvp.Value)
        );
    }

    /// <summary>
    /// Extracts a primitive value from an object that may be a JsonElement.
    /// </summary>
    /// <param name="value">The value to extract.</param>
    /// <returns>The extracted primitive value.</returns>
    private static object ExtractPrimitiveValue(object value)
    {
        if (value is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.String => jsonElement.GetString() ?? string.Empty,
                JsonValueKind.Number => jsonElement.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Object => ConvertJsonObject(jsonElement),
                JsonValueKind.Array => ConvertJsonArray(jsonElement),
                JsonValueKind.Null => string.Empty,
                _ => jsonElement.GetRawText()
            };
        }

        // If it's already a primitive type, return as-is
        return value;
    }

    /// <summary>
    /// Converts a JsonElement object to a Dictionary with primitive values.
    /// </summary>
    /// <param name="jsonElement">The JSON element containing the object.</param>
    /// <returns>A dictionary with string keys and primitive values.</returns>
    private static Dictionary<string, object> ConvertJsonObject(JsonElement jsonElement)
    {
        var result = new Dictionary<string, object>();
        foreach (var property in jsonElement.EnumerateObject())
        {
            result[property.Name] = ExtractPrimitiveValue(property.Value);
        }
        return result;
    }

    /// <summary>
    /// Converts a JsonElement array to a List with primitive values.
    /// </summary>
    /// <param name="jsonElement">The JSON element containing the array.</param>
    /// <returns>A list with primitive values.</returns>
    private static List<object> ConvertJsonArray(JsonElement jsonElement)
    {
        var result = new List<object>();
        foreach (var element in jsonElement.EnumerateArray())
        {
            result.Add(ExtractPrimitiveValue(element));
        }
        return result;
    }
}
