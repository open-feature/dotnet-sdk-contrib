using Flagsmith;
using OpenFeature.Constant;
using OpenFeature.Model;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Trait = Flagsmith.Trait;
using OpenFeature.Error;
using System;

namespace OpenFeature.Contrib.Providers.Flagsmith
{
    /// <summary>
    /// FlagsmithProvider is the .NET provider implementation for the feature flag solution Flagsmith.
    /// </summary>
    public class FlagsmithProvider : FeatureProvider
    {
        private readonly static Metadata Metadata = new("Flagsmith Provider");
        internal readonly IFlagsmithClient _flagsmithClient;
        /// <summary>
        /// Creates new instance of <see cref="FlagsmithProvider"/>
        /// </summary>
        /// <param name="options">Flagsmith client options. You can just use <see cref="FlagsmithConfiguration"/> class</param>
        public FlagsmithProvider(IFlagsmithConfiguration options)
        {
            _flagsmithClient = new FlagsmithClient(options);
        }

        /// <summary>
        /// Creates new instance of <see cref="FlagsmithProvider"/>
        /// </summary>
        /// <param name="options">Flagsmith client options. You can just use <see cref="FlagsmithConfiguration"/> class</param>
        /// <param name="httpClient">Http client that will be used for flagsmith requests. You also can use it to register <see cref="FeatureProvider"/> as Typed HttpClient with <see cref="FeatureProvider"> as abstraction</see></param>
        public FlagsmithProvider(IFlagsmithConfiguration options, HttpClient httpClient)
        {
            _flagsmithClient = new FlagsmithClient(options, httpClient);
        }


        /// <summary>
        /// Creates new instance of <see cref="FlagsmithProvider"/>
        /// </summary>
        /// <param name="flagsmithClient">Precreated Flagsmith client. You can just use <see cref="FlagsmithClient"/> class.</param>
        public FlagsmithProvider(IFlagsmithClient flagsmithClient)
        {
            _flagsmithClient = flagsmithClient;
        }

        private Task<IFlags> GetFlags(EvaluationContext ctx = null)
        {
            var key = ctx?.GetValue("targetingKey")?.AsString;
            return string.IsNullOrEmpty(key)
                ? _flagsmithClient.GetEnvironmentFlags()
                : _flagsmithClient.GetIdentityFlags(key, ctx.AsDictionary().Select(x => new Trait(x.Key, x.Value.AsObject) as ITrait).ToList());
        }


        /// <inheritdoc/>
        public override Metadata GetMetadata() => Metadata;

        /// <inheritdoc/>

        public override async Task<ResolutionDetails<bool>> ResolveBooleanValue(string flagKey, bool defaultValue, EvaluationContext context = null)
        {
            var flags = await GetFlags(context);
            var isFlagEnabled = await flags.IsFeatureEnabled(flagKey);
            if (!isFlagEnabled)
            {
                return new ResolutionDetails<bool>(flagKey, defaultValue, reason: Reason.Disabled);
            }

            var stringValue = await flags.GetFeatureValue(flagKey);
            if (bool.TryParse(stringValue, out var parsedValue))
            {
                return new ResolutionDetails<bool>(flagKey, parsedValue);
            }
            throw new TypeMismatchException("Failed to parse value in boolean type");

        }

        /// <inheritdoc/>

        public override async Task<ResolutionDetails<string>> ResolveStringValue(string flagKey, string defaultValue, EvaluationContext context = null)
        {

            var flags = await GetFlags(context);
            var isFlagEnabled = await flags.IsFeatureEnabled(flagKey);
            if (!isFlagEnabled)
            {
                return new ResolutionDetails<string>(flagKey, defaultValue, reason: Reason.Disabled);
            }

            var stringValue = await flags.GetFeatureValue(flagKey);
            return new ResolutionDetails<string>(flagKey, stringValue);
        }

        /// <inheritdoc/>
        public override async Task<ResolutionDetails<int>> ResolveIntegerValue(string flagKey, int defaultValue, EvaluationContext context = null)
        {

            var flags = await GetFlags(context);
            var isFlagEnabled = await flags.IsFeatureEnabled(flagKey);
            if (!isFlagEnabled)
            {
                return new ResolutionDetails<int>(flagKey, defaultValue, reason: Reason.Disabled);
            }

            var stringValue = await flags.GetFeatureValue(flagKey);
            if(int.TryParse(stringValue, out var parsedValue))
            {
                return new ResolutionDetails<int>(flagKey, parsedValue);
            }
            throw new TypeMismatchException("Failed to parse value in int type");
        }

        /// <inheritdoc/>
        public override async Task<ResolutionDetails<double>> ResolveDoubleValue(string flagKey, double defaultValue, EvaluationContext context = null)
        {

            var flags = await GetFlags(context);
            var isFlagEnabled = await flags.IsFeatureEnabled(flagKey);
            if (!isFlagEnabled)
            {
                return new ResolutionDetails<double>(flagKey, defaultValue, reason: Reason.Disabled);
            }

            var stringValue = await flags.GetFeatureValue(flagKey);
            if (double.TryParse(stringValue, out var parsedValue))
            {
                return new ResolutionDetails<double>(flagKey, parsedValue);
            }
            throw new TypeMismatchException("Failed to parse value in double type");
        }


        /// <inheritdoc/>
        public override async Task<ResolutionDetails<Value>> ResolveStructureValue(string flagKey, Value defaultValue, EvaluationContext context = null)
        {

            var flags = await GetFlags(context);
            var isFlagEnabled = await flags.IsFeatureEnabled(flagKey);
            if (!isFlagEnabled)
            {
                return new ResolutionDetails<Value>(flagKey, defaultValue, reason: Reason.Disabled);
            }
            var stringValue = await flags.GetFeatureValue(flagKey);

            try
            {
                var mappedValue = JsonNode.Parse(stringValue);
                var value = ConvertValue(mappedValue);
                if (value is not null)
                {
                    return new ResolutionDetails<Value>(flagKey, value);

                }
            }
            catch(Exception ex)
            {
                throw new TypeMismatchException("Failed to parse value in structure type", ex);
            }
            throw new TypeMismatchException("Failed to parse value in structure type");
        }

        /// <summary>
        ///     convertValue is converting the object return by the proxy response in the right type.
        /// </summary>
        /// <param name="node">The value we have received</param>
        /// <returns>A converted object</returns>
        private Value ConvertValue(JsonNode node)
        {
            if(node == null)
                return null;
            if (node is JsonArray jsonArray)
            {
                var arr = new List<Value>();
                foreach (var item in jsonArray)
                {
                    var convertedValue = ConvertValue(item);
                    if (convertedValue != null) arr.Add(convertedValue);
                }
                return new Value(arr);
            }

            if (node is JsonObject jsonObject)
            {
                var dict = jsonObject.ToDictionary(x => x.Key, x => ConvertValue(x.Value));

                return new Value(new Structure(dict));
            }

            if (node.AsValue().TryGetValue<JsonElement>(out var jsonElement))
            {
                if (jsonElement.ValueKind == JsonValueKind.False || jsonElement.ValueKind == JsonValueKind.True)
                    return new Value(jsonElement.GetBoolean());
                if (jsonElement.ValueKind == JsonValueKind.Number)
                    return new Value(jsonElement.GetDouble());

                if (jsonElement.ValueKind == JsonValueKind.String)
                    return new Value(jsonElement.ToString());
            }
            return null;
        }
    }
}