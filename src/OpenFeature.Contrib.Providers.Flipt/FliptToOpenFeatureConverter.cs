using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Flipt.Rest;
using OpenFeature.Constant;
using OpenFeature.Contrib.Providers.Flipt.ClientWrapper;
using OpenFeature.Contrib.Providers.Flipt.Converters;
using OpenFeature.Error;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.Flipt;

/// <summary>
///     A wrapper of fliptClient to handle data casting and error mappings to OpenFeature models
/// </summary>
public class FliptToOpenFeatureConverter : IFliptToOpenFeatureConverter
{
    private readonly IFliptClientWrapper _fliptClientWrapper;
    private readonly string _namespaceKey;

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

    internal FliptToOpenFeatureConverter(IFliptClientWrapper fliptClientWrapper, string namespaceKey = "default")
    {
        _fliptClientWrapper = fliptClientWrapper;
        _namespaceKey = namespaceKey;
    }

    /// <inheritdoc />
    public async Task<ResolutionDetails<T>> EvaluateAsync<T>(string flagKey, T defaultValue,
        EvaluationContext context = null)
    {
        var evaluationRequest = new EvaluationRequest
        {
            NamespaceKey = _namespaceKey,
            FlagKey = flagKey,
            EntityId = context?.TargetingKey ?? "",
            Context = context.ToStringDictionary()
        };

        try
        {
            var evaluationResponse = await _fliptClientWrapper.EvaluateVariantAsync(evaluationRequest).ConfigureAwait(false);

            if (evaluationResponse.Reason == VariantEvaluationResponseReason.FLAG_DISABLED_EVALUATION_REASON)
                return new ResolutionDetails<T>(flagKey, defaultValue, ErrorType.None,
                    Reason.Disabled);

            if (!evaluationResponse.Match)
                return new ResolutionDetails<T>(flagKey, defaultValue, ErrorType.None,
                    Reason.Default);
            try
            {
                if (string.IsNullOrEmpty(evaluationResponse.VariantAttachment))
                {
                    var convertedValue = (T)Convert.ChangeType(evaluationResponse.VariantKey, typeof(T));
                    return new ResolutionDetails<T>(flagKey,
                        convertedValue, ErrorType.None,
                        Reason.TargetingMatch, evaluationResponse.VariantKey);
                }

                var deserializedValueObj = JsonSerializer.Deserialize<Value>(evaluationResponse.VariantAttachment,
                    JsonConverterExtensions.DefaultSerializerSettings);

                return new ResolutionDetails<T>(flagKey,
                    (T)Convert.ChangeType(deserializedValueObj, typeof(T)),
                    ErrorType.None, Reason.TargetingMatch, evaluationResponse.VariantKey);
            }
            catch (Exception ex)
            {
                if (ex is InvalidCastException or FormatException)
                    throw new TypeMismatchException(ex.Message, ex);
            }
        }
        catch (FliptRestException ex)
        {
            throw HttpRequestExceptionFromFliptRestException(ex);
        }

        return new ResolutionDetails<T>(flagKey, defaultValue, ErrorType.General, Reason.Unknown);
    }

    /// <inheritdoc />
    public async Task<ResolutionDetails<bool>> EvaluateBooleanAsync(string flagKey, bool defaultValue,
        EvaluationContext context = null)
    {
        try
        {
            var evaluationRequest = new EvaluationRequest
            {
                NamespaceKey = _namespaceKey,
                FlagKey = flagKey,
                EntityId = context?.TargetingKey ?? "",
                Context = context.ToStringDictionary()
            };
            var boolEvaluationResponse = await _fliptClientWrapper.EvaluateBooleanAsync(evaluationRequest).ConfigureAwait(false);
            return new ResolutionDetails<bool>(flagKey, boolEvaluationResponse.Enabled, ErrorType.None,
                Reason.TargetingMatch);
        }
        catch (FliptRestException ex)
        {
            throw HttpRequestExceptionFromFliptRestException(ex);
        }
    }

    private static Exception HttpRequestExceptionFromFliptRestException(FliptRestException e)
    {
        return new HttpRequestException(e.Message, e);
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
