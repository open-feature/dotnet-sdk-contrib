using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Logic;
using Json.More;

namespace OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess.CustomEvaluators;

internal sealed class FlagdProperties
{

    internal const string FlagdPropertiesKey = "$flagd";
    internal const string FlagKeyKey = "flagKey";
    internal const string TimestampKey = "timestamp";
    internal const string TargetingKeyKey = "targetingKey";

    internal string FlagKey { get; set; }
    internal long Timestamp { get; set; }
    internal string TargetingKey { get; set; }

    internal FlagdProperties(EvaluationContext from)
    {

        if (from.TryFind(TargetingKeyKey, out JsonNode targetingKeyValue)
            && targetingKeyValue.GetValueKind() == JsonValueKind.String)
        {
            TargetingKey = targetingKeyValue.ToString();
        }
        if (from.TryFind($"{FlagdPropertiesKey}.{FlagKeyKey}", out JsonNode flagKeyValue)
            && flagKeyValue.GetValueKind() == JsonValueKind.String)
        {
            FlagKey = flagKeyValue.ToString();
        }
        if (from.TryFind($"{FlagdPropertiesKey}.{TimestampKey}", out JsonNode timestampValue)
            && timestampValue.GetValueKind() == JsonValueKind.Number)
        {
            Timestamp = timestampValue.GetValue<long>();
        }
    }
}
