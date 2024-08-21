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
using System.Globalization;
using System.Threading;

namespace OpenFeature.Contrib.Providers.Flagsmith
{
    /// <summary>
    /// FlagsmithProvider is the .NET provider implementation for the feature flag solution Flagsmith.
    /// </summary>
    public class FlagsmithProvider : FeatureProvider
    {
        private readonly static Metadata Metadata = new("Flagsmith Provider");
        delegate bool TryParseDelegate<T>(string value, out T x);
        internal readonly IFlagsmithClient _flagsmithClient;

        /// <summary>
        /// Settings for Flagsmith Open feature provider
        /// </summary>
        public IFlagsmithProviderConfiguration Configuration { get; }


        /// <summary>
        /// Creates new instance of <see cref="FlagsmithProvider"/>
        /// </summary>
        /// <param name="providerOptions">Open feature provider options. You can just use <see cref="FlagsmithProviderConfiguration"/> class </param>
        /// <param name="flagsmithOptions">Flagsmith client options. You can just use <see cref="FlagsmithConfiguration"/> class</param>
        public FlagsmithProvider(IFlagsmithProviderConfiguration providerOptions, IFlagsmithConfiguration flagsmithOptions)
        {
            Configuration = providerOptions;
            _flagsmithClient = new FlagsmithClient(flagsmithOptions);
        }

        /// <summary>
        /// Creates new instance of <see cref="FlagsmithProvider"/>
        /// </summary>
        /// <param name="flagsmithOptions">Flagsmith client options. You can just use <see cref="FlagsmithConfiguration"/> class</param>
        /// <param name="providerOptions">Open feature provider options. You can just use <see cref="FlagsmithProviderConfiguration"/> class </param>
        /// <param name="httpClient">Http client that will be used for flagsmith requests. You also can use it to register <see cref="FeatureProvider"/> as Typed HttpClient with <see cref="FeatureProvider"> as abstraction</see></param>
        public FlagsmithProvider(IFlagsmithProviderConfiguration providerOptions, IFlagsmithConfiguration flagsmithOptions, HttpClient httpClient)
        {
            Configuration = providerOptions;
            _flagsmithClient = new FlagsmithClient(flagsmithOptions, httpClient);
        }


        /// <summary>
        /// Creates new instance of <see cref="FlagsmithProvider"/>
        /// </summary>
        /// <param name="providerOptions">Open feature provider options. You can just use <see cref="FlagsmithProviderConfiguration"/> class </param>
        /// <param name="flagsmithClient">Precreated Flagsmith client. You can just use <see cref="FlagsmithClient"/> class.</param>
        public FlagsmithProvider(IFlagsmithProviderConfiguration providerOptions, IFlagsmithClient flagsmithClient)
        {
            Configuration = providerOptions;
            _flagsmithClient = flagsmithClient;
        }

        private Task<IFlags> GetFlags(EvaluationContext ctx)
        {
            var key = ctx?.TargetingKey;

            return string.IsNullOrEmpty(key)
                ? _flagsmithClient.GetEnvironmentFlags()
                : _flagsmithClient.GetIdentityFlags(key, ctx
                    .AsDictionary()
                    .Select(x => new Trait(x.Key, x.Value.AsObject) as ITrait)
                    .ToList());
        }

        private async Task<ResolutionDetails<T>> ResolveValue<T>(string flagKey, T defaultValue, TryParseDelegate<T> tryParse, EvaluationContext context)
        {

            var flags = await GetFlags(context);
            var isFlagEnabled = await flags.IsFeatureEnabled(flagKey);
            if (!isFlagEnabled)
            {
                return new(flagKey, defaultValue, reason: Reason.Disabled);
            }

            var stringValue = await flags.GetFeatureValue(flagKey);

            if (tryParse(stringValue, out var parsedValue))
            {
                return new(flagKey, parsedValue);
            }
            throw new TypeMismatchException("Failed to parse value in the expected type");

        }

        private async Task<ResolutionDetails<bool>> IsFeatureEnabled(string flagKey, EvaluationContext context)
        {
            var flags = await GetFlags(context);
            var isFeatureEnabled = await flags.IsFeatureEnabled(flagKey);
            return new(flagKey, isFeatureEnabled);
        }


        /// <inheritdoc/>
        public override Metadata GetMetadata() => Metadata;

        /// <inheritdoc/>

        public override Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(string flagKey, bool defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
            => Configuration.UsingBooleanConfigValue
            ? ResolveValue(flagKey, defaultValue, bool.TryParse, context)
            : IsFeatureEnabled(flagKey, context);

        /// <inheritdoc/>
        public override Task<ResolutionDetails<int>> ResolveIntegerValueAsync(string flagKey, int defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
            => ResolveValue(flagKey, defaultValue, int.TryParse, context);

        /// <inheritdoc/>
        public override Task<ResolutionDetails<double>> ResolveDoubleValueAsync(string flagKey, double defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
            => ResolveValue(flagKey, defaultValue, (string x, out double y) => double.TryParse(x, NumberStyles.Any, CultureInfo.InvariantCulture, out y), context);


        /// <inheritdoc/>
        public override Task<ResolutionDetails<string>> ResolveStringValueAsync(string flagKey, string defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
            => ResolveValue(flagKey, defaultValue, (string x, out string y) => { y = x; return true; }, context);


        /// <inheritdoc/>
        public override Task<ResolutionDetails<Value>> ResolveStructureValueAsync(string flagKey, Value defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
            => ResolveValue(flagKey, defaultValue, TryParseValue, context);

        private bool TryParseValue(string stringValue, out Value result)
        {
            try
            {
                var mappedValue = JsonNode.Parse(stringValue);
                result = ConvertValue(mappedValue);
            }
            catch
            {
                result = null;
            }
            return result is not null;
        }

        /// <summary>
        ///     convertValue is converting the dynamically typed object received from Flagsmith into the correct type
        /// </summary>
        /// <param name="node">The dynamically typed value we received from Flagsmith</param>
        /// <returns>A correctly typed object representing the flag value</returns>
        private Value ConvertValue(JsonNode node)
        {
            if (node == null)
                return null;
            if (node is JsonArray jsonArray)
            {
                var arr = new List<Value>();
                foreach (var item in jsonArray)
                {
                    var convertedValue = ConvertValue(item);
                    if (convertedValue != null) arr.Add(convertedValue);
                }
                return new(arr);
            }

            if (node is JsonObject jsonObject)
            {
                var dict = jsonObject.ToDictionary(x => x.Key, x => ConvertValue(x.Value));

                return new(new Structure(dict));
            }

            if (node.AsValue().TryGetValue<JsonElement>(out var jsonElement))
            {
                if (jsonElement.ValueKind == JsonValueKind.False || jsonElement.ValueKind == JsonValueKind.True)
                    return new(jsonElement.GetBoolean());
                if (jsonElement.ValueKind == JsonValueKind.Number)
                    return new(jsonElement.GetDouble());

                if (jsonElement.ValueKind == JsonValueKind.String)
                    return new(jsonElement.ToString());
            }
            return null;
        }
    }
}