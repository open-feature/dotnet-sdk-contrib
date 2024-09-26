using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.Flipt;

/// <summary>
///     FliptProvider is the .NET provider implementation for Flipt.io
/// </summary>
/// <remarks>
/// </remarks>
/// <param name="fliptUrl">Url of flipt instance</param>
/// <param name="namespaceKey">Namespace used for querying flags</param>
/// <param name="clientToken">Authentication access token</param>
/// <param name="timeoutInSeconds">Timeout when calling flipt endpoints in seconds</param>
public class FliptProvider(
    string fliptUrl,
    string namespaceKey = "default",
    string clientToken = "",
    int timeoutInSeconds = 30) : FeatureProvider
{
    private static readonly Metadata Metadata = new("Flipt Provider");

    private readonly IFliptClientWrapper _fliptClientWrapper =
        new FliptClientWrapper(fliptUrl, namespaceKey, clientToken, timeoutInSeconds);

    /// <inheritdoc />
    public override Metadata GetMetadata()
    {
        return Metadata;
    }

    /// <inheritdoc />
    public override async Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(string flagKey, bool defaultValue,
        EvaluationContext context = null,
        CancellationToken cancellationToken = new())
    {
        return await _fliptClientWrapper.EvaluateBooleanAsync(flagKey, defaultValue, context);
    }

    /// <inheritdoc />
    public override async Task<ResolutionDetails<string>> ResolveStringValueAsync(string flagKey,
        string defaultValue, EvaluationContext context = null,
        CancellationToken cancellationToken = new())
    {
        return await _fliptClientWrapper.EvaluateAsync(flagKey, defaultValue, context);
    }

    /// <inheritdoc />
    public override async Task<ResolutionDetails<int>> ResolveIntegerValueAsync(string flagKey, int defaultValue,
        EvaluationContext context = null,
        CancellationToken cancellationToken = new())
    {
        return await _fliptClientWrapper.EvaluateAsync(flagKey, defaultValue, context);
    }

    /// <inheritdoc />
    public override async Task<ResolutionDetails<double>> ResolveDoubleValueAsync(string flagKey, double defaultValue,
        EvaluationContext context = null,
        CancellationToken cancellationToken = new())
    {
        return await _fliptClientWrapper.EvaluateAsync(flagKey, defaultValue, context);
    }

    /// <inheritdoc />
    public override async Task<ResolutionDetails<Value>> ResolveStructureValueAsync(string flagKey, Value defaultValue,
        EvaluationContext context = null,
        CancellationToken cancellationToken = new())
    {
        return await _fliptClientWrapper.EvaluateAsync(flagKey, defaultValue, context);
    }
}