using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenFeature.Constant;
using OpenFeature.Error;
using OpenFeature.Model;
using OpenFeature.Providers.GOFeatureFlag.Api;
using OpenFeature.Providers.GOFeatureFlag.Exceptions;
using OpenFeature.Providers.GOFeatureFlag.Extensions;
using OpenFeature.Providers.GOFeatureFlag.Helpers;
using OpenFeature.Providers.GOFeatureFlag.Models;
using OpenFeature.Providers.GOFeatureFlag.Wasm;
using OpenFeature.Providers.GOFeatureFlag.Wasm.Bean;

namespace OpenFeature.Providers.GOFeatureFlag.Evaluator;

/// <summary>
///     InProcessEvaluator is an implementation of the IEvaluator interface that evaluates feature flags in-process.
/// </summary>
public class InProcessEvaluator : IEvaluator
{
    /// <summary>
    ///     API to contact GO Feature Flag.
    /// </summary>
    private readonly GOFeatureFlagApi _api;

    /// <summary>
    ///     WASM evaluation engine.
    /// </summary>
    private readonly EvaluateWasm _evaluationEngine;

    /// <summary>
    ///     Event channel to send events to the event bus or event handler.
    /// </summary>
    private readonly Channel<object> _eventChannel;

    /// <summary>
    ///     Options to configure the provider.
    /// </summary>
    private readonly GOFeatureFlagProviderOptions _options;

    /// <summary>
    ///     PeriodicAsyncRunner is used to periodically check for configuration changes.
    /// </summary>
    private readonly PeriodicAsyncRunner _periodicAsyncRunner;

    /// <summary>
    ///     Provider metadata containing information about the provider.
    /// </summary>
    private readonly Metadata _providerMetadata;

    /// <summary>
    ///     Last hash of the flags' configuration.
    /// </summary>
    private string? _etag;

    /// <summary>
    ///     Evaluation context enrichment.
    /// </summary>
    private IDictionary<string, object>? _evaluationContextEnrichment;

    /// <summary>
    ///     Local copy of the flags' configuration.
    /// </summary>
    private IDictionary<string, Flag> _flags;

    /// <summary>
    ///     Last update of the flags' configuration.
    /// </summary>
    private DateTime _lastUpdate;


    /// <summary>
    ///     Constructor of the InProcessEvaluator.
    /// </summary>
    /// <param name="api">API to contact GO Feature Flag</param>
    /// <param name="options">options to configure the provider</param>
    /// <param name="eventChannel">Event channel to send events to the event bus or event handler</param>
    /// <param name="providerMetadata">Metadata containing information about the provider</param>
    /// <exception cref="ArgumentNullException">Thrown when a mandatory </exception>
    public InProcessEvaluator(GOFeatureFlagApi api, GOFeatureFlagProviderOptions options, Channel<object> eventChannel,
        Metadata providerMetadata)
    {
        this._providerMetadata = providerMetadata ??
                                 throw new ArgumentNullException(nameof(providerMetadata),
                                     "Provider metadata cannot be null");
        this._api = api ?? throw new ArgumentNullException(nameof(api), "API layer cannot be null");
        this._options = options ?? throw new ArgumentNullException(nameof(options), "Options cannot be null");
        this._eventChannel = eventChannel;
        this._flags = new Dictionary<string, Flag>();
        this._lastUpdate = DateTime.MinValue.ToUniversalTime();
        this._evaluationEngine = new EvaluateWasm();
        this._periodicAsyncRunner = new PeriodicAsyncRunner(
            this.LoadConfigurationAsync, this._options.FlagChangePollingIntervalMs, this._options.Logger);
    }

    /// <summary>
    ///     Initialize the evaluator.
    /// </summary>
    public async Task InitializeAsync()
    {
        await this.LoadConfigurationAsync().ConfigureAwait(false);
        _ = Task.Run(this._periodicAsyncRunner.StartAsync);
    }

    /// <summary>
    ///     IsFlagTrackable checks if the flag with the given key is trackable.
    /// </summary>
    /// <param name="flagKey">name of the flag to check</param>
    /// <returns>true if the flag is trackable</returns>
    public bool IsFlagTrackable(string flagKey)
    {
        var flagExist = this._flags.TryGetValue(flagKey, out var flag);
        if (!flagExist)
        {
            this._options.Logger?.LogWarning("Flag with key {FlagKey} not found", flagKey);
            // if the flag is not found, this is most likely a configuration change, so we track it by default.
            return true;
        }

        return flag?.TrackEvents ?? true;
    }

