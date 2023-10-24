namespace OpenFeature.Contrib.Providers.Flagsmith;

/// <summary>
/// Settings for Flagsmith open feature provider
/// </summary>
public class FlagsmithProviderConfiguration : IFlagsmithProviderConfiguration
{
    /// <summary>
    /// Key that will be used as identity for Flagsmith requests. Default: "targetingKey"
    /// </summary>
    public string TargetingKey { get; set; } = "targetingKey";

    /// <inheritdoc/>
    public bool UsingBooleanConfigValue { get; set; }
}
