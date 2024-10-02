using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Flipt.Rest;
using OpenFeature.Constant;
using OpenFeature.Contrib.Providers.Flipt.ClientWrapper;
using OpenFeature.Contrib.Providers.Flipt.Converters;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.Flipt;

/// <summary>
///     A wrapper of fliptClient to handle data casting and error mappings to OpenFeature models
/// </summary>
public class FliptToOpenFeatureConverter(IFliptClientWrapper fliptClientWrapper, string namespaceKey = "default")
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
        int timeoutInSeconds = 30) : this(new FliptClientWrapper(fliptUrl, clientToken, timeoutInSeconds),
        namespaceKey)
    {
    }

    /// <inheritdoc />
    public async Task<ResolutionDetails<T>> EvaluateAsync<T>(string flagKey, T defaultValue,
        EvaluationContext context = null)
    {
        var evaluationRequest = new EvaluationRequest
        {
            NamespaceKey = namespaceKey,
            FlagKey = flagKey,
            EntityId = context?.TargetingKey ?? "",
            Context = context.ToStringDictionary()
        };

        try
        {
            var evaluationResponse = await fliptClientWrapper.EvaluateVariantAsync(evaluationRequest);

            if (evaluationResponse.Reason == EvaluationReason.FLAG_DISABLED_EVALUATION_REASON)
                return new ResolutionDetails<T>(flagKey, defaultValue, ErrorType.None,
                    evaluationResponse.Reason.ToString());

            if (!evaluationResponse.Match)
                return new ResolutionDetails<T>(flagKey, defaultValue, ErrorType.None,
                    evaluationResponse.Reason.ToString());
            try
            {
                if (string.IsNullOrEmpty(evaluationResponse.VariantAttachment))
                {
                    var convertedValue = (T)Convert.ChangeType(evaluationResponse.VariantKey, typeof(T));
                    return new ResolutionDetails<T>(flagKey,
                        convertedValue, ErrorType.None,
                        evaluationResponse.Reason.ToString(), evaluationResponse.VariantKey);
                }

                var deserializedValueObj = JsonSerializer.Deserialize<Value>(evaluationResponse.VariantAttachment,
                    JsonConverterExtensions.DefaultSerializerSettings);

                return new ResolutionDetails<T>(flagKey,
                    (T)Convert.ChangeType(deserializedValueObj, typeof(T)),
                    ErrorType.None, evaluationResponse.Reason.ToString(), evaluationResponse.VariantKey);
            }
            catch (Exception ex)
            {
                if (ex is InvalidCastException or FormatException)
                    return new ResolutionDetails<T>(flagKey, defaultValue, ErrorType.TypeMismatch);
            }
        }
        catch (FliptException ex)
        {
            return ResolutionDetailFromFliptException(ex, flagKey, defaultValue);
        }

        return new ResolutionDetails<T>(flagKey, defaultValue, ErrorType.General);
    }

    /// <inheritdoc />
    public async Task<ResolutionDetails<bool>> EvaluateBooleanAsync(string flagKey, bool defaultValue,
        EvaluationContext context = null)
    {
        try
        {
            var evaluationRequest = new EvaluationRequest
            {
                NamespaceKey = namespaceKey,
                FlagKey = flagKey,
                EntityId = context?.TargetingKey ?? "",
                Context = context.ToStringDictionary()
            };
            var boolEvaluationResponse = await fliptClientWrapper.EvaluateBooleanAsync(evaluationRequest);
            return new ResolutionDetails<bool>(flagKey, boolEvaluationResponse.Enabled, ErrorType.None,
                boolEvaluationResponse.Reason.ToString());
        }
        catch (FliptException ex)
        {
            return ResolutionDetailFromFliptException(ex, flagKey, defaultValue);
        }
    }

    private static ResolutionDetails<T> ResolutionDetailFromHttpException<T>(HttpRequestException e, string flagKey,
        T defaultValue)
    {
        var error = e.StatusCode switch
        {
            HttpStatusCode.NotFound => ErrorType.FlagNotFound,
            HttpStatusCode.BadRequest => ErrorType.TypeMismatch,
            HttpStatusCode.Forbidden => ErrorType.ProviderNotReady,
            HttpStatusCode.InternalServerError => ErrorType.ProviderNotReady,
            _ => ErrorType.General
        };
        return new ResolutionDetails<T>(flagKey, defaultValue, error, errorMessage: e.Message);
    }

    private static ResolutionDetails<T> ResolutionDetailFromFliptException<T>(FliptException e, string flagKey,
        T defaultValue)
    {
        var error = (HttpStatusCode)e.StatusCode switch
        {
            HttpStatusCode.NotFound => ErrorType.FlagNotFound,
            HttpStatusCode.BadRequest => ErrorType.TypeMismatch,
            HttpStatusCode.Forbidden => ErrorType.ProviderNotReady,
            HttpStatusCode.InternalServerError => ErrorType.ProviderNotReady,
            _ => ErrorType.General
        };
        return new ResolutionDetails<T>(flagKey, defaultValue, error, errorMessage: e.Message);
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
    Task<ResolutionDetails<T>> EvaluateAsync<T>(string flagKey, T defaultValue, EvaluationContext context = null);

    /// <summary>
    ///     Used for evaluating boolean flags
    /// </summary>
    /// <param name="flagKey"></param>
    /// <param name="defaultValue"></param>
    /// <param name="context"></param>
    /// <returns>OpenFeature ResolutionDetails object</returns>
    Task<ResolutionDetails<bool>> EvaluateBooleanAsync(string flagKey, bool defaultValue,
        EvaluationContext context = null);
}