    /// <summary>
    ///     Evaluate an Object flag.
    /// </summary>
    /// <param name="flagKey">name of the feature flag</param>
    /// <param name="defaultValue">default value</param>
    /// <param name="evaluationContext">evaluation context of the evaluation</param>
    /// <param name="cancellationToken">cancellation token</param>
    /// <returns>An ResolutionDetails response</returns>
    public Task<ResolutionDetails<Value>> EvaluateAsync(string flagKey, Value defaultValue,
        EvaluationContext? evaluationContext = null, CancellationToken cancellationToken = default)
    {
        var response = this.GenericEvaluate(flagKey, defaultValue, evaluationContext);
        this.HandleError(response, flagKey);
        if (response.Value is JsonElement jsonValue &&
            Array.Exists(new[] { JsonValueKind.Array, JsonValueKind.Object, JsonValueKind.Null },
                kind => kind == jsonValue.ValueKind))
        {
            var value = ConvertValue((JsonElement)response.Value);
            return Task.FromResult(PrepareResponse(response, flagKey, value ?? defaultValue));
        }

        throw new FeatureProviderException(ErrorType.TypeMismatch,
            $"Flag {flagKey} had unexpected type, expected string.");
    }

    /// <summary>
    ///     Evaluate a string flag.
    /// </summary>
    /// <param name="flagKey">name of the feature flag</param>
    /// <param name="defaultValue">default value</param>
    /// <param name="evaluationContext">evaluation context of the evaluation</param>
    /// <param name="cancellationToken">cancellation token</param>
    /// <returns>An ResolutionDetails response</returns>
    public Task<ResolutionDetails<string>> EvaluateAsync(string flagKey, string defaultValue,
        EvaluationContext? evaluationContext = null, CancellationToken cancellationToken = default)
    {
        var response = this.GenericEvaluate(flagKey, defaultValue, evaluationContext);
        this.HandleError(response, flagKey);
        if (response.Value is JsonElement jsonValue &&
            jsonValue.ValueKind == JsonValueKind.String)
        {
            return Task.FromResult(PrepareResponse(response, flagKey, jsonValue.GetString() ?? defaultValue));
        }

        throw new FeatureProviderException(ErrorType.TypeMismatch,
            $"Flag {flagKey} had unexpected type, expected string.");
    }

    /// <summary>
    ///     Evaluate an int flag.
    /// </summary>
    /// <param name="flagKey">name of the feature flag</param>
    /// <param name="defaultValue">default value</param>
    /// <param name="evaluationContext">evaluation context of the evaluation</param>
    /// <param name="cancellationToken">cancellation token</param>
    /// <returns>An ResolutionDetails response</returns>
    public Task<ResolutionDetails<int>> EvaluateAsync(string flagKey, int defaultValue,
        EvaluationContext? evaluationContext = null, CancellationToken cancellationToken = default)
    {
        var response = this.GenericEvaluate(flagKey, defaultValue, evaluationContext);
        this.HandleError(response, flagKey);
        if (response.Value is JsonElement jsonValue &&
            jsonValue.ValueKind == JsonValueKind.Number)
        {
            return Task.FromResult(PrepareResponse(response, flagKey, jsonValue.GetInt32()));
        }

        throw new FeatureProviderException(ErrorType.TypeMismatch,
            $"Flag {flagKey} had unexpected type, expected int.");
    }

    /// <summary>
    ///     Evaluate a boolean flag.
    /// </summary>
    /// <param name="flagKey">name of the feature flag</param>
    /// <param name="defaultValue">default value</param>
    /// <param name="evaluationContext">evaluation context of the evaluation</param>
    /// <param name="cancellationToken">cancellation token</param>
    /// <returns>An ResolutionDetails response</returns>
    public Task<ResolutionDetails<bool>> EvaluateAsync(string flagKey, bool defaultValue,
        EvaluationContext? evaluationContext = null,CancellationToken cancellationToken = default)
    {
        var response = this.GenericEvaluate(flagKey, defaultValue, evaluationContext);
        this.HandleError(response, flagKey);
        if (response.Value is JsonElement jsonValue &&
            (jsonValue.ValueKind == JsonValueKind.True || jsonValue.ValueKind == JsonValueKind.False))
        {
            return Task.FromResult(PrepareResponse(response, flagKey, jsonValue.GetBoolean()));
        }

        throw new FeatureProviderException(ErrorType.TypeMismatch,
            $"Flag {flagKey} had unexpected type, expected boolean.");
    }

