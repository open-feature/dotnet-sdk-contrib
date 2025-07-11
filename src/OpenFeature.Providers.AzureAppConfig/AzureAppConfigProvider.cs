using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.AppConfiguration;
using OpenFeature.Constant;
using OpenFeature.Model;

namespace OpenFeature.Providers.AzureAppConfig;

/// <summary>
/// An OpenFeature provider for Azure App Configuration with feature flags support.
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
    public AzureAppConfigProvider(string connectionString, AzureAppConfigProviderOptions options = null)
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
    public AzureAppConfigProvider(ConfigurationClient configurationClient, AzureAppConfigProviderOptions options = null)
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
    public override async Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(string flagKey, bool defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var featureFlagKey = this.GetFeatureFlagKey(flagKey);
            var configValue = await this._configurationClient.GetConfigurationSettingAsync(featureFlagKey, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (configValue?.Value?.Value == null)
            {
                return new ResolutionDetails<bool>(flagKey, defaultValue, ErrorType.FlagNotFound, "Feature flag not found");
            }

            var featureFlag = System.Text.Json.JsonSerializer.Deserialize<FeatureFlag>(configValue.Value.Value);

            if (!featureFlag.Enabled)
            {
                return new ResolutionDetails<bool>(flagKey, false);
            }

            // If no conditions are specified, return the enabled state
            if (featureFlag.Conditions?.ClientFilters == null || featureFlag.Conditions.ClientFilters.Count == 0)
            {
                return new ResolutionDetails<bool>(flagKey, true);
            }

            // For this basic implementation, we'll evaluate simple percentage filters
            // In a full implementation, you'd want to integrate with Microsoft.FeatureManagement
            var result = this.EvaluateConditions(featureFlag, context);
            return new ResolutionDetails<bool>(flagKey, result);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return new ResolutionDetails<bool>(flagKey, defaultValue, ErrorType.FlagNotFound, "Feature flag not found");
        }
        catch (Exception ex)
        {
            return new ResolutionDetails<bool>(flagKey, defaultValue, ErrorType.General, ex.Message);
        }
    }

    /// <inheritdoc/>
    public override async Task<ResolutionDetails<string>> ResolveStringValueAsync(string flagKey, string defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var configKey = this.GetConfigurationKey(flagKey);
            var configValue = await this._configurationClient.GetConfigurationSettingAsync(configKey, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (configValue?.Value?.Value == null)
            {
                return new ResolutionDetails<string>(flagKey, defaultValue, ErrorType.FlagNotFound, "Configuration key not found");
            }

            return new ResolutionDetails<string>(flagKey, configValue.Value.Value);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return new ResolutionDetails<string>(flagKey, defaultValue, ErrorType.FlagNotFound, "Configuration key not found");
        }
        catch (Exception ex)
        {
            return new ResolutionDetails<string>(flagKey, defaultValue, ErrorType.General, ex.Message);
        }
    }

    /// <inheritdoc/>
    public override async Task<ResolutionDetails<int>> ResolveIntegerValueAsync(string flagKey, int defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var configKey = this.GetConfigurationKey(flagKey);
            var configValue = await this._configurationClient.GetConfigurationSettingAsync(configKey, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (configValue?.Value?.Value == null)
            {
                return new ResolutionDetails<int>(flagKey, defaultValue, ErrorType.FlagNotFound, "Configuration key not found");
            }

            if (int.TryParse(configValue.Value.Value, out var intValue))
            {
                return new ResolutionDetails<int>(flagKey, intValue);
            }

            return new ResolutionDetails<int>(flagKey, defaultValue, ErrorType.TypeMismatch, "Value is not a valid integer");
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return new ResolutionDetails<int>(flagKey, defaultValue, ErrorType.FlagNotFound, "Configuration key not found");
        }
        catch (Exception ex)
        {
            return new ResolutionDetails<int>(flagKey, defaultValue, ErrorType.General, ex.Message);
        }
    }

    /// <inheritdoc/>
    public override async Task<ResolutionDetails<double>> ResolveDoubleValueAsync(string flagKey, double defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var configKey = this.GetConfigurationKey(flagKey);
            var configValue = await this._configurationClient.GetConfigurationSettingAsync(configKey, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (configValue?.Value?.Value == null)
            {
                return new ResolutionDetails<double>(flagKey, defaultValue, ErrorType.FlagNotFound, "Configuration key not found");
            }

            if (double.TryParse(configValue.Value.Value, out var doubleValue))
            {
                return new ResolutionDetails<double>(flagKey, doubleValue);
            }

            return new ResolutionDetails<double>(flagKey, defaultValue, ErrorType.TypeMismatch, "Value is not a valid double");
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return new ResolutionDetails<double>(flagKey, defaultValue, ErrorType.FlagNotFound, "Configuration key not found");
        }
        catch (Exception ex)
        {
            return new ResolutionDetails<double>(flagKey, defaultValue, ErrorType.General, ex.Message);
        }
    }

    /// <inheritdoc/>
    public override async Task<ResolutionDetails<Value>> ResolveStructureValueAsync(string flagKey, Value defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var configKey = this.GetConfigurationKey(flagKey);
            var configValue = await this._configurationClient.GetConfigurationSettingAsync(configKey, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (configValue?.Value?.Value == null)
            {
                return new ResolutionDetails<Value>(flagKey, defaultValue, ErrorType.FlagNotFound, "Configuration key not found");
            }

            try
            {
                var jsonElement = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(configValue.Value.Value);
                var value = new Value(jsonElement);
                return new ResolutionDetails<Value>(flagKey, value);
            }
            catch (System.Text.Json.JsonException)
            {
                // If it's not valid JSON, treat it as a string value
                var value = new Value(configValue.Value.Value);
                return new ResolutionDetails<Value>(flagKey, value);
            }
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return new ResolutionDetails<Value>(flagKey, defaultValue, ErrorType.FlagNotFound, "Configuration key not found");
        }
        catch (Exception ex)
        {
            return new ResolutionDetails<Value>(flagKey, defaultValue, ErrorType.General, ex.Message);
        }
    }

    private string GetFeatureFlagKey(string flagKey)
    {
        return this._options.FeatureFlagPrefix + flagKey;
    }

    private string GetConfigurationKey(string flagKey)
    {
        return this._options.ConfigurationPrefix + flagKey;
    }

    private bool EvaluateConditions(FeatureFlag featureFlag, EvaluationContext context)
    {
        if (featureFlag.Conditions?.ClientFilters == null)
            return true;

        foreach (var filter in featureFlag.Conditions.ClientFilters)
        {
            switch (filter.Name?.ToLowerInvariant())
            {
                case "microsoft.percentage":
                case "percentagefilter":
                    if (filter.Parameters?.TryGetValue("Value", out var percentageObj) == true)
                    {
                        if (double.TryParse(percentageObj.ToString(), out var percentage))
                        {
                            // Simple percentage evaluation - in production you'd want a more sophisticated approach
                            var userId = context?.GetValue("userId")?.AsString ?? context?.GetValue("targetingId")?.AsString ?? "anonymous";
                            var hash = Math.Abs(userId.GetHashCode()) % 100;
                            return hash < percentage;
                        }
                    }
                    break;

                case "microsoft.targeting":
                case "targetingfilter":
                    // Basic targeting evaluation - in production you'd integrate with Microsoft.FeatureManagement
                    if (context != null)
                    {
                        var userId = context.GetValue("userId")?.AsString;
                        var groups = context.GetValue("groups")?.AsList;

                        if (filter.Parameters?.TryGetValue("Audience", out var audienceObj) == true && audienceObj is System.Text.Json.JsonElement audienceElement)
                        {
                            // Check if user is in the target users list
                            if (audienceElement.TryGetProperty("Users", out var usersElement) && !string.IsNullOrEmpty(userId))
                            {
                                foreach (var userElement in usersElement.EnumerateArray())
                                {
                                    if (string.Equals(userElement.GetString(), userId, StringComparison.OrdinalIgnoreCase))
                                        return true;
                                }
                            }

                            // Check if user is in any of the target groups
                            if (audienceElement.TryGetProperty("Groups", out var groupsElement) && groups != null)
                            {
                                foreach (var groupElement in groupsElement.EnumerateArray())
                                {
                                    var targetGroup = groupElement.GetString();
                                    if (groups.Any(g => string.Equals(g.AsString, targetGroup, StringComparison.OrdinalIgnoreCase)))
                                        return true;
                                }
                            }
                        }
                    }
                    break;
            }
        }

        return false;
    }
}
