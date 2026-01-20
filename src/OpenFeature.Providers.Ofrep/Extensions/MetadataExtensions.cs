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
    /// <returns>A new dictionary with primitive type values (string, int, double, bool).</returns>
    internal static Dictionary<string, object> ToPrimitiveTypes(this Dictionary<string, object> metadata)
    {
        return metadata.ToDictionary(
            kvp => kvp.Key,
            kvp => ExtractValue(kvp.Value)
        );
    }

    /// <summary>
    /// Extracts a primitive value from an object that may be a JsonElement.
    /// For metadata, null values are converted to empty string to maintain backwards compatibility.
    /// </summary>
    /// <param name="value">The value to extract.</param>
    /// <returns>The extracted primitive value.</returns>
    private static object ExtractValue(object value)
    {
        if (value is JsonElement jsonElement)
        {
            var result = JsonConversionHelper.ExtractPrimitiveValue(jsonElement);
            // For metadata, convert null to empty string (backwards compatibility)
            return ConvertNullsToEmptyString(result) ?? string.Empty;
        }

        // If it's already a primitive type, return as-is
        return value;
    }

    /// <summary>
    /// Recursively converts null values to empty strings in dictionaries and lists.
    /// </summary>
    private static object? ConvertNullsToEmptyString(object? value)
    {
        return value switch
        {
            null => string.Empty,
            Dictionary<string, object?> dict => dict.ToDictionary(
                kvp => kvp.Key,
                kvp => ConvertNullsToEmptyString(kvp.Value) ?? string.Empty),
            IList<object?> list => list.Select(item => ConvertNullsToEmptyString(item) ?? string.Empty).ToList(),
            _ => value
        };
    }
}
