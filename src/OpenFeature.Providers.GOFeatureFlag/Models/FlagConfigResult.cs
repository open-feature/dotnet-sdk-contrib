namespace OpenFeature.Providers.GOFeatureFlag.Models;

/// <summary>
///     FlagConfigResult is the outcome of a flag configuration request: either a new configuration
///     (<see cref="FlagConfigResponse" />) or <see cref="FlagConfigNotModified" /> when the relay proxy answered
///     304 Not Modified.
/// </summary>
public abstract class FlagConfigResult
{
    /// <summary>
    ///     Only the types of this assembly can represent the outcome of a flag configuration request.
    /// </summary>
    private protected FlagConfigResult()
    {
    }
}
