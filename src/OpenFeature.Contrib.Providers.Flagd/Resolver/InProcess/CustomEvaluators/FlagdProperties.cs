using System.Collections.Generic;

namespace OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess.CustomEvaluators
{
    internal class FlagdProperties
    {

        internal const string FlagdPropertiesKey = "$flagd";
        internal const string FlagKeyKey = "flagKey";
        internal const string TimestampKey = "timestamp";
        internal const string TargetingKeyKey = "targetingKey";

        internal string FlagKey { get; set; }
        internal long Timestamp { get; set; }
        internal string TargetingKey { get; set; }

        internal FlagdProperties(object from)
        {
            //object value;
            if (from is IDictionary<string, object> dict)
            {
                if (dict.TryGetValue(TargetingKeyKey, out object targetingKeyValue)
                    && targetingKeyValue is string targetingKeyString)
                {
                    TargetingKey = targetingKeyString;
                }
                if (dict.TryGetValue(FlagdPropertiesKey, out object flagdPropertiesObj)
                    && flagdPropertiesObj is IDictionary<string, object> flagdProperties)
                {
                    if (flagdProperties.TryGetValue(FlagKeyKey, out object flagKeyObj)
                        && flagKeyObj is string flagKey)
                    {
                        FlagKey = flagKey;
                    }
                    if (flagdProperties.TryGetValue(TimestampKey, out object timestampObj)
                        && timestampObj is long timestamp)
                    {
                        Timestamp = timestamp;
                    }
                }
            }
        }
    }
}