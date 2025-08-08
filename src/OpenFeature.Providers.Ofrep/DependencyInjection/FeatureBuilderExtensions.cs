using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenFeature.DependencyInjection;
using OpenFeature.Providers.Ofrep.Configuration;

namespace OpenFeature.Providers.Ofrep.DependencyInjection;

/// <summary>
/// Extension methods for configuring the OpenFeatureBuilder with Ofrep provider.
/// </summary>
public static class FeatureBuilderExtensions
{
    /// <summary>
    /// Adds the OfrepProvider with default options registration.
    /// </summary>
    public static OpenFeatureBuilder AddOfrepProvider(this OpenFeatureBuilder builder)
    {
        builder.Services.AddOptions<OfrepProviderOptions>(OfrepProviderOptions.DefaultName);
        return builder.AddProvider(sp => CreateProvider(sp, null));
    }

    /// <summary>
    /// Adds the OfrepProvider with configured options.
    /// </summary>
    public static OpenFeatureBuilder AddOfrepProvider(this OpenFeatureBuilder builder, Action<OfrepProviderOptions> configure)
    {
        builder.Services.Configure(OfrepProviderOptions.DefaultName, configure);
        return builder.AddProvider(sp => CreateProvider(sp, null));
    }

    /// <summary>
    /// Adds the OfrepProvider for a named domain with default options.
    /// </summary>
    public static OpenFeatureBuilder AddOfrepProvider(this OpenFeatureBuilder builder, string domain)
    {
        builder.Services.AddOptions<OfrepProviderOptions>(domain);
        return builder.AddProvider(domain, CreateProvider);
    }

    /// <summary>
    /// Adds the OfrepProvider for a named domain with configured options.
    /// </summary>
    public static OpenFeatureBuilder AddOfrepProvider(this OpenFeatureBuilder builder, string domain, Action<OfrepProviderOptions> configure)
    {
        builder.Services.Configure(domain, configure);
        return builder.AddProvider(domain, CreateProvider);
    }

    private static OfrepProvider CreateProvider(IServiceProvider sp, string? domain)
    {
        var monitor = sp.GetRequiredService<IOptionsMonitor<OfrepProviderOptions>>();
        var opts = string.IsNullOrEmpty(domain) ? monitor.Get(OfrepProviderOptions.DefaultName) : monitor.Get(domain);

        if (string.IsNullOrWhiteSpace(opts.BaseUrl))
        {
            throw new ArgumentException("Ofrep BaseUrl is required. Set it on OfrepProviderOptions.BaseUrl.");
        }

        var ofrepOptions = new OfrepOptions(opts.BaseUrl)
        {
            Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds),
            Headers = opts.Headers
        };

        return new OfrepProvider(ofrepOptions);
    }
}
