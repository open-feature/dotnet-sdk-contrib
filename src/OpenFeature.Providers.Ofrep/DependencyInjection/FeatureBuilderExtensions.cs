using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenFeature.DependencyInjection;
using OpenFeature.Providers.Ofrep.Configuration;
#if NETFRAMEWORK
using System.Net.Http;
#endif
using Microsoft.Extensions.Logging;
using OpenFeature.Providers.Ofrep.Client;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace OpenFeature.Providers.Ofrep.DependencyInjection;

/// <summary>
/// Extension methods for configuring the OpenFeatureBuilder with Ofrep provider.
/// </summary>
public static class FeatureBuilderExtensions
{
    /// <summary>
    /// Adds the OfrepProvider with configured options.
    /// </summary>
    public static OpenFeatureBuilder AddOfrepProvider(this OpenFeatureBuilder builder, Action<OfrepProviderOptions> configure)
    {
        builder.Services.Configure(OfrepProviderOptions.DefaultName, configure);
        builder.Services.TryAddSingleton<IValidateOptions<OfrepProviderOptions>, OfrepProviderOptionsValidator>();
        return builder.AddProvider(sp => CreateProvider(sp, null));
    }

    /// <summary>
    /// Adds the OfrepProvider for a named domain with configured options.
    /// </summary>
    public static OpenFeatureBuilder AddOfrepProvider(this OpenFeatureBuilder builder, string domain, Action<OfrepProviderOptions> configure)
    {
        builder.Services.Configure(domain, configure);
        builder.Services.TryAddSingleton<IValidateOptions<OfrepProviderOptions>, OfrepProviderOptionsValidator>();
        return builder.AddProvider(domain, CreateProvider);
    }

    private static OfrepProvider CreateProvider(IServiceProvider sp, string? domain)
    {
        var monitor = sp.GetRequiredService<IOptionsMonitor<OfrepProviderOptions>>();
        var opts = string.IsNullOrWhiteSpace(domain) ? monitor.Get(OfrepProviderOptions.DefaultName) : monitor.Get(domain);

        // Options validation is handled by OfrepProviderOptionsValidator during service registration
        var ofrepOptions = new OfrepOptions(opts.BaseUrl)
        {
            Timeout = opts.Timeout,
            Headers = opts.Headers
        };

        // Resolve or create HttpClient if caller wants to manage it
        HttpClient? httpClient = null;

        // Prefer IHttpClientFactory if available
        var factory = sp.GetService<IHttpClientFactory>();
        if (factory != null)
        {
            httpClient = string.IsNullOrWhiteSpace(opts.HttpClientName) ? factory.CreateClient() : factory.CreateClient(opts.HttpClientName!);
        }

        // If no factory/client, let OfrepClient create its own HttpClient
        if (httpClient == null)
        {
            return new OfrepProvider(ofrepOptions); // internal client management
        }

        // Allow user to configure the HttpClient
        opts.ConfigureHttpClient?.Invoke(sp, httpClient);

        // Ensure base address/timeout/headers align with options unless already set by user
        if (httpClient.BaseAddress == null)
        {
            httpClient.BaseAddress = new Uri(ofrepOptions.BaseUrl);
        }
        httpClient.Timeout = ofrepOptions.Timeout;
        foreach (var header in ofrepOptions.Headers)
        {
            if (!httpClient.DefaultRequestHeaders.Contains(header.Key))
            {
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        // Build OfrepClient using provided HttpClient and wire into OfrepProvider
        var loggerFactory = sp.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger<OfrepClient>();
        var ofrepClient = new OfrepClient(httpClient, logger);
        return new OfrepProvider(ofrepClient);
    }
}
