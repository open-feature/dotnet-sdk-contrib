using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Constant;
using OpenFeature.Error;
using OpenFeature.Model;
using Unleash;
using Unleash.Events;
using Unleash.Internal;
using ErrorType = OpenFeature.Constant.ErrorType;

namespace OpenFeature.Providers.Unleash;

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
public sealed class UnleashProvider : FeatureProvider
{
    private static readonly Metadata ProviderMetadata = new("Unleash Provider");

    private readonly UnleashSettings _settings;
    private IUnleash? _unleash;

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
    /// (ReadyEvent) or first error (ErrorEvent) or cancellation.
    /// </remarks>
    public override Task InitializeAsync(EvaluationContext context, CancellationToken cancellationToken = default)
    {
        // Apply context fields to UnleashSettings where applicable
        var appName = context.GetAppName();
        if (!string.IsNullOrWhiteSpace(appName))
        {
            this._settings.AppName = appName;
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        if (this._settings.ToggleBootstrapProvider != null)
        {
            Volatile.Write(ref this._unleash, new DefaultUnleash(this._settings, callback =>
            {
                callback.TogglesUpdatedEvent = _ => this.EmitConfigurationChanged();
            }));
            tcs.TrySetResult();
            return Task.CompletedTask;
        }

        Volatile.Write(ref this._unleash, new DefaultUnleash(this._settings, callback =>
        {
            callback.ReadyEvent = _ =>
            {
                tcs.TrySetResult();
            };
            callback.ErrorEvent = evt =>
            {
                this.EmitProviderError(evt);
                tcs.TrySetResult();
            };
            callback.TogglesUpdatedEvent = _ => this.EmitConfigurationChanged();
        }));

        var registration = cancellationToken.Register(() => tcs.TrySetResult());
        tcs.Task.ContinueWith(_ => registration.Dispose(), TaskContinuationOptions.ExecuteSynchronously);

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

    internal void EmitProviderError(ErrorEvent evt)
    {
        this.EventChannel.Writer.TryWrite(new ProviderEventPayload
        {
            Type = ProviderEventTypes.ProviderError,
            ProviderName = ProviderMetadata.Name,
            Message = evt.Error?.Message ?? $"Unleash error: {evt.ErrorType}"
        });
    }

    /// <inheritdoc />
    public override Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(
        string flagKey, bool defaultValue, EvaluationContext? context = null, CancellationToken cancellationToken = default)
    {
        var unleashContext = context.ToUnleashContext();

        if (this._unleash == null)
        {
            throw new ProviderNotReadyException("Unleash not ready!");
        }

        var result = this._unleash.IsEnabled(flagKey, unleashContext, defaultValue);

        return Task.FromResult(new ResolutionDetails<bool>(flagKey, result));
    }

    /// <inheritdoc />
    public override Task<ResolutionDetails<string>> ResolveStringValueAsync(
        string flagKey, string defaultValue, EvaluationContext? context = null, CancellationToken cancellationToken = default)
    {
        var resolution = this.EvaluateVariant(flagKey, context);

        if (resolution == null)
        {
            return Task.FromResult(new ResolutionDetails<string>(flagKey, defaultValue, reason: Reason.Disabled));
        }

        var value = resolution.Value.PayloadValue ?? defaultValue;
        return Task.FromResult(new ResolutionDetails<string>(flagKey, value, variant: resolution.Value.VariantName, flagMetadata: resolution.Value.GetFlagMetadata()));
    }

    /// <inheritdoc />
    public override Task<ResolutionDetails<int>> ResolveIntegerValueAsync(
        string flagKey, int defaultValue, EvaluationContext? context = null, CancellationToken cancellationToken = default)
    {
        var resolution = this.EvaluateVariant(flagKey, context);

        if (resolution == null)
        {
            return Task.FromResult(new ResolutionDetails<int>(flagKey, defaultValue, reason: Reason.Disabled));
        }

        if (int.TryParse(resolution.Value.PayloadValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            return Task.FromResult(new ResolutionDetails<int>(flagKey, parsed, variant: resolution.Value.VariantName, flagMetadata: resolution.Value.GetFlagMetadata()));
        }

        return Task.FromResult(new ResolutionDetails<int>(
            flagKey,
            defaultValue,
            reason: Reason.Error,
            errorType: ErrorType.TypeMismatch,
            errorMessage: $"Cannot parse variant payload '{resolution.Value.PayloadValue}' as integer for flag '{flagKey}'."));
    }

    /// <inheritdoc />
    public override Task<ResolutionDetails<double>> ResolveDoubleValueAsync(
        string flagKey, double defaultValue, EvaluationContext? context = null, CancellationToken cancellationToken = default)
    {
        var resolution = this.EvaluateVariant(flagKey, context);

        if (resolution == null)
        {
            return Task.FromResult(new ResolutionDetails<double>(flagKey, defaultValue, reason: Reason.Disabled));
        }

        if (double.TryParse(resolution.Value.PayloadValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            return Task.FromResult(new ResolutionDetails<double>(flagKey, parsed, variant: resolution.Value.VariantName, flagMetadata: resolution.Value.GetFlagMetadata()));
        }

        return Task.FromResult(new ResolutionDetails<double>(
            flagKey,
            defaultValue,
            reason: Reason.Error,
            errorType: ErrorType.TypeMismatch,
            errorMessage: $"Cannot parse variant payload '{resolution.Value.PayloadValue}' as double for flag '{flagKey}'."));
    }

    /// <inheritdoc />
    public override Task<ResolutionDetails<Value>> ResolveStructureValueAsync(
        string flagKey, Value defaultValue, EvaluationContext? context = null, CancellationToken cancellationToken = default)
    {
        var resolution = this.EvaluateVariant(flagKey, context);

        if (resolution == null)
        {
            return Task.FromResult(new ResolutionDetails<Value>(flagKey, defaultValue, reason: Reason.Disabled));
        }

        var value = resolution.Value.PayloadValue != null
            ? new Value(resolution.Value.PayloadValue)
            : defaultValue;

        return Task.FromResult(new ResolutionDetails<Value>(flagKey, value, variant: resolution.Value.VariantName, flagMetadata: resolution.Value.GetFlagMetadata()));
    }

    /// <inheritdoc />
    public override Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        var unleash = Volatile.Read(ref this._unleash);
        unleash?.Dispose();
        return Task.CompletedTask;
    }

    private VariantResolution? EvaluateVariant(string flagKey, EvaluationContext? context)
    {
        var unleash = Volatile.Read(ref this._unleash) ?? throw new ProviderNotReadyException("Unleash not ready!");

        var unleashContext = context.ToUnleashContext();
        var variant = unleash.GetVariant(flagKey, unleashContext);

        if (variant == null || variant.Name == Variant.DISABLED_VARIANT.Name)
        {
            return null;
        }

        return new VariantResolution(variant.Name, variant.Payload?.Value, variant.Payload?.Type);
    }

    private readonly struct VariantResolution
    {
        public string VariantName { get; }
        public string? PayloadValue { get; }
        public string? PayloadType { get; }

        public VariantResolution(string variantName, string? payloadValue, string? payloadType)
        {
            this.VariantName = variantName;
            this.PayloadValue = payloadValue;
            this.PayloadType = payloadType;
        }

        public ImmutableMetadata? GetFlagMetadata()
        {
            if (PayloadType == null)
            {
                return null;
            }

            return new ImmutableMetadata(new Dictionary<string, object>
            {
                { "payload-type", PayloadType }
            });
        }
    }
}
