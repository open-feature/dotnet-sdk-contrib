using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Model;
using OpenFeature.Providers.GOFeatureFlag.Ofrep;
using OpenFeature.Providers.Ofrep;
using OpenFeature.Providers.Ofrep.Configuration;

namespace OpenFeature.Providers.GOFeatureFlag.Evaluator;

/// <summary>
///     RemoteEvaluator is an implementation of IEvaluator that uses the OFREP API to evaluate feature flags.
/// </summary>
public class RemoteEvaluator : IEvaluator
{
    private readonly IOfrepProvider _ofrepProvider;

    /// <summary>
    ///     Constructor for RemoteEvaluator.
    /// </summary>
    /// <param name="options">Options of the GOFF Provider</param>
    /// <param name="ofrepProvider">Optional custom OFREP provider (used for test)</param>
    internal RemoteEvaluator(GOFeatureFlagProviderOptions options, IOfrepProvider? ofrepProvider = null)
    {
        if (ofrepProvider != null)
        {
            this._ofrepProvider = ofrepProvider;
            return;
        }

        var ofrepOptions = new OfrepOptions(options.Endpoint);

        var headers = new Dictionary<string, string>
        {
            { "Content-Type", "application/json" }
        };
        if (options.ApiKey != null)
        {
            headers.Add("Authorization", $"Bearer {options.ApiKey}");
        }

        ofrepOptions.Headers = headers;
        ofrepOptions.Timeout = options.Timeout;

        this._ofrepProvider = new OfrepProviderWrapper(new OfrepProvider(ofrepOptions));
    }

    /// <summary>
    ///     Initialize the evaluator.
    /// </summary>
    public Task InitializeAsync()
    {
        return this._ofrepProvider.InitializeAsync(EvaluationContext.Empty);
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
        return this._ofrepProvider.ResolveStructureValueAsync(flagKey, defaultValue, evaluationContext);
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
        return this._ofrepProvider.ResolveStringValueAsync(flagKey, defaultValue, evaluationContext);
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
        return this._ofrepProvider.ResolveIntegerValueAsync(flagKey, defaultValue, evaluationContext);
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
        return this._ofrepProvider.ResolveDoubleValueAsync(flagKey, defaultValue, evaluationContext);
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
        EvaluationContext? evaluationContext = null, CancellationToken cancellationToken = default)
    {
        return this._ofrepProvider.ResolveBooleanValueAsync(flagKey, defaultValue, evaluationContext);
    }

    /// <summary>
    ///     Check if the flag is trackable.
    /// </summary>
    /// <param name="flagKey">name of the feature flag</param>
    /// <returns>true if the flag is trackable</returns>
    public bool IsFlagTrackable(string flagKey)
    {
        return true;
    }

    /// <summary>
    ///     Shutdown the evaluator.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        this._ofrepProvider.Dispose();
        return new ValueTask();
    }
}
