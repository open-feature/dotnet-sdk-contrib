namespace OpenFeature.Contrib.Providers.Flagsmith;

/// <summary>
/// Settings for Flagsmith open feature provider
/// </summary>
public class FlagsmithProviderConfiguration : IFlagsmithProviderConfiguration
{
    /// <inheritdoc/>
    public bool UsingBooleanConfigValue { get; set; }
}
