namespace OpenFeature.Contrib.Providers.Flagsmith;

/// <summary>
/// Settings for Flagsmith open feature provider
/// </summary>
public class FlagsmithProviderConfiguration : IFlagsmithProviderConfiguration
{
    /// <summary>
    /// Default value for targeting key
    /// </summary>
    public const string DefaultTargetingKey = "targetingKey";

    /// <summary>
    /// Key that will be used as identity for Flagsmith requests. Default: "targetingKey"
    /// </summary>
    public string TargetingKey { get; set; } = DefaultTargetingKey;

    /// <inheritdoc/>
    public bool UsingBooleanConfigValue { get; set; }
}
