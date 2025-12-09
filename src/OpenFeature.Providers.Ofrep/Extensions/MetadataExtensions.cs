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
            kvp => ExtractPrimitiveValue(kvp.Key, kvp.Value)
        );
    }

    /// <summary>
    /// Extracts a primitive value from an object that may be a JsonElement.
    /// </summary>
    /// <param name="key">The key for error reporting purposes.</param>
    /// <param name="value">The value to extract.</param>
    /// <returns>The extracted primitive value.</returns>
    private static object ExtractPrimitiveValue(string key, object value)
    {
        if (value is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.String => jsonElement.GetString() ?? string.Empty,
                JsonValueKind.Number => jsonElement.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => value.ToString() ?? string.Empty
            };
        }

        // If it's already a primitive type, return as-is
        return value;
    }
}
