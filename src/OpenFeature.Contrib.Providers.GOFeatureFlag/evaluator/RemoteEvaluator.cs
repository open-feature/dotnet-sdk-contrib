using System.Collections.Generic;
using System.Threading.Tasks;
using OpenFeature.Model;
using OpenFeature.Providers.Ofrep;
using OpenFeature.Providers.Ofrep.Configuration;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.v2.evaluator;

/// <summary>
///     RemoteEvaluator is an implementation of IEvaluator that uses the OFREP API to evaluate feature flags.
/// </summary>
public class RemoteEvaluator : IEvaluator
{
    private readonly OfrepProvider _ofrepProvider;

    /// <summary>
    ///     Constructor for RemoteEvaluator.
    /// </summary>
    /// <param name="options">Options of the GOFF Provider</param>
    public RemoteEvaluator(GoFeatureFlagProviderOptions options)
    {
        var ofrepOptions = new OfrepOptions(options.Endpoint);

        // Set OFREP headers
        var headers = new Dictionary<string, string>();
        headers.Add("Content-Type", "application/json");
        if (options.ApiKey != null)
        {
            headers.Add("Authorization", $"Bearer {options.ApiKey}");
        }

        ofrepOptions.Headers = headers;
        ofrepOptions.Timeout = options.Timeout;

        this._ofrepProvider = new OfrepProvider(ofrepOptions);
    }

    /// <summary>
    ///     Shutdown the evaluator.
    /// </summary>
    public Task DisposeAsync()
    {
        this._ofrepProvider.Dispose();
        return Task.CompletedTask;
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
    /// <returns>An ResolutionDetails response</returns>
    public Task<ResolutionDetails<Value>> Evaluate(string flagKey, Value defaultValue,
        EvaluationContext evaluationContext)
    {
        return this._ofrepProvider.ResolveStructureValueAsync(flagKey, defaultValue, evaluationContext);
    }

    /// <summary>
    ///     Evaluate a string flag.
    /// </summary>
    /// <param name="flagKey">name of the feature flag</param>
    /// <param name="defaultValue">default value</param>
    /// <param name="evaluationContext">evaluation context of the evaluation</param>
    /// <returns>An ResolutionDetails response</returns>
    public Task<ResolutionDetails<string>> Evaluate(string flagKey, string defaultValue,
        EvaluationContext evaluationContext)
    {
        return this._ofrepProvider.ResolveStringValueAsync(flagKey, defaultValue, evaluationContext);
    }

    /// <summary>
    ///     Evaluate an int flag.
    /// </summary>
    /// <param name="flagKey">name of the feature flag</param>
    /// <param name="defaultValue">default value</param>
    /// <param name="evaluationContext">evaluation context of the evaluation</param>
    /// <returns>An ResolutionDetails response</returns>
    public Task<ResolutionDetails<int>> Evaluate(string flagKey, int defaultValue,
        EvaluationContext evaluationContext)
    {
        return this._ofrepProvider.ResolveIntegerValueAsync(flagKey, defaultValue, evaluationContext);
    }

    /// <summary>
    ///     Evaluate a double flag.
    /// </summary>
    /// <param name="flagKey">name of the feature flag</param>
    /// <param name="defaultValue">default value</param>
    /// <param name="evaluationContext">evaluation context of the evaluation</param>
    /// <returns>An ResolutionDetails response</returns>
    public Task<ResolutionDetails<double>> Evaluate(string flagKey, double defaultValue,
        EvaluationContext evaluationContext)
    {
        return this._ofrepProvider.ResolveDoubleValueAsync(flagKey, defaultValue, evaluationContext);
    }

    /// <summary>
    ///     Evaluate a boolean flag.
    /// </summary>
    /// <param name="flagKey">name of the feature flag</param>
    /// <param name="defaultValue">default value</param>
    /// <param name="evaluationContext">evaluation context of the evaluation</param>
    /// <returns>An ResolutionDetails response</returns>
    public Task<ResolutionDetails<bool>> Evaluate(string flagKey, bool defaultValue,
        EvaluationContext evaluationContext)
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
}
