using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace OpenFeature.Providers.GOFeatureFlag.Converters;

/// <summary>
///     DictionaryConverter is converting a json Dictionary to a Dictionary with real object.
/// </summary>
public static class DictionaryConverter
{
    /// <summary>
    ///     Function that convert the dictionary to a Dictionary with real object.
    /// </summary>
    /// <param name="inputDictionary"></param>
    /// <returns>A dictionary with real types.</returns>
    public static Dictionary<string, object?> ConvertDictionary(Dictionary<string, object> inputDictionary)
    {
        return inputDictionary.ToDictionary(
            kvp => kvp.Key,
            kvp => ConvertValue(kvp.Value)
        );
    }

    /// <summary>
    ///     Function that convert a value to a object.
    /// </summary>
    /// <param name="value"></param>
    /// <returns>A value with real types.</returns>
    public static object? ConvertValue(object value)
    {
        if (value is JsonElement jsonElement)
        {
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.String:
                    return jsonElement.GetString() ?? string.Empty;
                case JsonValueKind.Number:
                    if (jsonElement.TryGetInt32(out var intValue))
                    {
                        return intValue;
                    }

                    if (jsonElement.TryGetDouble(out var doubleValue))
                    {
                        return doubleValue;
                    }

                    return jsonElement.GetRawText(); // Fallback to string if not int or double
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return null;
                case JsonValueKind.Object:
                    return ConvertDictionary(
                        JsonSerializer
                            .Deserialize<Dictionary<string, object>>(jsonElement
                                .GetRawText())??new Dictionary<string, object>());
                case JsonValueKind.Array:
                    var array = new List<object>();
                    foreach (var element in jsonElement.EnumerateArray())
                    {
                        var convertedValue = ConvertValue(element);
                        if (convertedValue is not null)
                        {
                            array.Add(convertedValue);
                        }
                    }
                    return array;
                default:
                    return jsonElement.GetRawText(); // Handle other types as needed
            }
        }

        return value; // Return original value if not a JsonElement
    }
}
