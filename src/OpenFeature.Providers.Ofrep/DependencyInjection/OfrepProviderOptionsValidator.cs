using Microsoft.Extensions.Options;

namespace OpenFeature.Providers.Ofrep.DependencyInjection;

/// <summary>
/// Validator for OfrepProviderOptions to ensure required fields are set during service registration.
/// </summary>
internal class OfrepProviderOptionsValidator : IValidateOptions<OfrepProviderOptions>
{
    public ValidateOptionsResult Validate(string? name, OfrepProviderOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            return ValidateOptionsResult.Fail("Ofrep BaseUrl is required. Set it on OfrepProviderOptions.BaseUrl.");
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
}
