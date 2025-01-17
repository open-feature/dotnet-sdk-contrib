using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using JsonLogic.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenFeature.Constant;
using OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess.CustomEvaluators;
using OpenFeature.Error;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess
{
    internal class FlagConfiguration
    {
        [JsonProperty("state")] internal string State { get; set; }
        [JsonProperty("defaultVariant")] internal string DefaultVariant { get; set; }
        [JsonProperty("variants")] internal Dictionary<string, object> Variants { get; set; }
        [JsonProperty("targeting")] internal object Targeting { get; set; }
        [JsonProperty("source")] internal string Source { get; set; }
        [JsonProperty("metadata")] internal Dictionary<string, object> Metadata { get; set; }
    }

    internal class FlagSyncData
    {
        [JsonProperty("flags")] internal Dictionary<string, FlagConfiguration> Flags { get; set; }
        [JsonProperty("$evaluators")] internal Dictionary<string, object> Evaluators { get; set; }
        [JsonProperty("metadata")] internal Dictionary<string, object> Metadata { get; set; }
    }

    internal class FlagConfigurationSync
    {
        string FlagData { get; set; }
        string Source { get; set; }
    }

    internal enum FlagConfigurationUpdateType
    {
        ADD,
        UPDATE,
        ALL,
        DELETE
    }

    internal class JsonEvaluator
    {
        private Dictionary<string, FlagConfiguration> _flags = new Dictionary<string, FlagConfiguration>();
        private Dictionary<string, object> _flagSetMetadata = new Dictionary<string, object>();

        private string _selector;

        private readonly JsonLogicEvaluator _evaluator = new JsonLogicEvaluator(EvaluateOperators.Default);


        internal JsonEvaluator(string selector)
        {
            _selector = selector;

            var stringEvaluator = new StringEvaluator();
            var semVerEvaluator = new SemVerEvaluator();
            var fractionalEvaluator = new FractionalEvaluator();

            EvaluateOperators.Default.AddOperator("starts_with", stringEvaluator.StartsWith);
            EvaluateOperators.Default.AddOperator("ends_with", stringEvaluator.EndsWith);
            EvaluateOperators.Default.AddOperator("sem_ver", semVerEvaluator.Evaluate);
            EvaluateOperators.Default.AddOperator("fractional", fractionalEvaluator.Evaluate);
        }

        internal FlagSyncData Parse(string flagConfigurations)
        {
            var parsed = JsonConvert.DeserializeObject<FlagSyncData>(flagConfigurations);
            var transformed = JsonConvert.SerializeObject(parsed);
            // replace evaluators
            if (parsed.Evaluators != null && parsed.Evaluators.Count > 0)
            {
                parsed.Evaluators.Keys.ToList().ForEach(key =>
                {
                    var val = parsed.Evaluators[key];
                    var evaluatorRegex = new Regex("{\"\\$ref\":\"" + key + "\"}");
                    transformed = evaluatorRegex.Replace(transformed, Convert.ToString(val));
                });
            }


            var data = JsonConvert.DeserializeObject<FlagSyncData>(transformed);
            if (data.Metadata == null)
            {
                data.Metadata = new Dictionary<string, object>();
            }
            else
            {
                foreach (var key in new List<string>(data.Metadata.Keys))
                {
                    var value = data.Metadata[key];
                    if (value is long longValue)
                    {
                        data.Metadata[key] = (int)longValue;
                        continue;
                    }

                    VerifyMetadataValue(key, value);
                }
            }

            foreach (var flagConfig in data.Flags)
            {
                if (flagConfig.Value.Metadata == null)
                {
                    continue;
                }

                foreach (var key in new List<string>(flagConfig.Value.Metadata.Keys))
                {
                    var value = flagConfig.Value.Metadata[key];
                    if (value is long longValue)
                    {
                        flagConfig.Value.Metadata[key] = (int)longValue;
                        continue;
                    }

                    VerifyMetadataValue(key, value);
                }
            }

            return data;
        }

        private static void VerifyMetadataValue(string key, object value)
        {
            if (value is int || value is double || value is string || value is bool)
            {
                return;
            }

            throw new ParseErrorException("Metadata entry for key " + key + " and value " + value +
                                          " is of unknown type");
        }

        internal void Sync(FlagConfigurationUpdateType updateType, string flagConfigurations)
        {
            var flagConfigsMap = Parse(flagConfigurations);

            switch (updateType)
            {
                case FlagConfigurationUpdateType.ALL:
                    _flags = flagConfigsMap.Flags;
                    _flagSetMetadata = flagConfigsMap.Metadata;

                    break;
                case FlagConfigurationUpdateType.ADD:
                case FlagConfigurationUpdateType.UPDATE:
                    foreach (var keyAndValue in flagConfigsMap.Flags)
                    {
                        _flags[keyAndValue.Key] = keyAndValue.Value;
                    }

                    foreach (var metadata in flagConfigsMap.Metadata)
                    {
                        _flagSetMetadata[metadata.Key] = metadata.Value;
                    }

                    break;
                case FlagConfigurationUpdateType.DELETE:
                    foreach (var keyAndValue in flagConfigsMap.Flags)
                    {
                        _flags.Remove(keyAndValue.Key);
                    }

                    foreach (var keyValuePair in flagConfigsMap.Metadata)
                    {
                        _flagSetMetadata.Remove(keyValuePair.Key);
                    }

                    break;
            }
        }

        public ResolutionDetails<bool> ResolveBooleanValueAsync(string flagKey, bool defaultValue,
            EvaluationContext context = null)
        {
            return ResolveValue(flagKey, defaultValue, context);
        }

        public ResolutionDetails<string> ResolveStringValueAsync(string flagKey, string defaultValue,
            EvaluationContext context = null)
        {
            return ResolveValue(flagKey, defaultValue, context);
        }

        public ResolutionDetails<int> ResolveIntegerValueAsync(string flagKey, int defaultValue,
            EvaluationContext context = null)
        {
            return ResolveValue(flagKey, defaultValue, context);
        }

        public ResolutionDetails<double> ResolveDoubleValueAsync(string flagKey, double defaultValue,
            EvaluationContext context = null)
        {
            return ResolveValue(flagKey, defaultValue, context);
        }

        public ResolutionDetails<Value> ResolveStructureValueAsync(string flagKey, Value defaultValue,
            EvaluationContext context = null)
        {
            return ResolveValue(flagKey, defaultValue, context);
        }

        private ResolutionDetails<T> ResolveValue<T>(string flagKey, T defaultValue,
            EvaluationContext context = null)
        {
            // check if we find the flag key
            var reason = Reason.Static;
            if (_flags.TryGetValue(flagKey, out var flagConfiguration))
            {
                if ("DISABLED" == flagConfiguration.State)
                {
                    throw new FeatureProviderException(ErrorType.FlagNotFound,
                        "FLAG_NOT_FOUND: flag '" + flagKey + "' is disabled");
                }

                Dictionary<string, object> combinedMetadata = new Dictionary<string, object>(_flagSetMetadata);
                if (flagConfiguration.Metadata != null)
                {
                    foreach (var metadataEntry in flagConfiguration.Metadata)
                    {
                        combinedMetadata[metadataEntry.Key] = metadataEntry.Value;
                    }
                }

                var flagMetadata = new ImmutableMetadata(combinedMetadata);
                var variant = flagConfiguration.DefaultVariant;
                if (flagConfiguration.Targeting != null &&
                    !String.IsNullOrEmpty(flagConfiguration.Targeting.ToString()) &&
                    flagConfiguration.Targeting.ToString() != "{}")
                {
                    reason = Reason.TargetingMatch;
                    var flagdProperties = new Dictionary<string, Value>();
                    flagdProperties.Add(FlagdProperties.FlagKeyKey, new Value(flagKey));
                    flagdProperties.Add(FlagdProperties.TimestampKey,
                        new Value(DateTimeOffset.UtcNow.ToUnixTimeSeconds()));

                    if (context == null)
                    {
                        context = EvaluationContext.Builder().Build();
                    }

                    var targetingContext = context.AsDictionary().Add(
                        FlagdProperties.FlagdPropertiesKey,
                        new Value(new Structure(flagdProperties))
                    );

                    var targetingString = flagConfiguration.Targeting.ToString();
                    // Parse json into hierarchical structure
                    var rule = JObject.Parse(targetingString);
                    // the JsonLogic evaluator will return the variant for the value

                    // convert the EvaluationContext object into something the JsonLogic evaluator can work with
                    dynamic contextObj = (object)ConvertToDynamicObject(targetingContext);

                    // convert whatever is returned to a string to try to use it as an index to Variants
                    var ruleResult = _evaluator.Apply(rule, contextObj);
                    if (ruleResult is bool)
                    {
                        // if this was a bool, convert from "True" to "true" to match JSON
                        variant = Convert.ToString(ruleResult).ToLower();
                    }
                    else
                    {
                        // convert whatever is returned to a string to support shorthand
                        variant = Convert.ToString(ruleResult);
                    }
                }

                // using the returned variant, go through the available variants and take the correct value if it exists
                if (variant == null)
                {
                    // if variant is null, revert to default
                    reason = Reason.Default;
                    flagConfiguration.Variants.TryGetValue(flagConfiguration.DefaultVariant,
                        out var defaultVariantValue);
                    if (defaultVariantValue == null)
                    {
                        throw new FeatureProviderException(ErrorType.ParseError,
                            "PARSE_ERROR: flag '" + flagKey + "' has missing or invalid defaultVariant.");
                    }

                    var value = ExtractFoundVariant<T>(defaultVariantValue, flagKey);
                    return new ResolutionDetails<T>(
                        flagKey: flagKey,
                        value,
                        reason: reason,
                        variant: variant,
                        flagMetadata: flagMetadata
                    );
                }
                else if (flagConfiguration.Variants.TryGetValue(variant, out var foundVariantValue))
                {
                    // if variant can be found, return it - this could be TARGETING_MATCH or STATIC. 
                    var value = ExtractFoundVariant<T>(foundVariantValue, flagKey);
                    return new ResolutionDetails<T>(
                        flagKey: flagKey,
                        value,
                        reason: reason,
                        variant: variant,
                        flagMetadata: flagMetadata
                    );
                }
            }

            throw new FeatureProviderException(ErrorType.FlagNotFound,
                "FLAG_NOT_FOUND: flag '" + flagKey + "' not found");
        }

        static T ExtractFoundVariant<T>(object foundVariantValue, string flagKey)
        {
            if (foundVariantValue is long)
            {
                foundVariantValue = Convert.ToInt32(foundVariantValue);
            }

            if (typeof(T) == typeof(double))
            {
                foundVariantValue = Convert.ToDouble(foundVariantValue);
            }
            else if (foundVariantValue is JObject value)
            {
                foundVariantValue = ConvertJObjectToOpenFeatureValue(value);
            }

            if (foundVariantValue is T castValue)
            {
                return castValue;
            }

            throw new FeatureProviderException(ErrorType.TypeMismatch,
                "TYPE_MISMATCH: flag '" + flagKey + "' does not match the expected type");
        }

        static dynamic ConvertToDynamicObject(IImmutableDictionary<string, Value> dictionary)
        {
            var expandoObject = new System.Dynamic.ExpandoObject();
            var expandoDict = (IDictionary<string, object>)expandoObject;

            foreach (var kvp in dictionary)
            {
                expandoDict.Add(kvp.Key,
                    kvp.Value.IsStructure
                        ? ConvertToDynamicObject(kvp.Value.AsStructure.AsDictionary())
                        : kvp.Value.AsObject);
            }

            return expandoObject;
        }

        static Value ConvertJObjectToOpenFeatureValue(JObject jsonValue)
        {
            var result = new Dictionary<string, Value>();

            foreach (var property in jsonValue.Properties())
            {
                switch (property.Value.Type)
                {
                    case JTokenType.String:
                        result.Add(property.Name, new Value((string)property.Value));
                        break;

                    case JTokenType.Integer:
                        result.Add(property.Name, new Value((Int64)property.Value));
                        break;

                    case JTokenType.Boolean:
                        result.Add(property.Name, new Value((bool)property.Value));
                        break;

                    case JTokenType.Float:
                        result.Add(property.Name, new Value((float)property.Value));
                        break;

                    case JTokenType.Object:
                        result.Add(property.Name, ConvertJObjectToOpenFeatureValue((JObject)property.Value));
                        break;

                    default:
                        // Handle unknown data type or throw an exception
                        throw new InvalidOperationException($"Unsupported data type: {property.Value.Type}");
                }
            }

            return new Value(new Structure(result));
        }
    }
}