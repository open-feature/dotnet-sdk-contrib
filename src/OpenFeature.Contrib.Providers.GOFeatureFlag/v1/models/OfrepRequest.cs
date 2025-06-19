using System.Collections.Generic;
using System.Text.Json;
using OpenFeature.Contrib.Providers.GOFeatureFlag.v1.converters;
using OpenFeature.Contrib.Providers.GOFeatureFlag.v1.exception;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.v1.models;

/// <summary>
///     GO Feature Flag request to be sent to the evaluation API
/// </summary>
public class OfrepRequest
{
    private const string KeyField = "targetingKey";
    private readonly EvaluationContext _ctx;

    /// <summary>
    ///     Create a new GO Feature Flag request to be sent to the evaluation API
    /// </summary>
    /// <param name="ctx"></param>
    /// <exception cref="InvalidEvaluationContext"></exception>
    /// <exception cref="InvalidTargetingKey"></exception>
    public OfrepRequest(EvaluationContext ctx)
    {
        try
        {
            if (ctx is null)
            {
                throw new InvalidEvaluationContext("GO Feature Flag need an Evaluation context to work.");
            }

            if (!ctx.GetValue(KeyField).IsString)
            {
                throw new InvalidTargetingKey("targetingKey field MUST be a string.");
            }
        }
        catch (KeyNotFoundException e)
        {
            throw new InvalidTargetingKey("targetingKey field is mandatory.", e);
        }

        this._ctx = ctx;
    }

    /// <summary>
    ///     Returns the JSON request as string to be sent to the API
    /// </summary>
    /// <returns>JSON request as string to be sent to the API</returns>
    public string AsJsonString()
    {
        var request = new Dictionary<string, object> { { "context", this._ctx.AsDictionary() } };
        return JsonSerializer.Serialize(request, JsonConverterExtensions.DefaultSerializerSettings);
    }
}
