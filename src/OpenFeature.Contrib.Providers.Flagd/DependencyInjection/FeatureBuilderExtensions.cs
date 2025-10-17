using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenFeature.Hosting;
using OpenFeature.Contrib.Providers.Flagd;
using OpenFeature.Contrib.Providers.Flagd.DependencyInjection;

namespace OpenFeature.DependencyInjection.Providers.Flagd;

/// <summary>
/// Extension methods for configuring the <see cref="OpenFeatureBuilder"/>.
/// </summary>
public static class FeatureBuilderExtensions
{
    /// <summary>
    /// Adds the <see cref="FlagdProvider"/> to the <see cref="OpenFeatureBuilder"/> with default <see cref="FlagdProviderOptions"/> configuration.
    /// </summary>
    /// <param name="builder">The <see cref="OpenFeatureBuilder"/> instance to configure.</param>
    /// <returns>The <see cref="OpenFeatureBuilder"/> instance for chaining.</returns>
    public static OpenFeatureBuilder AddFlagdProvider(this OpenFeatureBuilder builder)
    {
        builder.Services.AddOptions<FlagdProviderOptions>(FlagdProviderOptions.DefaultName);
        return builder.AddProvider(sp => CreateProvider(sp, null));
    }

    /// <summary>
    /// Adds the <see cref="FlagdProvider"/> to the <see cref="OpenFeatureBuilder"/> with <see cref="FlagdProviderOptions"/> configuration.
    /// </summary>
    /// <param name="builder">The <see cref="OpenFeatureBuilder"/> instance to configure.</param>
    /// <param name="options">Options to configure <see cref="FlagdProvider"/>.</param>
    /// <returns>The <see cref="OpenFeatureBuilder"/> instance for chaining.</returns>
    public static OpenFeatureBuilder AddFlagdProvider(this OpenFeatureBuilder builder, Action<FlagdProviderOptions> options)
    {
        builder.Services.Configure(FlagdProviderOptions.DefaultName, options);
        return builder.AddProvider(sp => CreateProvider(sp, null));
    }

    /// <summary>
    /// Adds the <see cref="FlagdProvider"/> to the <see cref="OpenFeatureBuilder"/> with a specific domain and default <see cref="FlagdProviderOptions"/> configuration.
    /// </summary>
    /// <param name="builder">The <see cref="OpenFeatureBuilder"/> instance to configure.</param>
    /// <param name="domain">The unique domain of the provider.</param>
    /// <returns>The <see cref="OpenFeatureBuilder"/> instance for chaining.</returns>
    public static OpenFeatureBuilder AddFlagdProvider(this OpenFeatureBuilder builder, string domain)
    {
        builder.Services.AddOptions<FlagdProviderOptions>(domain);
        return builder.AddProvider(domain, CreateProvider);
    }

    /// <summary>
    /// Adds the <see cref="FlagdProvider"/> to the <see cref="OpenFeatureBuilder"/> with a specific domain and <see cref="FlagdProviderOptions"/> configuration.
    /// </summary>
    /// <param name="builder">The <see cref="OpenFeatureBuilder"/> instance to configure.</param>
    /// <param name="domain">The unique domain of the provider.</param>
    /// <param name="options">Options to configure <see cref="FlagdProvider"/>.</param>
    /// <returns>The <see cref="OpenFeatureBuilder"/> instance for chaining.</returns>
    public static OpenFeatureBuilder AddFlagdProvider(this OpenFeatureBuilder builder, string domain, Action<FlagdProviderOptions> options)
    {
        builder.Services.Configure(domain, options);
        return builder.AddProvider(domain, CreateProvider);
    }

    private static FlagdProvider CreateProvider(IServiceProvider provider, string domain)
    {
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<FlagdProviderOptions>>();
        var logger = provider.GetService<ILogger<FlagdProvider>>();
        logger ??= NullLogger<FlagdProvider>.Instance;

        var options = string.IsNullOrEmpty(domain)
            ? optionsMonitor.Get(FlagdProviderOptions.DefaultName)
            : optionsMonitor.Get(domain);

        var config = options.ToFlagdConfig();
        config.Logger = logger;

        return new FlagdProvider(config);
    }
}
