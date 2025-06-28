using System.Threading.Tasks;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.evaluator;

/// <summary>
///     IEvaluator is an interface that represents the evaluation of a feature flag.
///     It can have multiple implementations: Remote or InProcess.
/// </summary>
public interface IEvaluator
{
    /// <summary>
    ///     Initialize the evaluator.
    /// </summary>
    public Task InitializeAsync();

    /// <summary>
    ///     Shutdown the evaluator.
    /// </summary>
    public Task DisposeAsync();

    /// <summary>
    ///     Evaluate an Object flag.
    /// </summary>
    /// <param name="flagKey">name of the feature flag</param>
    /// <param name="defaultValue">default value</param>
    /// <param name="evaluationContext">evaluation context of the evaluation</param>
    /// <returns>An ResolutionDetails response</returns>
    public Task<ResolutionDetails<Value>> Evaluate(string flagKey, Value defaultValue,
        EvaluationContext evaluationContext);

    /// <summary>
    ///     Evaluate a string flag.
    /// </summary>
    /// <param name="flagKey">name of the feature flag</param>
    /// <param name="defaultValue">default value</param>
    /// <param name="evaluationContext">evaluation context of the evaluation</param>
    /// <returns>An ResolutionDetails response</returns>
    public Task<ResolutionDetails<string>> Evaluate(string flagKey, string defaultValue,
        EvaluationContext evaluationContext);

    /// <summary>
    ///     Evaluate an int flag.
    /// </summary>
    /// <param name="flagKey">name of the feature flag</param>
    /// <param name="defaultValue">default value</param>
    /// <param name="evaluationContext">evaluation context of the evaluation</param>
    /// <returns>An ResolutionDetails response</returns>
    public Task<ResolutionDetails<int>> Evaluate(string flagKey, int defaultValue,
        EvaluationContext evaluationContext);

    /// <summary>
    ///     Evaluate a double flag.
    /// </summary>
    /// <param name="flagKey">name of the feature flag</param>
    /// <param name="defaultValue">default value</param>
    /// <param name="evaluationContext">evaluation context of the evaluation</param>
    /// <returns>An ResolutionDetails response</returns>
    public Task<ResolutionDetails<double>> Evaluate(string flagKey, double defaultValue,
        EvaluationContext evaluationContext);

    /// <summary>
    ///     Evaluate a boolean flag.
    /// </summary>
    /// <param name="flagKey">name of the feature flag</param>
    /// <param name="defaultValue">default value</param>
    /// <param name="evaluationContext">evaluation context of the evaluation</param>
    /// <returns>An ResolutionDetails response</returns>
    public Task<ResolutionDetails<bool>> Evaluate(string flagKey, bool defaultValue,
        EvaluationContext evaluationContext);

    /// <summary>
    ///     Check if the flag is trackable.
    /// </summary>
    /// <param name="flagKey">name of the feature flag</param>
    /// <returns>true if the flag is trackable</returns>
    public bool IsFlagTrackable(string flagKey);
}
