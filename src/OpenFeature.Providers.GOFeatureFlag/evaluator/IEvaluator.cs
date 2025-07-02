using System;
using System.Threading.Tasks;
using OpenFeature.Model;

namespace OpenFeature.Providers.GOFeatureFlag.evaluator;

/// <summary>
///     IEvaluator is an interface that represents the evaluation of a feature flag.
///     It can have multiple implementations: Remote or InProcess.
/// </summary>
public interface IEvaluator : IAsyncDisposable
{
    /// <summary>
    ///     Initialize the evaluator.
    /// </summary>
    public Task InitializeAsync();

    /// <summary>
    ///     Evaluate an Object flag.
    /// </summary>
    /// <param name="flagKey">name of the feature flag</param>
    /// <param name="defaultValue">default value</param>
    /// <param name="evaluationContext">evaluation context of the evaluation</param>
    /// <returns>An ResolutionDetails response</returns>
    public Task<ResolutionDetails<Value>> EvaluateAsync(string flagKey, Value defaultValue,
        EvaluationContext evaluationContext);

    /// <summary>
    ///     Evaluate a string flag.
    /// </summary>
    /// <param name="flagKey">name of the feature flag</param>
    /// <param name="defaultValue">default value</param>
    /// <param name="evaluationContext">evaluation context of the evaluation</param>
    /// <returns>An ResolutionDetails response</returns>
    public Task<ResolutionDetails<string>> EvaluateAsync(string flagKey, string defaultValue,
        EvaluationContext evaluationContext);

    /// <summary>
    ///     Evaluate an int flag.
    /// </summary>
    /// <param name="flagKey">name of the feature flag</param>
    /// <param name="defaultValue">default value</param>
    /// <param name="evaluationContext">evaluation context of the evaluation</param>
    /// <returns>An ResolutionDetails response</returns>
    public Task<ResolutionDetails<int>> EvaluateAsync(string flagKey, int defaultValue,
        EvaluationContext evaluationContext);

    /// <summary>
    ///     Evaluate a double flag.
    /// </summary>
    /// <param name="flagKey">name of the feature flag</param>
    /// <param name="defaultValue">default value</param>
    /// <param name="evaluationContext">evaluation context of the evaluation</param>
    /// <returns>An ResolutionDetails response</returns>
    public Task<ResolutionDetails<double>> EvaluateAsync(string flagKey, double defaultValue,
        EvaluationContext evaluationContext);

    /// <summary>
    ///     Evaluate a boolean flag.
    /// </summary>
    /// <param name="flagKey">name of the feature flag</param>
    /// <param name="defaultValue">default value</param>
    /// <param name="evaluationContext">evaluation context of the evaluation</param>
    /// <returns>An ResolutionDetails response</returns>
    public Task<ResolutionDetails<bool>> EvaluateAsync(string flagKey, bool defaultValue,
        EvaluationContext evaluationContext);

    /// <summary>
    ///     Check if the flag is trackable.
    /// </summary>
    /// <param name="flagKey">name of the feature flag</param>
    /// <returns>true if the flag is trackable</returns>
    public bool IsFlagTrackable(string flagKey);
}
