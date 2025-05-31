using System.Text.Json;
using Microsoft.Extensions.Logging;
using OpenFeature.Constant;
using OpenFeature.Contrib.Providers.Ofrep.Client;
using OpenFeature.Contrib.Providers.Ofrep.Configuration;
using OpenFeature.Contrib.Providers.Ofrep.Models;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.Ofrep;

/// <summary>
/// OFREPProvider is the .NET provider implementation for the OpenFeature REST
/// Evaluation Protocol.
/// </summary>
public sealed class OfrepProvider : FeatureProvider,
    IDisposable

{
    /// <summary>
    /// Default configuration for the OFREP provider.
    /// </summary>

    // ReSharper disable once InconsistentNaming
    private static readonly ConfigurationResponse DefaultConfiguration =
        new ConfigurationResponse
        {
            Name = "OpenFeature Remote Evaluation Protocol Server",
            Capabilities =
                new ProviderCapabilities
                {
                    CacheInvalidation =
                        new ProviderCacheInvalidation
                        {
                            Polling =
                                new FeatureCacheInvalidationPolling
                                {
                                    Enabled = false,
                                    MinPollingIntervalMs = 60000
                                }
                        },

                    FlagEvaluation =
                        new ProviderFlagEvaluation(new[]
                            {
                                "boolean", "string", "integer",
                                "double", "object"
                            }),
                    Caching =
                        new ProviderCaching { Enabled = false, TimeTolive = 60000 }
                }
        };

    private const string Name = "OpenFeature Remote Evaluation Protocol Server";

    private readonly OfrepClient _client;

    private readonly ILogger<OfrepProvider> _logger;

    private ProviderCapabilities _capabilities =
        DefaultConfiguration.Capabilities;

    private bool _disposed;

    /// <summary>
    /// Creates new instance of <see cref="OfrepProvider"/>
    /// </summary>
    /// <param name="configuration">The OFREP provider configuration.</param>
    public OfrepProvider(OfrepConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        _logger = DevLoggerProvider.CreateLogger<OfrepProvider>();

        // Get the logger from the internal provider
        var clientLogger = DevLoggerProvider.CreateLogger<IOfrepClient>();

        _client = new OfrepClient(configuration, clientLogger);
    }

    /// <summary>
    /// Updates the provider configuration based on the capabilities received
    /// from the OFREP server.
    /// </summary>
    /// <param name="capabilities">The capabilities supported by the OFREP
    /// server</param>
    private void UpdateProviderConfiguration(
        ProviderCapabilities? capabilities)
    {
        if (capabilities == null)
        {
            return;
        }

        _capabilities = capabilities;

        if (_client == null)
        {
            _logger.LogDebug(
                "OFREP client is null. Cannot update provider configuration.");
            return;
        }

        // If the caching capabilities are defined, and caching is Enabled
        // ensure the client is configured with the correct cache settings.
        if (capabilities.Caching != null && capabilities.Caching.Enabled)
        {
            _logger.LogDebug("Configuring cache configuration for http client");
        }
    }

    /// <inheritdoc/>
    public override async Task
        InitializeAsync(EvaluationContext context,
            CancellationToken cancellationToken = default)
    {
        // As part of initialization, we fetch the configuration from the OFREP
        // server

        try
        {
            var config = await _client.GetConfiguration(cancellationToken).ConfigureAwait(false);
            UpdateProviderConfiguration(config?.Capabilities);

            // Log the configuration received from the server
            _logger.LogDebug(
                "Configuration received from OFREP server: {Configuration}",
                JsonSerializer.Serialize(config));
        }
        catch
        {
            // If the server is unreachable, we log the error and continue,
            // and use the default configuration instead.
            UpdateProviderConfiguration(DefaultConfiguration.Capabilities);
        }
    }

    /// <inheritdoc/>
    public override Task
        ShutdownAsync(CancellationToken cancellationToken = default)
    {
        Dispose();

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override Metadata GetMetadata() => new Metadata(Name);

    /// <summary>
    /// Disposes the OFREP provider and releases any resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _client.Dispose();

        _disposed = true;

        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public override Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(
        string flagKey, bool defaultValue, EvaluationContext? context = null,
        CancellationToken cancellationToken = default) =>
        ResolveFlag(flagKey, "boolean", defaultValue, context,
            cancellationToken);

    /// <inheritdoc/>
    public override Task<ResolutionDetails<string>> ResolveStringValueAsync(
        string flagKey, string defaultValue, EvaluationContext? context = null,
        CancellationToken cancellationToken = default) =>
        ResolveFlag(flagKey, "string", defaultValue, context,
            cancellationToken);

    /// <inheritdoc/>
    public override Task<ResolutionDetails<int>> ResolveIntegerValueAsync(
        string flagKey, int defaultValue, EvaluationContext? context = null,
        CancellationToken cancellationToken = default) =>
        ResolveFlag(flagKey, "integer", defaultValue, context,
            cancellationToken);

    /// <inheritdoc/>
    public override Task<ResolutionDetails<double>> ResolveDoubleValueAsync(
        string flagKey, double defaultValue, EvaluationContext? context = null,
        CancellationToken cancellationToken = default) =>
        ResolveFlag(flagKey, "double", defaultValue, context,
            cancellationToken);

    /// <inheritdoc/>
    public override async Task<ResolutionDetails<Value>>
        ResolveStructureValueAsync(string flagKey, Value defaultValue,
            EvaluationContext? context = null,
            CancellationToken cancellationToken = default)

    {
        if (flagKey == null)
        {
            throw new ArgumentNullException(nameof(flagKey));
        }

        ValidateFlagTypeIsSupported("object");

        var response =
            await _client.EvaluateFlag(flagKey, "object", defaultValue?.AsObject,
                context, cancellationToken).ConfigureAwait(false);

        return new ResolutionDetails<Value>(
            flagKey,
            response.Value != null
                ? new Value(response.Value)
                : new Value(String.Empty),
            MapErrorType(response.ErrorCode ?? string.Empty),
            response.ErrorMessage, response.Variant);
    }

    /// <summary>
    /// Validates if the specified flag type is supported by the provider.
    /// </summary>
    /// <param name="type">The type of the flag as a string (e.g., "boolean",
    /// "string")</param> <exception cref="ArgumentException">Thrown if the flag
    /// type is not supported.</exception>
    private void ValidateFlagTypeIsSupported(string type)
    {
        if (_capabilities?.FlagEvaluation == null)
        {
            // If capabilities are null, we cannot validate the flag type. Assume
            // it's supported.
            return;
        }

        if (Array.IndexOf(_capabilities.FlagEvaluation.SupportedTypes, type) <
            0)
        {
            throw new ArgumentException(
                $"Flag type '{type}' is not supported by the provider.");
        }
    }

    /// <summary>
    /// Resolves a flag of the specified type using the OFREP client.
    /// </summary>
    /// <typeparam name="T">The type of the flag value</typeparam>
    /// <param name="flagKey">The unique identifier for the flag</param>
    /// <param name="type">The type of the flag as a string (e.g., "boolean",
    /// "string")</param> <param name="defaultValue">The default value to return
    /// if the flag cannot be resolved</param> <param name="context">Optional
    /// evaluation context with targeting information</param> <param
    /// name="cancellationToken">A token to cancel the operation</param>
    /// <returns>Resolution details containing the flag value and
    /// metadata</returns>
    private async Task<ResolutionDetails<T>> ResolveFlag<T>(
        string flagKey, string type, T defaultValue, EvaluationContext? context,
        CancellationToken cancellationToken)
    {
        if (flagKey == null)
        {
            throw new ArgumentNullException(nameof(flagKey));
        }

        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        ValidateFlagTypeIsSupported(type);

        var response = await _client.EvaluateFlag(flagKey, type, defaultValue,
            context, cancellationToken).ConfigureAwait(false);

        return new ResolutionDetails<T>(
            flagKey, response.Value != null ? response.Value : defaultValue,
            MapErrorType(response.ErrorCode ?? string.Empty),
            response.ErrorMessage, response.Variant);
    }

    /// <summary>
    /// Maps OFREP error codes to OpenFeature ErrorType enum values.
    /// </summary>
    /// <param name="errorCode">The error code string from the OFREP
    /// response</param> <returns>The corresponding OpenFeature
    /// ErrorType</returns>
    private static ErrorType MapErrorType(string errorCode)
    {
        var code = errorCode.ToLowerInvariant();

        ErrorType result;

        if (code == "flag_not_found")
        {
            result = ErrorType.FlagNotFound;
        }
        else if (code == "type_mismatch")
        {
            result = ErrorType.TypeMismatch;
        }
        else if (code == "parsing_error")
        {
            result = ErrorType.ParseError;
        }
        else if (code == "provider_not_ready")
        {
            result = ErrorType.ProviderNotReady;
        }
        else
        {
            result = ErrorType.None;
        }

        return result;
    }
}
