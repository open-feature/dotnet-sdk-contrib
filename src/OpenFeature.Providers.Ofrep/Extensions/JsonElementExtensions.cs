using System.Text.Json;
using OpenFeature.Model;

namespace OpenFeature.Providers.Ofrep.Extensions;

/// <summary>
/// Extension methods for converting JsonElement to OpenFeature Value types.
/// </summary>
internal static class JsonElementExtensions
{
    /// <summary>
    /// Converts a JsonElement to an OpenFeature Value.
    /// Uses JsonConversionHelper for consistent behavior with metadata conversion.
    /// </summary>
    /// <param name="jsonElement">The JSON element to convert.</param>
    /// <returns>An OpenFeature Value representing the JSON element.</returns>
    internal static Value ToValue(this JsonElement jsonElement)
    {
        var primitiveValue = JsonConversionHelper.ExtractPrimitiveValue(jsonElement);
        return ConvertToValue(primitiveValue);
    }

    /// <summary>
    /// Converts a primitive value to an OpenFeature Value.
    /// </summary>
    /// <param name="value">The primitive value to convert.</param>
    /// <returns>An OpenFeature Value.</returns>
    private static Value ConvertToValue(object? value)
    {
        return value switch
        {
            null => new Value(),
            string s => new Value(s),
            int i => new Value(i),
            double d => new Value(d),
            bool b => new Value(b),
            Dictionary<string, object?> dict => new Value(ConvertToStructure(dict)),
            List<object?> list => new Value(ConvertToValueList(list)),
            _ => new Value()
        };
    }

    /// <summary>
    /// Converts a dictionary to an OpenFeature Structure.
    /// </summary>
    /// <param name="dict">The dictionary to convert.</param>
    /// <returns>A Structure with the converted values.</returns>
    private static Structure ConvertToStructure(Dictionary<string, object?> dict)
    {
        var builder = Structure.Builder();
        foreach (var kvp in dict)
        {
            builder.Set(kvp.Key, ConvertToValue(kvp.Value));
        }
        return builder.Build();
    }

    /// <summary>
    /// Converts a list to a list of OpenFeature Values.
    /// </summary>
    /// <param name="list">The list to convert.</param>
    /// <returns>A list of Values.</returns>
    private static IList<Value> ConvertToValueList(List<object?> list)
    {
        var result = new List<Value>(list.Count);
        foreach (var item in list)
        {
            result.Add(ConvertToValue(item));
        }
        return result;
    }
}
