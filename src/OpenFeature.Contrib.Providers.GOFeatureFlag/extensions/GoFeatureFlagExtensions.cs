using System.Collections.Generic;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.v2.extensions;

/// <summary>
///     Extensions for GO Feature Flag provider.
/// </summary>
public static class GoFeatureFlagExtensions
{
    /// <summary>
    ///     Convert a Dictionary to an ImmutableMetadata.
    /// </summary>
    /// <param name="metadataDictionary"></param>
    /// <returns></returns>
    public static ImmutableMetadata
        ToImmutableMetadata(this Dictionary<string, object> metadataDictionary) // 'this' keyword is crucial
    {
        return metadataDictionary != null ? new ImmutableMetadata(metadataDictionary) : null;
    }

    public static bool IsAnonymous(this EvaluationContext evaluationContext)
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
