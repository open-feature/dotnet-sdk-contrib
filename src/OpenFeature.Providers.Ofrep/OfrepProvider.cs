using OpenFeature.Constant;
using OpenFeature.Model;
using OpenFeature.Providers.Ofrep.Client;
using OpenFeature.Providers.Ofrep.Client.Constants;
using OpenFeature.Providers.Ofrep.Configuration;
using OpenFeature.Providers.Ofrep.Models;

namespace OpenFeature.Providers.Ofrep;

/// <summary>
/// OFREPProvider is the .NET provider implementation for the OpenFeature REST
/// Evaluation Protocol.
/// </summary>
public sealed class OfrepProvider : FeatureProvider, IDisposable
{
    /// <summary>
    /// Default configuration for the OFREP provider.
    /// </summary>

    private static readonly ConfigurationResponse DefaultConfiguration =
        new()
        {
            Name = "OpenFeature Remote Evaluation Protocol Server",
            Capabilities =
                new ProviderCapabilities
                {
                    FlagEvaluation =
                        new ProviderFlagEvaluation([
                            "boolean", "string", "integer",
                            "double", "object"
                        ])
                }
        };

    private readonly IOfrepClient _client;

    private const string Name = "OpenFeature Remote Evaluation Protocol Server";
    private readonly ProviderCapabilities? _capabilities = DefaultConfiguration.Capabilities;
    private bool _disposed;

    /// <summary>
    /// Creates new instance of <see cref="OfrepProvider"/>
    /// </summary>
    /// <param name="configuration">The OFREP provider configuration.</param>
    public OfrepProvider(OfrepConfiguration configuration)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(configuration);
#else
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }
#endif

        this._client = new OfrepClient(configuration);
    }

    /// <inheritdoc/>
    public override Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        this.Dispose();

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override Metadata GetMetadata() => new(Name);

    /// <summary>
    /// Disposes the OFREP provider and releases any resources.
    /// </summary>
    public void Dispose()
    {
        if (this._disposed)
        {
            return;
        }

        this._client.Dispose();
        this._disposed = true;

        // ReSharper disable once GCSuppressFinalizeForTypeWithoutDestructor
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public override Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(
        string flagKey, bool defaultValue, EvaluationContext? context = null,
        CancellationToken cancellationToken = default) =>
        this.ResolveFlag(flagKey, "boolean", defaultValue, context,
            cancellationToken);

    /// <inheritdoc/>
    public override Task<ResolutionDetails<string>> ResolveStringValueAsync(
        string flagKey, string defaultValue, EvaluationContext? context = null,
        CancellationToken cancellationToken = default) =>
        this.ResolveFlag(flagKey, "string", defaultValue, context,
            cancellationToken);

    /// <inheritdoc/>
    public override Task<ResolutionDetails<int>> ResolveIntegerValueAsync(
        string flagKey, int defaultValue, EvaluationContext? context = null,
        CancellationToken cancellationToken = default) =>
        this.ResolveFlag(flagKey, "integer", defaultValue, context,
            cancellationToken);

    /// <inheritdoc/>
    public override Task<ResolutionDetails<double>> ResolveDoubleValueAsync(
        string flagKey, double defaultValue, EvaluationContext? context = null,
        CancellationToken cancellationToken = default) =>
        this.ResolveFlag(flagKey, "double", defaultValue, context,
            cancellationToken);

    /// <inheritdoc/>
    public override async Task<ResolutionDetails<Value>>
        ResolveStructureValueAsync(string flagKey, Value defaultValue,
            EvaluationContext? context = null,
            CancellationToken cancellationToken = default)

    {
#if NET8_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(flagKey);
#else
        if (flagKey == null)
        {
            throw new ArgumentNullException(nameof(flagKey));
        }
#endif

        this.ValidateFlagTypeIsSupported("object");

        var response =
            await this._client.EvaluateFlag(flagKey, "object", defaultValue.AsObject,
                context, cancellationToken).ConfigureAwait(false);

        return new ResolutionDetails<Value>(
            flagKey,
            response.Value != null ? new Value(response.Value) : new Value(String.Empty),
            MapErrorType(response.ErrorCode ?? string.Empty),
            reason: string.Empty,
            variant: response.Variant,
            errorMessage: response.ErrorMessage);
    }

    /// <summary>
    /// Validates if the specified flag type is supported by the provider.
    /// </summary>
    /// <param name="type">The type of the flag as a string (e.g., "boolean",
    /// "string")</param> <exception cref="ArgumentException">Thrown if the flag
    /// type is not supported.</exception>
    private void ValidateFlagTypeIsSupported(string type)
    {
        if (this._capabilities?.FlagEvaluation == null)
        {
            // If capabilities are null, we cannot validate the flag type. Assume
            // it's supported.
            return;
        }

        if (Array.IndexOf(this._capabilities.FlagEvaluation.SupportedTypes, type) <
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
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(flagKey);
        ArgumentNullException.ThrowIfNull(type);
#else
        if (flagKey == null)
        {
            throw new ArgumentNullException(nameof(flagKey));
        }

        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }
#endif
        this.ValidateFlagTypeIsSupported(type);

        var response = await this._client.EvaluateFlag(flagKey, type, defaultValue,
            context, cancellationToken).ConfigureAwait(false);

        return new ResolutionDetails<T>(
            flagKey,
            response.Value != null ? response.Value : defaultValue,
            MapErrorType(response.ErrorCode ?? string.Empty),
            reason: string.Empty,
            variant: response.Variant,
            errorMessage: response.ErrorMessage);
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

        ErrorType result = code switch
        {
            ErrorCodes.FlagNotFound => ErrorType.FlagNotFound,
            ErrorCodes.TypeMismatch => ErrorType.TypeMismatch,
            ErrorCodes.ParsingError => ErrorType.ParseError,
            ErrorCodes.ProviderNotReady => ErrorType.ProviderNotReady,
            _ => ErrorType.None
        };

        return result;
    }
}
