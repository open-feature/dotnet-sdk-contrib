using System.Collections.Generic;

namespace OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess.CustomEvaluators
{
    internal class FlagdProperties {
        
        internal const string FlagdPropertiesKey = "$flagd";
        internal const string FlagKeyKey = "flagKey";
        internal const string TimestampKey = "timestamp";
        
        internal string FlagKey { get; set; }
        internal long Timestamp { get; set; }
        
        internal FlagdProperties(object from)
        {
            //object value;
            if (from is Dictionary<string, object> dict)
            {
                if (dict.TryGetValue(FlagdPropertiesKey, out object flagdPropertiesObj)
                    && flagdPropertiesObj is Dictionary<string, object> flagdProperties)
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