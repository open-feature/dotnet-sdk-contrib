using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Flipt.Authentication;
using Flipt.Clients;
using Flipt.DTOs;
using Flipt.Utilities;
using OpenFeature.Constant;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.Flipt;

/// <summary>
///     A wrapper of fliptClient to handle data casting and error mappings to OpenFeature models
/// </summary>
public class FliptClientWrapper : IFliptClientWrapper
{
    private readonly Evaluation _fliptEvaluationClient;
    private readonly string _namespaceKey;

    /// <summary>
    ///     Wrapper that uses Flipt to OpenFeature compliant models
    /// </summary>
    /// <param name="fliptUrl">Url of flipt instance</param>
    /// <param name="namespaceKey">Namespace used for querying flags</param>
    /// <param name="clientToken">Authentication access token</param>
    /// <param name="timeoutInSeconds">Timeout when calling flipt endpoints in seconds</param>
    public FliptClientWrapper(string fliptUrl,
        string namespaceKey = "default",
        string clientToken = "",
        int timeoutInSeconds = 30)
    {
        _fliptEvaluationClient = BuildClient(fliptUrl, clientToken, timeoutInSeconds).Evaluation;
        _namespaceKey = namespaceKey;
    }

    /// <inheritdoc />
    public async Task<ResolutionDetails<T>> EvaluateAsync<T>(string flagKey, T defaultValue, EvaluationContext context)
    {
        var evaluationRequest = new EvaluationRequest(_namespaceKey, flagKey, context?.TargetingKey ?? "",
            context?.AsDictionary().ToDictionary(k => k.Key, v => JsonSerializer.Serialize(v.Value)) ?? []);

        try
        {
            var evaluationResponse = await _fliptEvaluationClient.EvaluateVariantAsync(evaluationRequest);
            if (!(evaluationResponse?.Match ?? false))
                return new ResolutionDetails<T>(flagKey, defaultValue, ErrorType.None,
                    evaluationResponse?.Reason.ToString());

            try
            {
                var convertedValue = (T)Convert.ChangeType(evaluationResponse.VariantKey, typeof(T));
                return new ResolutionDetails<T>(flagKey,
                    convertedValue, ErrorType.None,
                    evaluationResponse.Reason.ToString());
            }
            catch (InvalidCastException)
            {
                // cannot change type if of type Value
                if (typeof(T) == typeof(Value))
                    return new ResolutionDetails<T>(flagKey,
                        (T)Convert.ChangeType(new Value(evaluationResponse.VariantAttachment), typeof(T)),
                        ErrorType.None, evaluationResponse.Reason.ToString());
                return new ResolutionDetails<T>(flagKey, defaultValue, ErrorType.TypeMismatch);
            }
            catch (FormatException)
            {
                return new ResolutionDetails<T>(flagKey, defaultValue, ErrorType.TypeMismatch);
            }
        }
        catch (HttpRequestException e)
        {
            return e.StatusCode == HttpStatusCode.NotFound
                ? new ResolutionDetails<T>(flagKey, defaultValue, ErrorType.FlagNotFound)
                : new ResolutionDetails<T>(flagKey, defaultValue, ErrorType.General);
        }
    }

    /// <inheritdoc />
    public async Task<ResolutionDetails<bool>> EvaluateBooleanAsync(string flagKey, bool defaultValue,
        EvaluationContext context)
    {
        var evaluationRequest = new EvaluationRequest(_namespaceKey, flagKey, context?.TargetingKey ?? "",
            context?.AsDictionary().ToDictionary(k => k.Key, v => JsonSerializer.Serialize(v.Value)) ?? []);

        try
        {
            var boolEvaluationResponse = await _fliptEvaluationClient.EvaluateBooleanAsync(evaluationRequest);
            return new ResolutionDetails<bool>(flagKey, boolEvaluationResponse?.Enabled ?? defaultValue, ErrorType.None,
                boolEvaluationResponse?.Reason.ToString());
        }
        catch (HttpRequestException e)
        {
            return e.StatusCode == HttpStatusCode.NotFound
                ? new ResolutionDetails<bool>(flagKey, defaultValue, ErrorType.FlagNotFound)
                : new ResolutionDetails<bool>(flagKey, defaultValue, ErrorType.General);
        }
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
///     Contract for fliptClient wrapper
/// </summary>
public interface IFliptClientWrapper
{
    /// <summary>
    ///     Used for evaluating non-boolean flags. Flipt handles datatypes which is not boolean as variants
    /// </summary>
    /// <param name="flagKey"></param>
    /// <param name="defaultValue"></param>
    /// <param name="context"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns>OpenFeature ResolutionDetails object</returns>
    Task<ResolutionDetails<T>> EvaluateAsync<T>(string flagKey, T defaultValue, EvaluationContext context);

    /// <summary>
    ///     Used for evaluating boolean flags
    /// </summary>
    /// <param name="flagKey"></param>
    /// <param name="defaultValue"></param>
    /// <param name="context"></param>
    /// <returns>OpenFeature ResolutionDetails object</returns>
    Task<ResolutionDetails<bool>> EvaluateBooleanAsync(string flagKey, bool defaultValue, EvaluationContext context);
}