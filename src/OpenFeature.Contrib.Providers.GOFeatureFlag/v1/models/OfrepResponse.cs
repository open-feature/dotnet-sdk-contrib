using System.Collections.Generic;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.v1.models;

/// <summary>
///     OfrepResponse is the response returned by the OFREP API.
/// </summary>
public class OfrepResponse
{
    /// <summary>
    ///     value contains the result of the flag.
    /// </summary>
    public object Value { get; set; }

    /// <summary>
    ///     key contains the name of the feature flag.
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    ///     reason used to choose this variation.
    /// </summary>
    public string Reason { get; set; }

    /// <summary>
    ///     variationType contains the name of the variation used for this flag.
    /// </summary>
    public string Variant { get; set; }

    /// <summary>
    ///     cacheable is true if the flag is cacheable.
    /// </summary>
    public bool Cacheable { get; set; }

    /// <summary>
    ///     errorCode is empty if everything went ok.
    /// </summary>
    public string ErrorCode { get; set; }

    /// <summary>
    ///     errorDetails is set only if errorCode is not empty.
    /// </summary>
    public string ErrorDetails { get; set; }

    /// <summary>
    ///     metadata contains the metadata of the flag.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; }
}
