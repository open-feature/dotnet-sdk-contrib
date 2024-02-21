using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
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
        [JsonProperty("state")]
        internal string State { get; set; }
        [JsonProperty("defaultVariant")]
        internal string DefaultVariant { get; set; }
        [JsonProperty("variants")]
        internal Dictionary<string, object> Variants { get; set; }
        [JsonProperty("targeting")]
        internal object Targeting { get; set; }
        [JsonProperty("source")]
        internal string Source { get; set; }
    }

    internal class FlagSyncData
    {
        [JsonProperty("flags")]
        internal Dictionary<string, FlagConfiguration> Flags { get; set; }
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

        private string _selector;

        private readonly JsonLogicEvaluator _evaluator = new JsonLogicEvaluator(EvaluateOperators.Default);

        internal JsonEvaluator(string selector)
        {
            _selector = selector;

            var stringEvaluator = new StringEvaluator();

            EvaluateOperators.Default.AddOperator("starts_with", stringEvaluator.StartsWith);
            EvaluateOperators.Default.AddOperator("ends_with", stringEvaluator.EndsWith);
        }

        internal void Sync(FlagConfigurationUpdateType updateType, string flagConfigurations)
        {
            var flagConfigsMap = JsonConvert.DeserializeObject<FlagSyncData>(flagConfigurations);

            switch (updateType)
            {
                case FlagConfigurationUpdateType.ALL:
                    _flags = flagConfigsMap.Flags;
                    break;
                case FlagConfigurationUpdateType.ADD:
                    foreach (var keyAndValue in flagConfigsMap.Flags)
                    {
                        _flags[keyAndValue.Key] = keyAndValue.Value;
                    }
                    break;
                case FlagConfigurationUpdateType.UPDATE:
                    foreach (var keyAndValue in flagConfigsMap.Flags)
                    {
                        _flags[keyAndValue.Key] = keyAndValue.Value;
                    }
                    break;
                case FlagConfigurationUpdateType.DELETE:
                    foreach (var keyAndValue in flagConfigsMap.Flags)
                    {
                        _flags.Remove(keyAndValue.Key);
                    }
                    break;

            }
        }

        public ResolutionDetails<bool> ResolveBooleanValue(string flagKey, bool defaultValue, EvaluationContext context = null)
        {
            return ResolveValue(flagKey, defaultValue, context);
        }

        public ResolutionDetails<string> ResolveStringValue(string flagKey, string defaultValue, EvaluationContext context = null)
        {
            return ResolveValue(flagKey, defaultValue, context);
        }

        public ResolutionDetails<int> ResolveIntegerValue(string flagKey, int defaultValue, EvaluationContext context = null)
        {
            return ResolveValue(flagKey, defaultValue, context);
        }

        public ResolutionDetails<double> ResolveDoubleValue(string flagKey, double defaultValue, EvaluationContext context = null)
        {
            return ResolveValue(flagKey, defaultValue, context);
        }

        public ResolutionDetails<Value> ResolveStructureValue(string flagKey, Value defaultValue, EvaluationContext context = null)
        {
            return ResolveValue(flagKey, defaultValue, context);
        }

        private ResolutionDetails<T> ResolveValue<T>(string flagKey, T defaultValue, EvaluationContext context = null)
        {
            // check if we find the flag key
            var reason = Reason.Default;
            if (_flags.TryGetValue(flagKey, out var flagConfiguration))
            {
                if ("DISABLED" == flagConfiguration.State)
                {
                    throw new FeatureProviderException(ErrorType.FlagNotFound, "FLAG_NOT_FOUND: flag '" + flagKey + "' is disabled");
                }
                reason = Reason.Static;
                var variant = flagConfiguration.DefaultVariant;
                if (flagConfiguration.Targeting != null && !String.IsNullOrEmpty(flagConfiguration.Targeting.ToString()) && flagConfiguration.Targeting.ToString() != "{}")
                {
                    reason = Reason.TargetingMatch;
                    var targetingString = flagConfiguration.Targeting.ToString();
                    // Parse json into hierarchical structure
                    var rule = JObject.Parse(targetingString);
                    // the JsonLogic evaluator will return the variant for the value

                    // convert the EvaluationContext object into something the JsonLogic evaluator can work with
                    dynamic contextObj = (object)ConvertToDynamicObject(context.AsDictionary());

                    variant = (string)_evaluator.Apply(rule, contextObj);
                }


                // using the returned variant, go through the available variants and take the correct value if it exists
                if (variant != null && flagConfiguration.Variants.TryGetValue(variant, out var foundVariantValue))
                {
                    if (foundVariantValue is Int64)
                    {
                        checked
                        {
                            foundVariantValue = Convert.ToInt32(foundVariantValue);
                        }
                    }
                    else if (foundVariantValue is JObject value)
                    {
                        foundVariantValue = ConvertJObjectToOpenFeatureValue(value);
                    }
                    if (foundVariantValue is T castValue)
                    {
                        return new ResolutionDetails<T>(
                            flagKey: flagKey,
                            value: castValue,
                            reason: reason,
                            variant: variant
                            );
                    }
                    throw new FeatureProviderException(ErrorType.TypeMismatch, "TYPE_MISMATCH: flag '" + flagKey + "' does not match the expected type");
                }
            }
            throw new FeatureProviderException(ErrorType.FlagNotFound, "FLAG_NOT_FOUND: flag '" + flagKey + "' not found");
        }

        static dynamic ConvertToDynamicObject(IImmutableDictionary<string, Value> dictionary)
        {
            var expandoObject = new System.Dynamic.ExpandoObject();
            var expandoDict = (IDictionary<string, object>)expandoObject;

            foreach (var kvp in dictionary)
            {
                expandoDict.Add(kvp.Key,
                    kvp.Value.IsStructure ? ConvertToDynamicObject(kvp.Value.AsStructure.AsDictionary()) : kvp.Value.AsObject);
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
