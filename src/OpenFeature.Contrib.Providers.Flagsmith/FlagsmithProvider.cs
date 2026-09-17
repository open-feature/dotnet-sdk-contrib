using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Flagsmith;
using OpenFeature.Constant;
using OpenFeature.Error;
using OpenFeature.Model;
using Trait = Flagsmith.Trait;

namespace OpenFeature.Contrib.Providers.Flagsmith;

/// <summary>
/// FlagsmithProvider is the .NET provider implementation for the feature flag solution Flagsmith.
/// </summary>
public class FlagsmithProvider : FeatureProvider
{
    private static readonly Metadata Metadata = new("Flagsmith Provider");

    /// <summary>
    /// Key under which the OpenFeature SDK stores the targeting key inside the evaluation context.
    /// </summary>
    private const string TargetingKeyAttribute = "targetingKey";

    private delegate bool TryParseDelegate<T>(string value, out T x);

    internal readonly IFlagsmithClient _flagsmithClient;

    /// <summary>
    /// Settings for Flagsmith Open feature provider
    /// </summary>
    public IFlagsmithProviderConfiguration Configuration { get; }


    /// <summary>
    /// Creates new instance of <see cref="FlagsmithProvider"/>
    /// </summary>
    /// <param name="providerOptions">Open feature provider options. You can just use <see cref="FlagsmithProviderConfiguration"/> class </param>
    /// <param name="flagsmithOptions">Flagsmith client options.</param>
    public FlagsmithProvider(IFlagsmithProviderConfiguration providerOptions, FlagsmithConfiguration flagsmithOptions)
    {
        Configuration = providerOptions;
        _flagsmithClient = new FlagsmithClient(flagsmithOptions);
    }

    /// <summary>
    /// Creates new instance of <see cref="FlagsmithProvider"/>
    /// </summary>
    /// <param name="flagsmithOptions">Flagsmith client options.</param>
    /// <param name="providerOptions">Open feature provider options. You can just use <see cref="FlagsmithProviderConfiguration"/> class </param>
    /// <param name="httpClient">Http client that will be used for flagsmith requests. You also can use it to register <see cref="FeatureProvider"/> as Typed HttpClient with <see cref="FeatureProvider"> as abstraction</see></param>
    public FlagsmithProvider(IFlagsmithProviderConfiguration providerOptions, FlagsmithConfiguration flagsmithOptions, HttpClient httpClient)
    {
        Configuration = providerOptions;
        flagsmithOptions.HttpClient = httpClient;
        _flagsmithClient = new FlagsmithClient(flagsmithOptions);
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
        var identifier = ctx?.TargetingKey;

        if (string.IsNullOrEmpty(identifier))
        {
            return _flagsmithClient.GetEnvironmentFlags();
        }

        // The targeting key identifies the Flagsmith identity, it is not a trait of that identity,
        // so it must not be sent along with the traits.
        var traits = ctx
            .AsDictionary()
            .Where(x => !string.Equals(x.Key, TargetingKeyAttribute, StringComparison.Ordinal))
            .Select(x => new Trait(x.Key, x.Value.AsObject) as ITrait)
            .ToList();

        return _flagsmithClient.GetIdentityFlags(identifier, traits);
    }

    private async Task<ResolutionDetails<T>> ResolveValue<T>(string flagKey, T defaultValue, TryParseDelegate<T> tryParse, EvaluationContext context)
    {
        var flags = await GetFlags(context).ConfigureAwait(false);
        var isFlagEnabled = await flags.IsFeatureEnabled(flagKey).ConfigureAwait(false);
        if (!isFlagEnabled)
        {
            return new(flagKey, defaultValue, reason: Reason.Disabled);
        }

        var stringValue = await flags.GetFeatureValue(flagKey).ConfigureAwait(false);

        return tryParse(stringValue, out var parsedValue)
            ? new(flagKey, parsedValue)
            : throw new TypeMismatchException("Failed to parse value in the expected type");
    }

    private async Task<ResolutionDetails<bool>> IsFeatureEnabled(string flagKey, EvaluationContext context)
    {
        var flags = await GetFlags(context).ConfigureAwait(false);
        var isFeatureEnabled = await flags.IsFeatureEnabled(flagKey).ConfigureAwait(false);
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

    private static bool TryParseValue(string stringValue, out Value result)
    {
        if (string.IsNullOrWhiteSpace(stringValue))
        {
            result = null;
            return false;
        }

        try
        {
            result = ConvertValue(JsonNode.Parse(stringValue));
        }
        catch (JsonException)
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
    private static Value ConvertValue(JsonNode node)
    {
        switch (node)
        {
            case null:
                return null;
            case JsonArray jsonArray:
            {
                var arr = jsonArray.Select(ConvertValue).Where(convertedValue => convertedValue != null).ToList();
                return new(arr);
            }
            case JsonObject jsonObject:
            {
                var dict = jsonObject.ToDictionary(x => x.Key, x => ConvertValue(x.Value));

                return new(new Structure(dict));
            }
        }

        if (!node.AsValue().TryGetValue<JsonElement>(out var jsonElement))
        {
            return null;
        }

        return jsonElement.ValueKind switch
        {
            JsonValueKind.False or JsonValueKind.True => new(jsonElement.GetBoolean()),
            JsonValueKind.Number => new(jsonElement.GetDouble()),
            JsonValueKind.String => new(jsonElement.ToString()),
            _ => null
        };
    }
}
