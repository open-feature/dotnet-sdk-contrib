using System.Threading.Tasks;
using Flipt.Authentication;
using Flipt.Clients;
using Flipt.DTOs;
using Flipt.Utilities;

namespace OpenFeature.Contrib.Providers.Flipt;

/// <summary>
///     Wrapper for Flipt server sdk client for .net
/// </summary>
public class FliptClientWrapper : IFliptClientWrapper
{
    private readonly Evaluation _fliptEvaluationClient;

    /// <summary>
    /// </summary>
    /// <param name="fliptUrl">Url of flipt instance</param>
    /// <param name="clientToken">Authentication access token</param>
    /// <param name="timeoutInSeconds">Timeout when calling flipt endpoints in seconds</param>
    public FliptClientWrapper(string fliptUrl,
        string clientToken = "",
        int timeoutInSeconds = 30)
    {
        _fliptEvaluationClient = BuildClient(fliptUrl, clientToken, timeoutInSeconds).Evaluation;
    }

    /// <inheritdoc />
    public async Task<VariantEvaluationResponse> EvaluateVariantAsync(EvaluationRequest evaluationRequest)
    {
        return await _fliptEvaluationClient.EvaluateVariantAsync(evaluationRequest);
    }

    /// <inheritdoc />
    public async Task<BooleanEvaluationResponse> EvaluateBooleanAsync(EvaluationRequest evaluationRequest)
    {
        return await _fliptEvaluationClient.EvaluateBooleanAsync(evaluationRequest);
    }

    private static FliptClient BuildClient(string fliptUrl, string clientToken, int timeoutInSeconds = 30)
    {
        return FliptClient.Builder()
            .WithUrl(fliptUrl)
            .WithAuthentication(new ClientTokenAuthenticationStrategy(clientToken))
            .WithTimeout(timeoutInSeconds)
            .Build();
    }
}

/// <summary>
/// </summary>
public interface IFliptClientWrapper
{
    /// <summary>
    /// </summary>
    /// <param name="evaluationRequest"></param>
    /// <returns></returns>
    Task<VariantEvaluationResponse> EvaluateVariantAsync(EvaluationRequest evaluationRequest);

    /// <summary>
    /// </summary>
    /// <param name="evaluationRequest"></param>
    /// <returns></returns>
    Task<BooleanEvaluationResponse> EvaluateBooleanAsync(EvaluationRequest evaluationRequest);
}