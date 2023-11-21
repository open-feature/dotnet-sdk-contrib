using io.harness.cfsdk.client.dto;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.Harness;

/// <summary>
/// HarnessAdapter is the .NET adapter for the Harness feature flag SDK
/// It provides functions to convert the OpenFeature EvaluationContext to
/// a harness target, and functions to convert the Harness evaluation result
/// to a OpenFeature ResolutionDetails.
/// </summary>
public static class HarnessAdapter
{
    /// <summary>
    /// Convert the Harness evaluation result to a OpenFeature ResolutionDetails.
    /// </summary>
    /// <param name="flagKey"></param>
    /// <param name="defaultValue"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static ResolutionDetails<T> HarnessResponse<T>(string flagKey, T defaultValue)
    {
        /*
         * string flagKey, T value, ErrorType errorType = ErrorType.None, string reason = null,
           string variant = null, string errorMessage = null
         */
        return new ResolutionDetails<T>(
            flagKey,
            defaultValue);
    }

    /// <summary>
    /// Convert the OpenFeature EvaluationContext to a harness target.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static Target CreateTarget(EvaluationContext context)
    {
        // Get the identifier, if it is missing or empty return null
        if (context.TryGetValue("identifier", out var identifier) != true || identifier.IsString != true)
        {
            return null;
        }

        // Get the name, if it is missing or empty return null
        if (context.TryGetValue("name", out var name) != true || name.IsString != true)
        {
            return null;
        }

        // Create a target (different targets can get different results based on rules)
        // TODO we need to deal with target attributes
        //  .Attributes(new Dictionary<string, string>(){{"email", "demo@harness.io"}})
        Target target = Target.builder()
            .Name(name.AsString)
            .Identifier(identifier.AsString)
            .build();
        return target;

    }


}