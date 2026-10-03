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

    /// <summary>
    /// Parses the raw string Flagsmith stores for a feature into the type the caller asked for.
    /// Mirrors the shape of <c>bool.TryParse</c> so the framework parsers can be passed directly.
    /// </summary>
    /// <typeparam name="T">The type the flag value is resolved as.</typeparam>
    /// <param name="value">The raw feature value returned by Flagsmith.</param>
    /// <param name="x">The parsed value, or the default of <typeparamref name="T"/> when parsing fails.</param>
    /// <returns><c>true</c> when <paramref name="value"/> was parsed successfully.</returns>
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

    /// <summary>
    /// Fetches the flags that apply to an evaluation context. A context carrying a targeting key is
    /// resolved as a Flagsmith identity, with the remaining context attributes sent as that identity's
    /// traits; without one the environment flags are used.
    /// </summary>
    /// <param name="ctx">The evaluation context, which may be <c>null</c>.</param>
    /// <returns>The flags Flagsmith resolved for the context.</returns>
    private Task<IFlags> GetFlags(EvaluationContext ctx)
    {
        var identifier = ctx?.TargetingKey;

        if (string.IsNullOrWhiteSpace(identifier))
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

    /// <summary>
    /// Resolves the configured value of a feature. A feature that is turned off in Flagsmith resolves to
    /// <paramref name="defaultValue"/> with <see cref="Reason.Disabled"/> rather than to its stored value.
    /// </summary>
    /// <typeparam name="T">The type the flag value is resolved as.</typeparam>
    /// <param name="flagKey">The key of the feature to resolve.</param>
    /// <param name="defaultValue">The value to fall back to when the feature is disabled.</param>
    /// <param name="tryParse">Parser turning the stored string into <typeparamref name="T"/>.</param>
    /// <param name="context">The evaluation context, which may be <c>null</c>.</param>
    /// <returns>The resolution details for the feature.</returns>
    /// <exception cref="TypeMismatchException">The stored value cannot be parsed as <typeparamref name="T"/>.</exception>
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

    /// <summary>
    /// Resolves a feature from its enabled state rather than from its configured value, which is how a
    /// boolean flag is evaluated unless <see cref="IFlagsmithProviderConfiguration.UsingBooleanConfigValue"/>
    /// is set.
    /// </summary>
    /// <param name="flagKey">The key of the feature to resolve.</param>
    /// <param name="context">The evaluation context, which may be <c>null</c>.</param>
    /// <returns>The resolution details holding whether the feature is enabled.</returns>
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

    /// <summary>
    /// Parses a feature value holding a JSON document into a <see cref="Value"/>. Anything that is not
    /// usable JSON is reported as a failed parse rather than as an exception, so that
    /// <see cref="ResolveValue{T}"/> can turn it into a <see cref="TypeMismatchException"/>.
    /// </summary>
    /// <param name="stringValue">The raw feature value returned by Flagsmith.</param>
    /// <param name="result">The converted value, or <c>null</c> when the input is not usable JSON.</param>
    /// <returns><c>true</c> when <paramref name="stringValue"/> was converted successfully.</returns>
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
        // JsonException: the value is not well-formed JSON.
        // FormatException: a number is well-formed but not representable as a double, which
        // JsonElement.GetDouble reports by throwing on the .NET Framework / netstandard2.0 builds.
        catch (Exception e) when (e is JsonException or FormatException)
        {
            result = null;
        }
        return result is not null;
    }

    /// <summary>
    /// Converts the dynamically typed JSON received from Flagsmith into the matching OpenFeature type,
    /// recursing through arrays and objects.
    /// </summary>
    /// <param name="node">The dynamically typed value we received from Flagsmith.</param>
    /// <returns>
    /// A correctly typed object representing the flag value, or <c>null</c> for a JSON null and for a
    /// scalar that cannot be represented, such as a number outside the range of a double.
    /// </returns>
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
