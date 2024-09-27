using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.Flipt;

public static class FliptExtensions
{
    public static Dictionary<string, string> ToStringDictionary(this EvaluationContext evaluationContext)
    {
        return evaluationContext?.AsDictionary().ToDictionary(k => k.Key, v => JsonSerializer.Serialize(v.Value)) ?? [];
    }
}