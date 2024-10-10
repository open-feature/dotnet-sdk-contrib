using System;
using System.Net.Http;
using System.Threading.Tasks;
using Flipt.Rest;

namespace OpenFeature.Contrib.Providers.Flipt.ClientWrapper;

/// <summary>
///     Wrapper for Flipt server sdk client for .net
/// </summary>
public class FliptClientWrapper : IFliptClientWrapper
{
    private readonly FliptRestClient _fliptRestClient;

    /// <summary>
    /// </summary>
    /// <param name="fliptUrl">Url of flipt instance</param>
    /// <param name="clientToken">Authentication access token</param>
    /// <param name="timeoutInSeconds">Timeout when calling flipt endpoints in seconds</param>
    public FliptClientWrapper(string fliptUrl,
        string clientToken = "",
        int timeoutInSeconds = 30)
    {
        _fliptRestClient = BuildClient(fliptUrl, clientToken, timeoutInSeconds);
    }

    /// <inheritdoc />
    public async Task<VariantEvaluationResponse> EvaluateVariantAsync(EvaluationRequest evaluationRequest)
    {
        return await _fliptRestClient.EvaluateV1VariantAsync(evaluationRequest);
    }

    /// <inheritdoc />
    public async Task<BooleanEvaluationResponse> EvaluateBooleanAsync(EvaluationRequest evaluationRequest)
    {
        return await _fliptRestClient.EvaluateV1BooleanAsync(evaluationRequest);
    }

    private static FliptRestClient BuildClient(string fliptUrl, string clientToken, int timeoutInSeconds = 30)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(fliptUrl),
            Timeout = TimeSpan.FromSeconds(timeoutInSeconds),
            DefaultRequestHeaders = { { "Authorization", $"Bearer {clientToken}" } }
        };
        return new FliptRestClient(httpClient);
    }
}