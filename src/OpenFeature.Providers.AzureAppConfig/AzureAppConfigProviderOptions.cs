namespace OpenFeature.Providers.AzureAppConfig;

/// <summary>
/// Configuration options for the Azure App Configuration provider.
/// </summary>
public class AzureAppConfigProviderOptions
{
    /// <summary>
    /// Gets or sets the prefix used for feature flags in Azure App Configuration.
    /// Default is ".appconfig.featureflag/".
    /// </summary>
    public string FeatureFlagPrefix { get; set; } = ".appconfig.featureflag/";

    /// <summary>
    /// Gets or sets the prefix used for configuration values in Azure App Configuration.
    /// Default is empty string.
    /// </summary>
    public string ConfigurationPrefix { get; set; } = "";

    /// <summary>
    /// Gets or sets the label to filter configuration values. If null, no label filter is applied.
    /// </summary>
    public string? Label { get; set; }
}
