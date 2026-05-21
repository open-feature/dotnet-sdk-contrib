using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Constant;
using OpenFeature.Model;
using Unleash;
using Unleash.Internal;

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
public class UnleashProvider : FeatureProvider
{
    private static readonly Metadata ProviderMetadata = new("Unleash Provider");

    private readonly UnleashSettings _settings;
    private TaskCompletionSource<IUnleash>? _clientTcs;

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

        var tcs = new TaskCompletionSource<IUnleash>(TaskCreationOptions.RunContinuationsAsynchronously);
        Volatile.Write(ref this._clientTcs, tcs);

        if (this._settings.ToggleBootstrapProvider != null)
        {
            var unleash = new DefaultUnleash(this._settings, callback =>
            {
                callback.TogglesUpdatedEvent = _ => this.EmitConfigurationChanged();
            });
            tcs.TrySetResult(unleash);
            return Task.CompletedTask;
        }

        IUnleash client = null!;
        client = new DefaultUnleash(this._settings, callback =>
        {
            callback.ReadyEvent = _ =>
            {
                tcs.TrySetResult(client);
            };
            callback.ErrorEvent = evt =>
            {
                this.EmitProviderError(evt);
                tcs.TrySetResult(client);
            };
            callback.TogglesUpdatedEvent = _ => this.EmitConfigurationChanged();
        });

        var registration = cancellationToken.Register(() => tcs.TrySetResult(client));
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

    internal void EmitProviderError(global::Unleash.Events.ErrorEvent evt)
    {
        this.EventChannel.Writer.TryWrite(new ProviderEventPayload
        {
            Type = ProviderEventTypes.ProviderError,
            ProviderName = ProviderMetadata.Name,
            Message = evt.Error?.Message ?? $"Unleash error: {evt.ErrorType}"
        });
    }

    /// <summary>
    /// Allows tests to simulate the ReadyEvent by providing a client instance.
    /// </summary>
    internal void SetReady(IUnleash client)
    {
        var tcs = Volatile.Read(ref this._clientTcs);
        if (tcs == null)
        {
            tcs = new TaskCompletionSource<IUnleash>(TaskCreationOptions.RunContinuationsAsynchronously);
            Interlocked.CompareExchange(ref this._clientTcs, tcs, null);
            tcs = Volatile.Read(ref this._clientTcs)!;
        }

        tcs.TrySetResult(client);
    }

    private Task<IUnleash>? GetClientTask()
    {
        return Volatile.Read(ref this._clientTcs)?.Task;
    }

    /// <inheritdoc />
    public override async Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(
        string flagKey, bool defaultValue, EvaluationContext? context = null, CancellationToken cancellationToken = default)
    {
        var clientTask = this.GetClientTask();
        if (clientTask == null)
        {
            return new ResolutionDetails<bool>(flagKey, defaultValue, reason: Reason.Error, errorType: ErrorType.ProviderNotReady);
        }

        var unleash = await clientTask.ConfigureAwait(false);
        var unleashContext = context.ToUnleashContext();
        var result = unleash.IsEnabled(flagKey, unleashContext, defaultValue);

        return new ResolutionDetails<bool>(flagKey, result);
    }

    /// <inheritdoc />
    public override async Task<ResolutionDetails<string>> ResolveStringValueAsync(
        string flagKey, string defaultValue, EvaluationContext? context = null, CancellationToken cancellationToken = default)
    {
        var clientTask = this.GetClientTask();
        if (clientTask == null)
        {
            return new ResolutionDetails<string>(flagKey, defaultValue, reason: Reason.Error, errorType: ErrorType.ProviderNotReady);
        }

        var unleash = await clientTask.ConfigureAwait(false);
        var resolution = this.EvaluateVariant(unleash, flagKey, context);

        if (resolution == null)
        {
            return new ResolutionDetails<string>(flagKey, defaultValue, reason: Reason.Disabled);
        }

        var value = resolution.Value.PayloadValue ?? defaultValue;
        return new ResolutionDetails<string>(flagKey, value, variant: resolution.Value.VariantName, flagMetadata: resolution.Value.GetFlagMetadata());
    }

