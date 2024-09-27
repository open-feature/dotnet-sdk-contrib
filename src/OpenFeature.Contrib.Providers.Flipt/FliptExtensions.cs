using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.Flipt;

public static class FliptExtensions
{
    /// <summary>
    ///     Transforms openFeature EvaluationContext to a mutable Dictionary that flipt sdk accepts
    /// </summary>
    /// <param name="evaluationContext">OpenFeature EvaluationContext</param>
    /// <returns></returns>
    public static Dictionary<string, string> ToStringDictionary(this EvaluationContext evaluationContext)
    {
        return evaluationContext?.AsDictionary().ToDictionary(k => k.Key, v => JsonSerializer.Serialize(v.Value)) ?? [];
    }
}