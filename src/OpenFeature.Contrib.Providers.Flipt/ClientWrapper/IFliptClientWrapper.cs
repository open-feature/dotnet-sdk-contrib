using System.Threading.Tasks;
using Flipt.Rest;

namespace OpenFeature.Contrib.Providers.Flipt.ClientWrapper;

/// <summary>
/// </summary>
public interface IFliptClientWrapper
{
    /// <summary>
    ///     Wrapper to Flipt.io/EvaluateVariantAsync method
    /// </summary>
    /// <param name="evaluationRequest"></param>
    /// <returns></returns>
    Task<VariantEvaluationResponse> EvaluateVariantAsync(EvaluationRequest evaluationRequest);

    /// <summary>
    ///     Wrapper to Flipt.io/EvaluateBooleanAsync method
    /// </summary>
    /// <param name="evaluationRequest"></param>
    /// <returns></returns>
    Task<BooleanEvaluationResponse> EvaluateBooleanAsync(EvaluationRequest evaluationRequest);
}
