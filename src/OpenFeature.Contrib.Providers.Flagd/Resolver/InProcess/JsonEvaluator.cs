using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Json.Logic;
using Json.More;
using OpenFeature.Constant;
using OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess.CustomEvaluators;
using OpenFeature.Error;
using OpenFeature.Model;
using EvaluationContext = OpenFeature.Model.EvaluationContext;

namespace OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess
{
    internal class FlagConfiguration
    {
        [JsonPropertyName("state")] public string State { get; set; }
        [JsonPropertyName("defaultVariant")] public string DefaultVariant { get; set; }
        [JsonPropertyName("variants")] public Dictionary<string, JsonElement> Variants { get; set; }
        [JsonPropertyName("targeting")] public object Targeting { get; set; }
        [JsonPropertyName("source")] public string Source { get; set; }
        [JsonPropertyName("metadata")] public Dictionary<string, JsonElement> Metadata { get; set; }
    }

    internal class FlagSyncData
    {
        [JsonPropertyName("flags")] public Dictionary<string, FlagConfiguration> Flags { get; set; }
        [JsonPropertyName("$evaluators")] public Dictionary<string, object> Evaluators { get; set; }
        [JsonPropertyName("metadata")] public Dictionary<string, JsonElement> Metadata { get; set; }
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
        private Dictionary<string, JsonElement> _flagSetMetadata = new Dictionary<string, JsonElement>();

        private string _selector;

        //private readonly JsonEvaluator _evaluator = new Evaluator;


        internal JsonEvaluator(string selector)
        {
            _selector = selector;

            RuleRegistry.AddRule("starts_with", new StartsWithRule());
            RuleRegistry.AddRule("ends_with", new EndsWithRule());
            RuleRegistry.AddRule("sem_ver", new SemVerRule());
            RuleRegistry.AddRule("fractional", new FractionalEvaluator());
        }

        internal FlagSyncData Parse(string flagConfigurations)
        {
            var parsed = JsonSerializer.Deserialize<FlagSyncData>(flagConfigurations);
            var transformed = JsonSerializer.Serialize(parsed);
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


            var data = JsonSerializer.Deserialize<FlagSyncData>(transformed);
            if (data.Metadata == null)
            {
                data.Metadata = new Dictionary<string, JsonElement>();
            }
            else
            {
                foreach (var key in new List<string>(data.Metadata.Keys))
                {
                    var value = data.Metadata[key];
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
                    VerifyMetadataValue(key, value);
                }
            }

            return data;
        }

        private static void VerifyMetadataValue(string key, JsonElement value)
        {
            //if (value is int || value is double || value is string || value is bool)
            if (value.ValueKind == JsonValueKind.Number
                || value.ValueKind == JsonValueKind.String
                || value.ValueKind == JsonValueKind.True
                || value.ValueKind == JsonValueKind.False)
            {
                return;
            }

            throw new ParseErrorException("Metadata entry for key " + key + " and value " + value +
                                          " is of unknown type");
        }

        private static object ExtractMetadataValue(string key, JsonElement value)
        {
            switch (value.ValueKind)
            {
                case JsonValueKind.Number:
                    return value.GetDouble();
                case JsonValueKind.String:
                    return value.GetString();
                case JsonValueKind.False:
                case JsonValueKind.True:
                    return value.GetBoolean();

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

                Dictionary<string, object> combinedMetadata = _flagSetMetadata.ToDictionary(
                    entry => entry.Key,
                    entry => ExtractMetadataValue(entry.Key, entry.Value));

                if (flagConfiguration.Metadata != null)
                {
                    foreach (var metadataEntry in flagConfiguration.Metadata)
                    {
                        combinedMetadata[metadataEntry.Key] = ExtractMetadataValue(metadataEntry.Key, metadataEntry.Value);
                    }
                }

                var flagMetadata = new ImmutableMetadata(combinedMetadata);
                var variant = flagConfiguration.DefaultVariant;
                if (flagConfiguration.Targeting != null &&
                    !String.IsNullOrEmpty(flagConfiguration.Targeting.ToString()) &&
                    flagConfiguration.Targeting.ToString() != "{}")
                {
                    reason = Reason.TargetingMatch;
                    var flagdProperties = new Dictionary<string, Value>
                    {
                      { FlagdProperties.FlagKeyKey, new Value(flagKey) },
                      { FlagdProperties.TimestampKey, new Value(DateTimeOffset.UtcNow.ToUnixTimeSeconds()) }
                    };

                    if (context == null)
                    {
                        context = EvaluationContext.Builder().Build();
                    }


                    var contextDictionary = context.AsDictionary();
                    contextDictionary = contextDictionary.Add(FlagdProperties.FlagdPropertiesKey, new Value(new Structure(flagdProperties)));
                    // TODO: all missing comments
                    var targetingString = flagConfiguration.Targeting.ToString();
                    // Parse json into hierarchical structure
                    var rule = JsonNode.Parse(targetingString);
                    // the JsonLogic evaluator will return the variant for the value

                    // convert the EvaluationContext object into something the JsonLogic evaluator can work with
                    var contextObj = JsonNode.Parse(JsonSerializer.Serialize(ConvertToDynamicObject(contextDictionary)));

                    // convert whatever is returned to a string to try to use it as an index to Variants
                    var ruleResult = JsonLogic.Apply(rule, contextObj);
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
                    if (defaultVariantValue.ValueKind == JsonValueKind.Undefined || defaultVariantValue.ValueKind == JsonValueKind.Null)
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

        static T ExtractFoundVariant<T>(JsonElement foundVariantValue, string flagKey)
        {
            try
            {
                if (typeof(T) == typeof(int))
                {
                    return (T)(object)foundVariantValue.GetInt32();
                }

                if (typeof(T) == typeof(double))
                {
                    return (T)(object)foundVariantValue.GetDouble();
                }

                if (typeof(T) == typeof(bool))
                {
                    return (T)(object)foundVariantValue.GetBoolean();
                }

                if (typeof(T) == typeof(string))
                {
                    return (T)(object)foundVariantValue.GetString();
                }

                if (foundVariantValue.ValueKind == JsonValueKind.Object || foundVariantValue.ValueKind == JsonValueKind.Array)
                {
                    var converted = ConvertJsonObjectToOpenFeatureValue(foundVariantValue.AsNode().AsObject());
                    if (converted is T castValue)
                    {
                        return castValue;
                    }
                }
                throw new Exception("Cannot cast flag value to expected type");

            }
            catch (Exception e)
            {
                throw new FeatureProviderException(ErrorType.TypeMismatch,
                    "TYPE_MISMATCH: flag '" + flagKey + "' does not match the expected type", e);
            }

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

        static Value ConvertJsonObjectToOpenFeatureValue(JsonObject jsonValue)
        {
            var result = new Dictionary<string, Value>();

            foreach (var property in jsonValue.AsEnumerable())
            {
                switch (property.Value.GetValueKind())
                {
                    case JsonValueKind.String:
                        result.Add(property.Key, new Value((string)property.Value));
                        break;

                    case JsonValueKind.Number:
                        result.Add(property.Key, new Value((long)property.Value));
                        break;

                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        result.Add(property.Key, new Value((bool)property.Value));
                        break;

                    case JsonValueKind.Object:
                    case JsonValueKind.Array:
                        result.Add(property.Key, ConvertJsonObjectToOpenFeatureValue(property.Value.AsObject()));
                        break;

                    default:
                        // Handle unknown data type or throw an exception
                        throw new InvalidOperationException($"Unsupported data type: {property.Value.GetType()}");
                }
            }

            return new Value(new Structure(result));
        }
    }
}