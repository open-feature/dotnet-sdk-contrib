using System.Text.Json;
using Azure.Data.AppConfiguration;
using OpenFeature.Constant;
using OpenFeature.Model;

namespace OpenFeature.Providers.AzureAppConfig;

/// <summary>
/// An OpenFeature provider for Azure App Configuration boolean feature flags.
/// </summary>
public sealed class AzureAppConfigProvider : FeatureProvider
{
    private const string Name = "Azure App Configuration Provider";
    private readonly ConfigurationClient _configurationClient;
    private readonly AzureAppConfigProviderOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureAppConfigProvider"/> class.
    /// </summary>
    /// <param name="connectionString">The Azure App Configuration connection string.</param>
    /// <param name="options">Optional configuration options for the provider.</param>
    /// <exception cref="ArgumentNullException">Thrown when connectionString is null or empty.</exception>
    public AzureAppConfigProvider(string connectionString, AzureAppConfigProviderOptions? options = null)
    {
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty.");

        this._options = options ?? new AzureAppConfigProviderOptions();
        this._configurationClient = new ConfigurationClient(connectionString);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureAppConfigProvider"/> class.
    /// </summary>
    /// <param name="configurationClient">The Azure App Configuration client.</param>
    /// <param name="options">Optional configuration options for the provider.</param>
    /// <exception cref="ArgumentNullException">Thrown when configurationClient is null.</exception>
    public AzureAppConfigProvider(ConfigurationClient configurationClient, AzureAppConfigProviderOptions? options = null)
    {
        this._configurationClient = configurationClient ?? throw new ArgumentNullException(nameof(configurationClient));
        this._options = options ?? new AzureAppConfigProviderOptions();
    }

    /// <inheritdoc/>
    public override Metadata GetMetadata()
    {
        return new Metadata(Name);
    }

    /// <inheritdoc/>
    public override async Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(string flagKey, bool defaultValue, EvaluationContext? context = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var featureFlagKey = this.GetFeatureFlagKey(flagKey);
            var configValue = await this._configurationClient.GetConfigurationSettingAsync(featureFlagKey, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (configValue?.Value?.Value == null)
            {
                return new ResolutionDetails<bool>(flagKey, defaultValue, ErrorType.FlagNotFound, reason: Reason.Error, errorMessage: "Feature flag not found");
            }

            FeatureFlag? featureFlag;
            try
            {
                featureFlag = JsonSerializer.Deserialize<FeatureFlag>(configValue.Value.Value);
            }
            catch (JsonException)
            {
                return new ResolutionDetails<bool>(flagKey, defaultValue, ErrorType.General, reason: Reason.Error, errorMessage: "Failed to deserialize feature flag");
            }

            if (featureFlag == null)
            {
                return new ResolutionDetails<bool>(flagKey, defaultValue, ErrorType.General, reason: Reason.Error, errorMessage: "Failed to deserialize feature flag");
            }

            if (!featureFlag.Enabled)
            {
                return new ResolutionDetails<bool>(flagKey, defaultValue, reason: Reason.Disabled);
            }

            var result = GetVariantValue(featureFlag);

            return new ResolutionDetails<bool>(flagKey, result, reason: Reason.Static);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return new ResolutionDetails<bool>(flagKey, defaultValue, ErrorType.FlagNotFound, reason: Reason.Error, errorMessage: ex.Message);
        }
        catch (Exception ex)
        {
            return new ResolutionDetails<bool>(flagKey, defaultValue, ErrorType.General, reason: Reason.Error, errorMessage: ex.Message);
        }
    }

    /// <inheritdoc/>
    public override Task<ResolutionDetails<string>> ResolveStringValueAsync(string flagKey, string defaultValue, EvaluationContext? context = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ResolutionDetails<string>(flagKey, defaultValue, ErrorType.TypeMismatch, reason: Reason.Error, errorMessage: "String values are not supported. Use boolean feature flags only."));
    }

    /// <inheritdoc/>
    public override Task<ResolutionDetails<int>> ResolveIntegerValueAsync(string flagKey, int defaultValue, EvaluationContext? context = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ResolutionDetails<int>(flagKey, defaultValue, ErrorType.TypeMismatch, reason: Reason.Error, errorMessage: "Integer values are not supported. Use boolean feature flags only."));
    }

    /// <inheritdoc/>
    public override Task<ResolutionDetails<double>> ResolveDoubleValueAsync(string flagKey, double defaultValue, EvaluationContext? context = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ResolutionDetails<double>(flagKey, defaultValue, ErrorType.TypeMismatch, reason: Reason.Error, errorMessage: "Double values are not supported. Use boolean feature flags only."));
    }

    /// <inheritdoc/>
    public override Task<ResolutionDetails<Value>> ResolveStructureValueAsync(string flagKey, Value defaultValue, EvaluationContext? context = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ResolutionDetails<Value>(flagKey, defaultValue, ErrorType.TypeMismatch, reason: Reason.Error, errorMessage: "Structure values are not supported. Use boolean feature flags only."));
    }

    private string GetFeatureFlagKey(string flagKey)
    {
        return this._options.FeatureFlagPrefix + flagKey;
    }

    private static bool GetVariantValue(FeatureFlag featureFlag)
    {
        if (featureFlag.Variants == null || featureFlag.Variants.Count == 0)
        {
            throw new InvalidOperationException("Feature flag has no variants");
        }

        var variant = featureFlag.Variants.FirstOrDefault(v =>
            string.Equals(v.Name, featureFlag.Allocation.DefaultWhenEnabled, StringComparison.OrdinalIgnoreCase));

        // If variant found, return its value; otherwise throw an exception
        return variant?.ConfigurationValue ?? throw new InvalidOperationException("Feature flag has no variants");
    }
}
