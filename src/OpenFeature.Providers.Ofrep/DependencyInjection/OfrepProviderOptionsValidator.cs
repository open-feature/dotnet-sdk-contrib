using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using OpenFeature.Providers.Ofrep.Configuration;

namespace OpenFeature.Providers.Ofrep.DependencyInjection;

/// <summary>
/// Validator for OfrepProviderOptions to ensure required fields are set during service registration.
/// </summary>
internal class OfrepProviderOptionsValidator : IValidateOptions<OfrepProviderOptions>
{
    private readonly IConfiguration? _configuration;

    /// <summary>
    /// Creates a new instance of <see cref="OfrepProviderOptionsValidator"/>.
    /// </summary>
    /// <param name="configuration">Optional configuration for fallback values.</param>
    public OfrepProviderOptionsValidator(IConfiguration? configuration = null)
    {
        this._configuration = configuration;
    }

    public ValidateOptionsResult Validate(string? name, OfrepProviderOptions options)
    {
        // If BaseUrl is not set, check if configuration/environment variable is available as fallback
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            var configEndpoint = this.GetConfigurationValue(OfrepOptions.EnvVarEndpoint);
            if (string.IsNullOrWhiteSpace(configEndpoint))
            {
                return ValidateOptionsResult.Fail(
                    $"Ofrep BaseUrl is required. Set it on OfrepProviderOptions.BaseUrl, via IConfiguration key '{OfrepOptions.EnvVarEndpoint}', or the {OfrepOptions.EnvVarEndpoint} environment variable.");
            }

            // Validate the configuration value
            if (!Uri.TryCreate(configEndpoint, UriKind.Absolute, out var configUri))
            {
                return ValidateOptionsResult.Fail(
                    $"Configuration key '{OfrepOptions.EnvVarEndpoint}' must be a valid absolute URI.");
            }

            if (configUri.Scheme != Uri.UriSchemeHttp && configUri.Scheme != Uri.UriSchemeHttps)
            {
                return ValidateOptionsResult.Fail(
                    $"Configuration key '{OfrepOptions.EnvVarEndpoint}' must use HTTP or HTTPS scheme.");
            }

            // Configuration value is valid, allow fallback
            return ValidateOptionsResult.Success;
        }

        // Validate that it's a valid absolute URI
        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri))
        {
            return ValidateOptionsResult.Fail("Ofrep BaseUrl must be a valid absolute URI.");
        }

        // Validate that it uses HTTP or HTTPS scheme (required for OFREP)
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return ValidateOptionsResult.Fail("Ofrep BaseUrl must use HTTP or HTTPS scheme.");
        }

        return ValidateOptionsResult.Success;
    }

    /// <summary>
    /// Gets a configuration value by key, falling back to environment variable if IConfiguration is not available.
    /// </summary>
    private string? GetConfigurationValue(string key)
    {
        // Try IConfiguration first (which can include environment variables if AddEnvironmentVariables() was called)
        var value = this._configuration?[key];
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        // Fall back to direct environment variable access
        return Environment.GetEnvironmentVariable(key);
    }
}
