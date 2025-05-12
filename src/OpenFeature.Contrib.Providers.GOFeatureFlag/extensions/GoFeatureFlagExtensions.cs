using System.Collections.Generic;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.extensions;

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
}
