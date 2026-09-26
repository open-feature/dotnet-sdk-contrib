using OpenFeature.Model;

namespace OpenFeature.Providers.Ofrep.Extensions;

/// <summary>
/// Extension methods for EvaluationContext class
/// </summary>
internal static class EvaluationContextExtensions
{
    /// <summary>
    /// Converts the EvaluationContext to a dictionary of string keys and object values.
    /// </summary>
    /// <param name="context">the evaluation context</param>
    /// <returns>A dictionary representation of the evaluation context.</returns>
    internal static Dictionary<string, object?> ToDictionary(this EvaluationContext context)
    {
        return context.AsDictionary().ToDictionary(
            kvp => kvp.Key,
            kvp => ConvertToJsonValue(kvp.Value));
    }

    /// <summary>
    /// Converts a Value to a JSON-serializable object. Structures and lists are recursively converted
    /// to <see cref="Dictionary{TKey,TValue}"/> and <see cref="List{T}"/> so that the source-generated
    /// serializer graph covers every runtime type a context value can produce.
    /// </summary>
    /// <param name="value">the value to convert</param>
    /// <returns>A JSON-serializable representation of the value.</returns>
    private static object? ConvertToJsonValue(Value value)
    {
        if (value.IsStructure)
        {
            return value.AsStructure!.ToDictionary(
                kvp => kvp.Key,
                kvp => ConvertToJsonValue(kvp.Value));
        }

        if (value.IsList)
        {
            return value.AsList!.Select(ConvertToJsonValue).ToList();
        }

        return value.AsObject;
    }
}
