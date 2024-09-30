using System;
using System.Net;
using System.Net.Http;
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
public class FliptToOpenFeatureConverter(Evaluation fliptEvaluationClient, string namespaceKey = "default")
    : IFliptToOpenFeatureConverter
{
    /// <summary>
    ///     Wrapper that uses Flipt to OpenFeature compliant models
    /// </summary>
    /// <param name="fliptUrl">Url of flipt instance</param>
    /// <param name="namespaceKey">Namespace used for querying flags</param>
    /// <param name="clientToken">Authentication access token</param>
    /// <param name="timeoutInSeconds">Timeout when calling flipt endpoints in seconds</param>
    public FliptToOpenFeatureConverter(string fliptUrl,
        string namespaceKey = "default",
        string clientToken = "",
        int timeoutInSeconds = 30) : this(BuildClient(fliptUrl, clientToken, timeoutInSeconds).Evaluation, namespaceKey)
    {
    }

    /// <inheritdoc />
    public async Task<ResolutionDetails<T>> EvaluateAsync<T>(string flagKey, T defaultValue, EvaluationContext context)
    {
        var evaluationRequest = new EvaluationRequest(namespaceKey, flagKey, context?.TargetingKey ?? "",
            context.ToStringDictionary());

        if (fliptEvaluationClient == null)
            return new ResolutionDetails<T>(flagKey, defaultValue, ErrorType.ProviderNotReady);

        try
        {
            var evaluationResponse = await fliptEvaluationClient.EvaluateVariantAsync(evaluationRequest);
            if (!(evaluationResponse?.Match ?? false))
                return new ResolutionDetails<T>(flagKey, defaultValue, ErrorType.None,
                    evaluationResponse?.Reason.ToString());
            // Todo Andrei:
            // Create another layer that just converts errors and responses to OpenFeature compliant models and responses
            try
            {
                var convertedValue = (T)Convert.ChangeType(evaluationResponse.VariantKey, typeof(T));
                return new ResolutionDetails<T>(flagKey,
                    convertedValue, ErrorType.None,
                    evaluationResponse.Reason.ToString());
            }
            catch (InvalidCastException)
            {
                // handle differently if type is Value
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
        catch (HttpRequestException ex)
        {
            return ResolutionDetailFromHttpException(ex, flagKey, defaultValue);
        }
    }

    /// <inheritdoc />
    public async Task<ResolutionDetails<bool>> EvaluateBooleanAsync(string flagKey, bool defaultValue,
        EvaluationContext context)
    {
        if (fliptEvaluationClient == null)
            return new ResolutionDetails<bool>(flagKey, defaultValue, ErrorType.ProviderNotReady);
        try
        {
            var evaluationRequest = new EvaluationRequest(namespaceKey, flagKey, context?.TargetingKey ?? "",
                context.ToStringDictionary());

            var boolEvaluationResponse = await fliptEvaluationClient.EvaluateBooleanAsync(evaluationRequest);
            return new ResolutionDetails<bool>(flagKey, boolEvaluationResponse?.Enabled ?? defaultValue, ErrorType.None,
                boolEvaluationResponse?.Reason.ToString());
        }
        catch (HttpRequestException ex)
        {
            return ResolutionDetailFromHttpException(ex, flagKey, defaultValue);
        }
    }

    private static ResolutionDetails<T> ResolutionDetailFromHttpException<T>(HttpRequestException e, string flagKey,
        T defaultValue)
    {
        var error = e.StatusCode switch
        {
            HttpStatusCode.NotFound => ErrorType.FlagNotFound,
            HttpStatusCode.BadRequest => ErrorType.TypeMismatch,
            HttpStatusCode.InternalServerError => ErrorType.ProviderNotReady,
            _ => ErrorType.General
        };
        return new ResolutionDetails<T>(flagKey, defaultValue, error, errorMessage: e.Message);
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
public interface IFliptToOpenFeatureConverter
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