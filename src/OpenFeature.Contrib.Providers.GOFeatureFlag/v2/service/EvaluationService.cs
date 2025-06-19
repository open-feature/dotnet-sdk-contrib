using System.Threading.Tasks;
using OpenFeature.Contrib.Providers.GOFeatureFlag.v2.evaluator;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.v2.service;

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

    public async Task<ResolutionDetails<bool>> GetEvaluation(string flagKey, bool defaultValue,
        EvaluationContext evaluationContext)
    {
        return await evaluator.Evaluate(flagKey, defaultValue, evaluationContext).ConfigureAwait(false);
    }

    public async Task<ResolutionDetails<string>> GetEvaluation(string flagKey, string defaultValue,
        EvaluationContext evaluationContext)
    {
        return await evaluator.Evaluate(flagKey, defaultValue, evaluationContext).ConfigureAwait(false);
    }

    public async Task<ResolutionDetails<int>> GetEvaluation(string flagKey, int defaultValue,
        EvaluationContext evaluationContext)
    {
        return await evaluator.Evaluate(flagKey, defaultValue, evaluationContext).ConfigureAwait(false);
    }

    public async Task<ResolutionDetails<double>> GetEvaluation(string flagKey, double defaultValue,
        EvaluationContext evaluationContext)
    {
        return await evaluator.Evaluate(flagKey, defaultValue, evaluationContext).ConfigureAwait(false);
    }

    public async Task<ResolutionDetails<Value>> GetEvaluation(string flagKey, Value defaultValue,
        EvaluationContext evaluationContext)
    {
        return await evaluator.Evaluate(flagKey, defaultValue, evaluationContext).ConfigureAwait(false);
    }

    public bool IsFlagTrackable(string flagKey)
    {
        return evaluator.IsFlagTrackable(flagKey);
    }
}
