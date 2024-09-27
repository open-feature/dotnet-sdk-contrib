using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using OpenFeature.Contrib.Providers.Flipt.Converters;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.Flipt;

/// <summary>
///     Extension helper methods
/// </summary>
public static class FliptExtensions
{
    /// <summary>
    ///     Transforms openFeature EvaluationContext to a mutable Dictionary that flipt sdk accepts
    /// </summary>
    /// <param name="evaluationContext">OpenFeature EvaluationContext</param>
    /// <returns></returns>
    public static Dictionary<string, string> ToStringDictionary(this EvaluationContext evaluationContext)
    {
        var serializeOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new OpenFeatureValueConverter(),
                new StructureConverter()
            }
        };
        return evaluationContext?.AsDictionary()
            .ToDictionary(k => k.Key, v => JsonSerializer.Serialize(v.Value.AsObject, serializeOptions)) ?? [];
    }
}