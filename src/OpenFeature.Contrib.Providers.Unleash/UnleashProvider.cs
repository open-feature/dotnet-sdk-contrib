using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Constant;
using OpenFeature.Error;
using OpenFeature.Model;
using Unleash;
using Unleash.Internal;

namespace OpenFeature.Contrib.Providers.Unleash;

/// <summary>
/// UnleashProvider is the OpenFeature .NET provider implementation for the Unleash feature flag system.
/// </summary>
public class UnleashProvider : FeatureProvider
{
    private static readonly Metadata ProviderMetadata = new("Unleash Provider");

    private readonly IUnleash _unleash;
    private readonly bool _ownsUnleash;

    /// <summary>
    /// Creates a new UnleashProvider wrapping an existing IUnleash instance.
    /// The caller is responsible for disposing the IUnleash instance.
    /// </summary>
    /// <param name="unleash">An existing Unleash client instance.</param>
    public UnleashProvider(IUnleash unleash)
    {
        this._unleash = unleash ?? throw new ArgumentNullException(nameof(unleash));
        this._ownsUnleash = false;
    }

    /// <summary>
    /// Creates a new UnleashProvider that owns and manages an Unleash client created from the given settings.
    /// The provider will dispose the client on shutdown.
    /// </summary>
    /// <param name="settings">Unleash settings used to create the client.</param>
    public UnleashProvider(UnleashSettings settings)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));
        this._unleash = new DefaultUnleash(settings);
        this._ownsUnleash = true;
    }

    /// <inheritdoc />
    public override Metadata GetMetadata() => ProviderMetadata;

    /// <inheritdoc />
    public override Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(
        string flagKey, bool defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
    {
        var unleashContext = ContextTransformer.Transform(context);
        var result = this._unleash.IsEnabled(flagKey, unleashContext, defaultValue);

        return Task.FromResult(new ResolutionDetails<bool>(flagKey, result));
    }

    /// <inheritdoc />
    public override Task<ResolutionDetails<string>> ResolveStringValueAsync(
        string flagKey, string defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
    {
        var resolution = this.EvaluateVariant(flagKey, context);

        if (resolution == null)
        {
            return Task.FromResult(new ResolutionDetails<string>(flagKey, defaultValue, reason: Reason.Default));
        }

        var value = resolution.Value.PayloadValue ?? defaultValue;
        return Task.FromResult(new ResolutionDetails<string>(flagKey, value, variant: resolution.Value.VariantName));
    }

    /// <inheritdoc />
    public override Task<ResolutionDetails<int>> ResolveIntegerValueAsync(
        string flagKey, int defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
    {
        var resolution = this.EvaluateVariant(flagKey, context);

        if (resolution == null)
        {
            return Task.FromResult(new ResolutionDetails<int>(flagKey, defaultValue, reason: Reason.Default));
        }

        if (int.TryParse(resolution.Value.PayloadValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            return Task.FromResult(new ResolutionDetails<int>(flagKey, parsed, variant: resolution.Value.VariantName));
        }

        throw new TypeMismatchException($"Cannot parse variant payload '{resolution.Value.PayloadValue}' as integer for flag '{flagKey}'.");
    }

    /// <inheritdoc />
    public override Task<ResolutionDetails<double>> ResolveDoubleValueAsync(
        string flagKey, double defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
    {
        var resolution = this.EvaluateVariant(flagKey, context);

        if (resolution == null)
        {
            return Task.FromResult(new ResolutionDetails<double>(flagKey, defaultValue, reason: Reason.Default));
        }

        if (double.TryParse(resolution.Value.PayloadValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            return Task.FromResult(new ResolutionDetails<double>(flagKey, parsed, variant: resolution.Value.VariantName));
        }

        throw new TypeMismatchException($"Cannot parse variant payload '{resolution.Value.PayloadValue}' as double for flag '{flagKey}'.");
    }

    /// <inheritdoc />
    public override Task<ResolutionDetails<Value>> ResolveStructureValueAsync(
        string flagKey, Value defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
    {
        var resolution = this.EvaluateVariant(flagKey, context);

        if (resolution == null)
        {
            return Task.FromResult(new ResolutionDetails<Value>(flagKey, defaultValue, reason: Reason.Default));
        }

        var value = resolution.Value.PayloadValue != null
            ? new Value(resolution.Value.PayloadValue)
            : defaultValue;

        return Task.FromResult(new ResolutionDetails<Value>(flagKey, value, variant: resolution.Value.VariantName));
    }

    /// <summary>
    /// Shuts down the provider, disposing the Unleash client if owned by this provider.
    /// </summary>
    public void Shutdown()
    {
        if (this._ownsUnleash)
        {
            this._unleash.Dispose();
        }
    }

    private VariantResolution? EvaluateVariant(string flagKey, EvaluationContext context)
    {
        var unleashContext = ContextTransformer.Transform(context);
        var variant = this._unleash.GetVariant(flagKey, unleashContext);

        if (variant == null || variant.Name == Variant.DISABLED_VARIANT.Name)
        {
            return null;
        }

        return new VariantResolution(variant.Name, variant.Payload?.Value);
    }

    private readonly struct VariantResolution
    {
        public string VariantName { get; }
        public string PayloadValue { get; }

        public VariantResolution(string variantName, string payloadValue)
        {
            this.VariantName = variantName;
            this.PayloadValue = payloadValue;
        }
    }
}
