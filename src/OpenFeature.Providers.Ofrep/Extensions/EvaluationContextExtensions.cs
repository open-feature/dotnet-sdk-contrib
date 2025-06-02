using OpenFeature.Model;

namespace OpenFeature.Providers.Ofrep.Extensions;

/// <summary>
/// Extension methods for EvaluationContext class
/// </summary>
public static class EvaluationContextExtensions
{
    /// <summary>
    /// Converts the EvaluationContext to a dictionary of string keys and object values.
    /// </summary>
    /// <param name="context">the evaluation context</param>
    /// <returns>A dictionary representation of the evaluation context.</returns>
    public static Dictionary<string, object?> ToDictionary(this EvaluationContext context)
    {
        return context.AsDictionary().ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.AsObject
        );
    }
}
