using System.Threading.Tasks;
using OpenFeature.Contrib.Providers.GOFeatureFlag.evaluator;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.service;

/// <summary>
///     EvaluationService is a service that provides methods to evaluate feature flags.
/// </summary>
/// <param name="evaluator">Evaluator used to perform feature flag evaluation.</param>
public class EvaluationService(IEvaluator evaluator)
{
    /// <summary>
    ///     Initialize the evaluator.
    /// </summary>
    public async Task InitializeAsync()
    {
        await evaluator.InitializeAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Shutdown the evaluator.
    /// </summary>
    public async Task DisposeAsync()
    {
        await evaluator.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     GetEvaluationAsync is a method that evaluates a feature flag with the given key and default value.
    /// </summary>
    /// <param name="flagKey">Name of the feature flag</param>
    /// <param name="defaultValue">Default value if on error</param>
    /// <param name="evaluationContext">Context for the flag evaluation</param>
    /// <returns>A resolution details</returns>
    public async Task<ResolutionDetails<bool>> GetEvaluationAsync(string flagKey, bool defaultValue,
        EvaluationContext evaluationContext)
    {
        return await evaluator.EvaluateAsync(flagKey, defaultValue, evaluationContext).ConfigureAwait(false);
    }

    /// <summary>
    ///     GetEvaluationAsync is a method that evaluates a feature flag with the given key and default value.
    /// </summary>
    /// <param name="flagKey">Name of the feature flag</param>
    /// <param name="defaultValue">Default value if on error</param>
    /// <param name="evaluationContext">Context for the flag evaluation</param>
    /// <returns>A resolution details</returns>
    public async Task<ResolutionDetails<string>> GetEvaluationAsync(string flagKey, string defaultValue,
        EvaluationContext evaluationContext)
    {
        return await evaluator.EvaluateAsync(flagKey, defaultValue, evaluationContext).ConfigureAwait(false);
    }

    /// <summary>
    ///     GetEvaluationAsync is a method that evaluates a feature flag with the given key and default value.
    /// </summary>
    /// <param name="flagKey">Name of the feature flag</param>
    /// <param name="defaultValue">Default value if on error</param>
    /// <param name="evaluationContext">Context for the flag evaluation</param>
    /// <returns>A resolution details</returns>
    public async Task<ResolutionDetails<int>> GetEvaluationAsync(string flagKey, int defaultValue,
        EvaluationContext evaluationContext)
    {
        return await evaluator.EvaluateAsync(flagKey, defaultValue, evaluationContext).ConfigureAwait(false);
    }

    /// <summary>
    ///     GetEvaluationAsync is a method that evaluates a feature flag with the given key and default value.
    /// </summary>
    /// <param name="flagKey">Name of the feature flag</param>
    /// <param name="defaultValue">Default value if on error</param>
    /// <param name="evaluationContext">Context for the flag evaluation</param>
    /// <returns>A resolution details</returns>
    public async Task<ResolutionDetails<double>> GetEvaluationAsync(string flagKey, double defaultValue,
        EvaluationContext evaluationContext)
    {
        return await evaluator.EvaluateAsync(flagKey, defaultValue, evaluationContext).ConfigureAwait(false);
    }

    /// <summary>
    ///     GetEvaluationAsync is a method that evaluates a feature flag with the given key and default value.
    /// </summary>
    /// <param name="flagKey">Name of the feature flag</param>
    /// <param name="defaultValue">Default value if on error</param>
    /// <param name="evaluationContext">Context for the flag evaluation</param>
    /// <returns>A resolution details</returns>
    public async Task<ResolutionDetails<Value>> GetEvaluationAsync(string flagKey, Value defaultValue,
        EvaluationContext evaluationContext)
    {
        return await evaluator.EvaluateAsync(flagKey, defaultValue, evaluationContext).ConfigureAwait(false);
    }

    /// <summary>
    ///     Returns whether the flag is trackable or not.
    /// </summary>
    /// <param name="flagKey">Name of the feature flag</param>
    /// <returns>true if the flag is trackable</returns>
    public bool IsFlagTrackable(string flagKey)
    {
        return evaluator.IsFlagTrackable(flagKey);
    }
}
