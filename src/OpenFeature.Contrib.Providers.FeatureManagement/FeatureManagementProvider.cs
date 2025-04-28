using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using OpenFeature.Constant;
using OpenFeature.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenFeature.Contrib.Providers.FeatureManagement;

/// <summary>
/// OpenFeature provider using the Microsoft.FeatureManagement library
/// </summary>
public sealed class FeatureManagementProvider : FeatureProvider
{
    private readonly Metadata metadata = new Metadata("FeatureManagement Provider");
    private readonly IVariantFeatureManager featureManager;

    /// <summary>
    /// Create a new instance of the FeatureManagementProvider
    /// </summary>
    /// <param name="configuration">Provide the Configuration to use as the feature flags.</param>
    /// <param name="options">Provide specific FeatureManagementOptions</param>
    public FeatureManagementProvider(IConfiguration configuration, FeatureManagementOptions options)
    {
        featureManager = new FeatureManager(
            new ConfigurationFeatureDefinitionProvider(configuration),
            options
        );
    }

    /// <summary>
    /// Create a new instance of the FeatureManagementProvider
    /// </summary>
    /// <param name="configuration">Provide the Configuration to use as the feature flags.</param>
    public FeatureManagementProvider(IConfiguration configuration) : this(configuration, new FeatureManagementOptions())
    {
    }

    /// <summary>
    /// Return the Metadata associated with this provider.
    /// </summary>
    /// <returns>Metadata</returns>
    public override Metadata GetMetadata() => metadata;

    /// <inheritdoc />
    public override async Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(string flagKey, bool defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
    {
        var variant = await Evaluate(flagKey, context, CancellationToken.None).ConfigureAwait(false);

        if (variant == null)
        {
            var exists = false;
            await foreach (var name in featureManager.GetFeatureNamesAsync().WithCancellation(cancellationToken))
            {
                if (!flagKey.Equals(name, StringComparison.OrdinalIgnoreCase)) continue;
                exists = true;
                break;
            }
            var enabled = await featureManager.IsEnabledAsync(flagKey, context, cancellationToken).ConfigureAwait(false);
            if (exists)
            {
                return new ResolutionDetails<bool>(flagKey, enabled);
            }

            return new ResolutionDetails<bool>(flagKey, defaultValue, ErrorType.FlagNotFound, Reason.Error);
        }

        if (Boolean.TryParse(variant?.Configuration?.Value, out var value))
            return new ResolutionDetails<bool>(flagKey, value);
        return new ResolutionDetails<bool>(flagKey, defaultValue);
    }

    /// <inheritdoc />
    public override async Task<ResolutionDetails<double>> ResolveDoubleValueAsync(string flagKey, double defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
    {
        var variant = await Evaluate(flagKey, context, CancellationToken.None).ConfigureAwait(false);

        if (Double.TryParse(variant?.Configuration?.Value, out var value))
            return new ResolutionDetails<double>(flagKey, value);

        return new ResolutionDetails<double>(flagKey, defaultValue);
    }

    /// <inheritdoc />
    public override async Task<ResolutionDetails<int>> ResolveIntegerValueAsync(string flagKey, int defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
    {
        var variant = await Evaluate(flagKey, context, CancellationToken.None).ConfigureAwait(false);

        if (int.TryParse(variant?.Configuration?.Value, out var value))
            return new ResolutionDetails<int>(flagKey, value);

        return new ResolutionDetails<int>(flagKey, defaultValue);
    }

    /// <inheritdoc />
    public override async Task<ResolutionDetails<string>> ResolveStringValueAsync(string flagKey, string defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
    {
        var variant = await Evaluate(flagKey, context, CancellationToken.None).ConfigureAwait(false);

        if (string.IsNullOrEmpty(variant?.Configuration?.Value))
            return new ResolutionDetails<string>(flagKey, defaultValue);

        return new ResolutionDetails<string>(flagKey, variant.Configuration.Value);
    }

    /// <inheritdoc />
    public override async Task<ResolutionDetails<Value>> ResolveStructureValueAsync(string flagKey, Value defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
    {
        var variant = await Evaluate(flagKey, context, CancellationToken.None).ConfigureAwait(false);

        if (variant == null)
            return new ResolutionDetails<Value>(flagKey, defaultValue);

        Value parsedVariant = ParseVariant(variant);
        return new ResolutionDetails<Value>(flagKey, parsedVariant);
    }

    /// <inheritdoc />
    private ValueTask<Variant> Evaluate(string flagKey, EvaluationContext evaluationContext, CancellationToken cancellationToken)
    {
        TargetingContext targetingContext = ConvertContext(evaluationContext);
        if (targetingContext != null)
            return featureManager.GetVariantAsync(flagKey, targetingContext, cancellationToken);
        return featureManager.GetVariantAsync(flagKey, CancellationToken.None);
    }

    /// <summary>
    /// Converts the OpenFeature EvaluationContext to the Microsoft.FeatureManagement TargetingContext
    /// </summary>
    /// <param name="evaluationContext"></param>
    /// <returns></returns>
    private TargetingContext ConvertContext(EvaluationContext evaluationContext)
    {
        if (evaluationContext == null)
            return null;

        TargetingContext targetingContext = new TargetingContext();

        if (evaluationContext.ContainsKey(nameof(targetingContext.UserId)))
        {
            Value userId = evaluationContext.GetValue(nameof(targetingContext.UserId));
            if (userId.IsString) targetingContext.UserId = userId.AsString;
        }

        if (evaluationContext.ContainsKey(nameof(targetingContext.Groups)))
        {
            Value groups = evaluationContext.GetValue(nameof(targetingContext.Groups));
            if (groups.IsList)
            {
                List<string> groupList = new List<string>();
                foreach (var group in groups.AsList)
                {
                    if (group.IsString) groupList.Add(group.AsString);
                }
                targetingContext.Groups = groupList;
            }
        }

        return targetingContext;
    }

    /// <summary>
    /// Parses an Microsoft.FeatureManagement Variant into an OpenFeature Value
    /// </summary>
    /// <param name="variant"></param>
    /// <returns></returns>
    private Value ParseVariant(Variant variant)
    {
        if (variant == null || variant.Configuration == null)
            return null;

        if (variant.Configuration.Value == null)
            return ParseChildren(variant.Configuration.GetChildren());

        return ParseUnknownType(variant.Configuration.Value);
    }

    /// <summary>
    /// Iterataes over a Variants configuration to parse it into an OpenFeature Value
    /// </summary>
    /// <param name="children"></param>
    /// <returns></returns>
    private Value ParseChildren(IEnumerable<IConfigurationSection> children)
    {
        IDictionary<string, Value> keyValuePairs = new Dictionary<string, Value>();
        if (children == null) return null;
        foreach (var child in children)
        {
            if (child.Value != null)
                keyValuePairs.Add(child.Key, ParseUnknownType(child.Value));
            if (child.GetChildren().Any())
                keyValuePairs.Add(child.Key, ParseChildren(child.GetChildren()));
        }
        return new Value(new Structure(keyValuePairs));
    }

    /// <summary>
    /// Attempts to parse an arbitrary string value through a set of parsable types
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    private Value ParseUnknownType(string value)
    {
        if (bool.TryParse(value, out bool boolResult))
            return new Value(boolResult);
        if (double.TryParse(value, out double doubleResult))
            return new Value(doubleResult);
        if (int.TryParse(value, out int intResult))
            return new Value(intResult);
        if (DateTime.TryParse(value, out DateTime dateTimeResult))
            return new Value(dateTimeResult);

        return new Value(value);
    }
}
