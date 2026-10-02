namespace OpenFeature.Providers.GOFeatureFlag.Models;

/// <summary>
///     FlagConfigNotModified is returned when the relay proxy answers 304 Not Modified.
///     It carries no configuration on purpose, so the configuration in use cannot be overwritten by a 304.
/// </summary>
public sealed class FlagConfigNotModified : FlagConfigResult
{
    /// <summary>
    ///     The single instance of <see cref="FlagConfigNotModified" />.
    /// </summary>
    public static readonly FlagConfigNotModified Instance = new();

    private FlagConfigNotModified()
    {
    }
}
