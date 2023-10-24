using Flagsmith;

namespace OpenFeature.Contrib.Providers.Flagsmith;

/// <summary>
/// Settings for Flagsmith Open feature provider
/// </summary>
public interface IFlagsmithProviderConfiguration
{
    /// <summary>
    /// Key that will be used as identity for Flagsmith requests.
    /// </summary>
    public string TargetingKey { get; }

    /// <summary>
    /// Determines whether to resolve a feature value as a boolean or use
    /// the isFeatureEnabled as the flag itself. These values will be false
    /// and true respectively.
    /// Default: false
    /// </summary>
    public bool UsingBooleanConfigValue { get; }
}
