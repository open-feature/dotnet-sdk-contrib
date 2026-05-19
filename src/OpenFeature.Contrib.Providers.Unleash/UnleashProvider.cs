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
/// <remarks>
/// This provider creates and owns its own DefaultUnleash client instance rather than accepting
/// an external IUnleash instance. This is because the Unleash SDK only allows subscribing to
/// lifecycle events (ReadyEvent, ErrorEvent) during client construction. Without access to these
/// events, the provider cannot properly implement the OpenFeature initialization lifecycle
/// (i.e., signaling readiness only after toggles have been fetched).
/// </remarks>
public class UnleashProvider : FeatureProvider
{
    private static readonly Metadata ProviderMetadata = new("Unleash Provider");

    private readonly UnleashSettings _settings;
    private IUnleash _unleash;

    /// <summary>
    /// Creates a new UnleashProvider that will create and own a DefaultUnleash client.
    /// The client is created during <see cref="InitializeAsync"/> and disposed during <see cref="ShutdownAsync"/>.
    /// </summary>
    /// <param name="settings">Unleash settings used to create the client.</param>
    public UnleashProvider(UnleashSettings settings)
    {
        this._settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <inheritdoc />
    public override Metadata GetMetadata() => ProviderMetadata;

    /// <inheritdoc />
    /// <remarks>
    /// When <see cref="UnleashSettings.ToggleBootstrapProvider"/> is configured, the provider
    /// completes initialization immediately after client construction since bootstrap data is
    /// loaded synchronously. Otherwise, initialization waits for the first successful HTTP fetch
    /// (ReadyEvent) or propagates any initialization error (ErrorEvent).
    /// </remarks>
    public override Task InitializeAsync(EvaluationContext context, CancellationToken cancellationToken = default)
    {
        if (this._settings.ToggleBootstrapProvider != null)
        {
            this._unleash = new DefaultUnleash(this._settings, callback =>
            {
                callback.TogglesUpdatedEvent = _ => this.EmitConfigurationChanged();
            });
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        using var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

        this._unleash = new DefaultUnleash(this._settings, callback =>
        {
            callback.ReadyEvent = _ => tcs.TrySetResult(true);
            callback.ErrorEvent = evt => tcs.TrySetException(
                evt.Error ?? new Exception($"Unleash initialization failed: {evt.ErrorType}"));
            callback.TogglesUpdatedEvent = _ => this.EmitConfigurationChanged();
        });

        return tcs.Task;
    }

    internal void EmitConfigurationChanged()
    {
        this.EventChannel.Writer.TryWrite(new ProviderEventPayload
        {
            Type = ProviderEventTypes.ProviderConfigurationChanged,
            ProviderName = ProviderMetadata.Name
        });
    }

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

    /// <inheritdoc />
    public override Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        this._unleash?.Dispose();
        return Task.CompletedTask;
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
