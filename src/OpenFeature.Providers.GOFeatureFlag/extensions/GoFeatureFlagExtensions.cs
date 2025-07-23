using System.Collections.Generic;
using OpenFeature.Model;

namespace OpenFeature.Providers.GOFeatureFlag.Extensions;

/// <summary>
///     Extensions for GO Feature Flag provider.
/// </summary>
public static class GOFeatureFlagExtensions
{
    /// <summary>
    ///     Convert a Dictionary to an ImmutableMetadata.
    /// </summary>
    /// <param name="metadataDictionary"></param>
    /// <returns></returns>
    public static ImmutableMetadata?
        ToImmutableMetadata(this Dictionary<string, object>? metadataDictionary) // 'this' keyword is crucial
    {
        return metadataDictionary != null ? new ImmutableMetadata(metadataDictionary) : null;
    }

    /// <summary>
    /// Extension method to check if the evaluation context is anonymous.
    /// </summary>
    /// <param name="evaluationContext">The evaluation context to check.</param>
    public static bool IsAnonymous(this EvaluationContext? evaluationContext)
    {
        try
        {
            if (evaluationContext == null) { return false; }

            var anonymousField = evaluationContext.GetValue("anonymous");
            if (anonymousField.AsBoolean == true) { return true; }

            return false;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
    }
}
