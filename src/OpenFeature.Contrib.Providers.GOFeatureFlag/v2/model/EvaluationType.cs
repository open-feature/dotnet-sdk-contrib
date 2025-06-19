namespace OpenFeature.Contrib.Providers.GOFeatureFlag.v2.model;

/// <summary>
///     This enum represents the type of evaluation that can be performed.
/// </summary>
public enum EvaluationType
{
    /// <summary>
    ///     InProcess: The evaluation is done in the process of the application.
    /// </summary>
    InProcess,

    /// <summary>
    ///     Remote: The evaluation is done on the edge (e.g., CDN or API).
    /// </summary>
    Remote
}