    /// <inheritdoc />
    public override async Task<ResolutionDetails<int>> ResolveIntegerValueAsync(
        string flagKey, int defaultValue, EvaluationContext? context = null, CancellationToken cancellationToken = default)
    {
        var clientTask = this.GetClientTask();
        if (clientTask == null)
        {
            return new ResolutionDetails<int>(flagKey, defaultValue, reason: Reason.Error, errorType: ErrorType.ProviderNotReady);
        }

        var unleash = await clientTask.ConfigureAwait(false);
        var resolution = this.EvaluateVariant(unleash, flagKey, context);

        if (resolution == null)
        {
            return new ResolutionDetails<int>(flagKey, defaultValue, reason: Reason.Disabled);
        }

        if (int.TryParse(resolution.Value.PayloadValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            return new ResolutionDetails<int>(flagKey, parsed, variant: resolution.Value.VariantName, flagMetadata: resolution.Value.GetFlagMetadata());
        }

        return new ResolutionDetails<int>(
            flagKey,
            defaultValue,
            reason: Reason.Error,
            errorType: ErrorType.TypeMismatch,
            errorMessage: $"Cannot parse variant payload '{resolution.Value.PayloadValue}' as integer for flag '{flagKey}'.");
    }

    /// <inheritdoc />
    public override async Task<ResolutionDetails<double>> ResolveDoubleValueAsync(
        string flagKey, double defaultValue, EvaluationContext? context = null, CancellationToken cancellationToken = default)
    {
        var clientTask = this.GetClientTask();
        if (clientTask == null)
        {
            return new ResolutionDetails<double>(flagKey, defaultValue, reason: Reason.Error, errorType: ErrorType.ProviderNotReady);
        }

        var unleash = await clientTask.ConfigureAwait(false);
        var resolution = this.EvaluateVariant(unleash, flagKey, context);

        if (resolution == null)
        {
            return new ResolutionDetails<double>(flagKey, defaultValue, reason: Reason.Disabled);
        }

        if (double.TryParse(resolution.Value.PayloadValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            return new ResolutionDetails<double>(flagKey, parsed, variant: resolution.Value.VariantName, flagMetadata: resolution.Value.GetFlagMetadata());
        }

        return new ResolutionDetails<double>(
            flagKey,
            defaultValue,
            reason: Reason.Error,
            errorType: ErrorType.TypeMismatch,
            errorMessage: $"Cannot parse variant payload '{resolution.Value.PayloadValue}' as double for flag '{flagKey}'.");
    }

    /// <inheritdoc />
    public override async Task<ResolutionDetails<Value>> ResolveStructureValueAsync(
        string flagKey, Value defaultValue, EvaluationContext? context = null, CancellationToken cancellationToken = default)
    {
        var clientTask = this.GetClientTask();
        if (clientTask == null)
        {
            return new ResolutionDetails<Value>(flagKey, defaultValue, reason: Reason.Error, errorType: ErrorType.ProviderNotReady);
        }

        var unleash = await clientTask.ConfigureAwait(false);
        var resolution = this.EvaluateVariant(unleash, flagKey, context);

        if (resolution == null)
        {
            return new ResolutionDetails<Value>(flagKey, defaultValue, reason: Reason.Disabled);
        }

        var value = resolution.Value.PayloadValue != null
            ? new Value(resolution.Value.PayloadValue)
            : defaultValue;

        return new ResolutionDetails<Value>(flagKey, value, variant: resolution.Value.VariantName, flagMetadata: resolution.Value.GetFlagMetadata());
    }

    /// <inheritdoc />
    public override async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        var clientTask = this.GetClientTask();
        if (clientTask != null)
        {
            var client = await clientTask.ConfigureAwait(false);
            client?.Dispose();
        }
    }

    private VariantResolution? EvaluateVariant(IUnleash unleash, string flagKey, EvaluationContext? context)
    {
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
