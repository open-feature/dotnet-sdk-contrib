using OpenFeature.Constant;
using OpenFeature.Model;
using OpenFeature.Providers.Ofrep.Client;
using OpenFeature.Providers.Ofrep.Client.Constants;
using OpenFeature.Providers.Ofrep.Configuration;

namespace OpenFeature.Providers.Ofrep;

/// <summary>
/// OFREPProvider is the .NET provider implementation for the OpenFeature REST
/// Evaluation Protocol.
/// </summary>
public sealed class OfrepProvider : FeatureProvider, IDisposable
{
    private readonly IOfrepClient _client;

    private const string Name = "OpenFeature Remote Evaluation Protocol Server";
    private bool _disposed;

    /// <summary>
    /// Creates new instance of <see cref="OfrepProvider"/>
    /// </summary>
    /// <param name="configuration">The OFREP provider configuration.</param>
    public OfrepProvider(OfrepOptions configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

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
        this.ResolveFlag(flagKey, defaultValue, context,
            cancellationToken);

    /// <inheritdoc/>
    public override Task<ResolutionDetails<string>> ResolveStringValueAsync(
        string flagKey, string defaultValue, EvaluationContext? context = null,
        CancellationToken cancellationToken = default) =>
        this.ResolveFlag(flagKey, defaultValue, context,
            cancellationToken);

    /// <inheritdoc/>
    public override Task<ResolutionDetails<int>> ResolveIntegerValueAsync(
        string flagKey, int defaultValue, EvaluationContext? context = null,
        CancellationToken cancellationToken = default) =>
        this.ResolveFlag(flagKey, defaultValue, context,
            cancellationToken);

    /// <inheritdoc/>
    public override Task<ResolutionDetails<double>> ResolveDoubleValueAsync(
        string flagKey, double defaultValue, EvaluationContext? context = null,
        CancellationToken cancellationToken = default) =>
        this.ResolveFlag(flagKey, defaultValue, context,
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

        var response =
            await this._client.EvaluateFlag(flagKey, defaultValue.AsObject,
                context, cancellationToken).ConfigureAwait(false);

        return new ResolutionDetails<Value>(
            flagKey,
            response.Value != null ? new Value(response.Value) : new Value(String.Empty),
            MapErrorType(response.ErrorCode ?? string.Empty),
            reason: response.Reason,
            variant: response.Variant,
            errorMessage: response.ErrorMessage,
            flagMetadata: response.Metadata != null ? new ImmutableMetadata(response.Metadata) : null);
    }

    /// <summary>
    /// Resolves a flag of the specified type using the OFREP client.
    /// </summary>
    /// <typeparam name="T">The type of the flag value</typeparam>
    /// <param name="flagKey">The unique identifier for the flag</param>
    /// <param name="defaultValue">The default value to return if the flag cannot be resolved</param>
    /// <param name="context">Optional evaluation context with targeting information</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>Resolution details containing the flag value and
    /// metadata</returns>
    private async Task<ResolutionDetails<T>> ResolveFlag<T>(
        string flagKey,
        T defaultValue,
        EvaluationContext? context,
        CancellationToken cancellationToken)
    {
        if (flagKey == null)
        {
            throw new ArgumentNullException(nameof(flagKey));
        }

        var response = await this._client.EvaluateFlag(flagKey, defaultValue,
            context, cancellationToken).ConfigureAwait(false);

        return new ResolutionDetails<T>(
            flagKey,
            response.Value != null ? response.Value : defaultValue,
            MapErrorType(response.ErrorCode ?? string.Empty),
            reason: response.Reason,
            variant: response.Variant,
            errorMessage: response.ErrorMessage,
            flagMetadata: response.Metadata != null ? new ImmutableMetadata(response.Metadata) : null);
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
            ErrorCodes.ParseError => ErrorType.ParseError,
            ErrorCodes.ProviderNotReady => ErrorType.ProviderNotReady,
            ErrorCodes.InvalidContext => ErrorType.InvalidContext,
            ErrorCodes.TargetingKeyMissing => ErrorType.TargetingKeyMissing,
            ErrorCodes.ProviderFatal => ErrorType.ProviderFatal,
            ErrorCodes.General => ErrorType.General,
            _ => ErrorType.None
        };

        return result;
    }
}