    /// <summary>
    ///     Evaluate a double flag.
    /// </summary>
    /// <param name="flagKey">name of the feature flag</param>
    /// <param name="defaultValue">default value</param>
    /// <param name="evaluationContext">evaluation context of the evaluation</param>
    /// <param name="cancellationToken">cancellation token</param>
    /// <returns>An ResolutionDetails response</returns>
    public Task<ResolutionDetails<double>> EvaluateAsync(string flagKey, double defaultValue,
        EvaluationContext? evaluationContext = null, CancellationToken cancellationToken = default)
    {
        var response = this.GenericEvaluate(flagKey, defaultValue, evaluationContext);
        this.HandleError(response, flagKey);
        if (response.Value is JsonElement jsonValue &&
            jsonValue.ValueKind == JsonValueKind.Number)
        {
            return Task.FromResult(PrepareResponse(response, flagKey, jsonValue.GetDouble()));
        }

        throw new FeatureProviderException(ErrorType.TypeMismatch,
            $"Flag {flagKey} had unexpected type, expected double.");
    }

    /// <summary>
    ///     Shutdown the evaluator.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        return new ValueTask(this._periodicAsyncRunner.StopAsync());
    }


    /// <summary>
    ///     Evaluates a flag with the given key and default value in the context of the provided evaluation context.
    /// </summary>
    /// <param name="flagKey">name of the feature flag</param>
    /// <param name="defaultValue">default value in case of error</param>
    /// <param name="evaluationContext">Context of the evaluation</param>
    /// <returns>An EvaluationResponse containing the output of the evaluation.</returns>
    private EvaluationResponse GenericEvaluate(string flagKey, object defaultValue,
        EvaluationContext? evaluationContext = null)
    {
        var flagExist = this._flags.TryGetValue(flagKey, out var flag);
        if (!flagExist || flag is null)
        {
            return new EvaluationResponse
            {
                Value = defaultValue,
                ErrorCode = nameof(ErrorType.FlagNotFound),
                ErrorDetails = $"Flag with key '{flagKey}' not found",
                Reason = Reason.Error,
                TrackEvents = true
            };
        }

        var input = new WasmInput
        {
            FlagKey = flagKey,
            EvalContext = evaluationContext?.AsDictionary().ToImmutableDictionary() ?? ImmutableDictionary<string, Value>.Empty,
            FlagContext = new FlagContext
            {
                DefaultSdkValue = defaultValue,
                EvaluationContextEnrichment = this._evaluationContextEnrichment ?? new Dictionary<string, object>(),
            },
            Flag = flag
        };

        return this._evaluationEngine.Evaluate(input);
    }

    /// <summary>
    ///     LoadConfiguration is responsible for loading the configuration of the flags from the API.
    /// </summary>
    /// <exception cref="ImpossibleToRetrieveConfigurationException">
    ///     In case, we are not able to call the relay proxy and to
    ///     get the flag values.
    /// </exception>
    private async Task LoadConfigurationAsync()
    {
        // call the API to retrieve the flags' configuration and store it in the local copy
        var flagConfigResponse =
            await this._api.RetrieveFlagConfigurationAsync(this._etag, this._options.EvaluationFlagList)
                .ConfigureAwait(false);

        if (flagConfigResponse is null)
        {
            throw new ImpossibleToRetrieveConfigurationException("Flag configuration response is null");
        }

        if ((this._etag ?? "").Equals(flagConfigResponse.Etag))
        {
            this._options.Logger?.LogDebug("Flag configuration has not changed: {}", flagConfigResponse);
            return;
        }

        var respLastUpdated = flagConfigResponse.LastUpdated?.UtcDateTime ?? DateTime.MinValue.ToUniversalTime();
        if (this._lastUpdate != DateTime.MinValue.ToUniversalTime() &&
            respLastUpdated != DateTime.MinValue.ToUniversalTime() &&
            DateTimeOffset.Compare(respLastUpdated, this._lastUpdate) < 0)
        {
            this._options.Logger?.LogInformation("configuration received is older than the current one");
            return;
        }

        this._options.Logger?.LogInformation("flag configuration has changed");
        this._etag = flagConfigResponse.Etag;
        this._lastUpdate = flagConfigResponse.LastUpdated?.UtcDateTime ?? DateTime.MinValue;
        this._flags = flagConfigResponse.Flags ?? new Dictionary<string, Flag>();
        this._evaluationContextEnrichment =
            flagConfigResponse.EvaluationContextEnrichment ?? new Dictionary<string, object>();

        // send an event to the event channel to notify about the configuration change
        this._eventChannel.Writer.TryWrite(new ProviderEventPayload
        {
            Type = ProviderEventTypes.ProviderConfigurationChanged,
            ProviderName = this._providerMetadata.Name
        });
    }


    /// <summary>
    ///     HandleError is handling the error response from the evaluation API.
    /// </summary>
    /// <param name="response">Response of the evaluation.</param>
    /// <param name="flagKey">Name of the feature flag.</param>
    /// <exception cref="FeatureProviderException">Thrown when the evaluation is on error.</exception>
    private void HandleError(EvaluationResponse response, string flagKey)
    {
        if (!string.IsNullOrEmpty(response.ErrorCode))
        {
            throw new FeatureProviderException(
                EnumHelper.GetEnumValueFromDescription<ErrorType>(response.ErrorCode),
                response.ErrorDetails
            );
        }

        if (nameof(Reason.Disabled).Equals(response.Reason))
        {
            throw new FeatureProviderException(ErrorType.FlagNotFound,
                $"Flag {flagKey} is disabled.");
        }

        if (nameof(ErrorType.FlagNotFound).Equals(response.ErrorCode))
        {
            throw new FeatureProviderException(ErrorType.FlagNotFound,
                $"Flag {flagKey} was not found in your configuration");
        }
    }

    /// <summary>
    ///     PrepareResponse is preparing the response to be returned to the caller.
    /// </summary>
    /// <param name="response">Response of the evaluation.</param>
    /// <param name="flagKey">Name of the feature flag.</param>
    /// <param name="value">Value of the feature flag.</param>
    /// <typeparam name="T">Type of the flag.</typeparam>
    /// <returns></returns>
    /// <exception cref="FeatureProviderException"></exception>
    private static ResolutionDetails<T> PrepareResponse<T>(EvaluationResponse response, string flagKey, T value)
    {
        try
        {
            return new ResolutionDetails<T>(
                flagKey,
                value,
                ErrorType.None,
                response.Reason,
                response.VariationType,
                null,
                response.Metadata.ToImmutableMetadata()
            );
        }
        catch (InvalidCastException ex)
        {
            throw new FeatureProviderException(ErrorType.TypeMismatch,
                $"Flag value {flagKey} had unexpected type {response?.Value?.GetType()}.",
                ex);
        }
    }

    /// <summary>
    ///     convertValue is converting the object return by the proxy response in the right type.
    /// </summary>
    /// <param name="value">The value we have received</param>
    /// <returns>A converted object</returns>
    /// <exception cref="InvalidCastException">If we are not able to convert the data.</exception>
    private static Value? ConvertValue(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.False || value.ValueKind == JsonValueKind.True)
        {
            return new Value(value.GetBoolean());
        }

        if (value.ValueKind == JsonValueKind.Number)
        {
            return new Value(value.GetDouble());
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            var dict = new Dictionary<string, Value>();
            using var objEnumerator = value.EnumerateObject();
            while (objEnumerator.MoveNext())
            {
                var current = objEnumerator.Current;
                var currentValue = ConvertValue(current.Value);
                if (currentValue != null)
                {
                    dict.Add(current.Name, currentValue);
                }
            }

            return new Value(new Structure(dict));
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            return new Value(value.ToString());
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            using var arrayEnumerator = value.EnumerateArray();
            var arr = new List<Value>();

            while (arrayEnumerator.MoveNext())
            {
                var current = arrayEnumerator.Current;
                var convertedValue = ConvertValue(current);
                if (convertedValue != null)
                {
                    arr.Add(convertedValue);
                }
            }

            return new Value(arr);
        }

        throw new ImpossibleToConvertTypeException($"impossible to convert the object {value}");
    }
}
