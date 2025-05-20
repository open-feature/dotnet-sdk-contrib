using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenFeature.Contrib.Providers.Flagd;

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
        => builder.AddProvider(sp =>
        {
            return CreateProvider(sp, null, new FlagdProviderOptions());
        });

    /// <summary>
    /// Adds the <see cref="FlagdProvider"/> to the <see cref="OpenFeatureBuilder"/> with <see cref="FlagdProviderOptions"/> configuration.
    /// </summary>
    /// <param name="builder">The <see cref="OpenFeatureBuilder"/> instance to configure.</param>
    /// <param name="options">Options to configure <see cref="FlagdProvider"/>.</param>
    /// <returns>The <see cref="OpenFeatureBuilder"/> instance for chaining.</returns>
    public static OpenFeatureBuilder AddFlagdProvider(this OpenFeatureBuilder builder, FlagdProviderOptions options)
        => builder.AddProvider(sp =>
        {
            return CreateProvider(sp, null, options);
        });

    /// <summary>
    /// Adds the <see cref="FlagdProvider"/> to the <see cref="OpenFeatureBuilder"/> with a specific domain and default <see cref="FlagdProviderOptions"/> configuration.
    /// </summary>
    /// <param name="builder">The <see cref="OpenFeatureBuilder"/> instance to configure.</param>
    /// <param name="domain">The unique domain of the provider.</param>
    /// <returns>The <see cref="OpenFeatureBuilder"/> instance for chaining.</returns>
    public static OpenFeatureBuilder AddFlagdProvider(this OpenFeatureBuilder builder, string domain)
        => builder.AddProvider(domain, (sp, domain) =>
        {
            return CreateProvider(sp, domain, new FlagdProviderOptions());
        });

    /// <summary>
    /// Adds the <see cref="FlagdProvider"/> to the <see cref="OpenFeatureBuilder"/> with a specific domain and <see cref="FlagdProviderOptions"/> configuration.
    /// </summary>
    /// <param name="builder">The <see cref="OpenFeatureBuilder"/> instance to configure.</param>
    /// <param name="domain">The unique domain of the provider.</param>
    /// <param name="options">Options to configure <see cref="FlagdProvider"/>.</param>
    /// <returns>The <see cref="OpenFeatureBuilder"/> instance for chaining.</returns>
    public static OpenFeatureBuilder AddFlagdProvider(this OpenFeatureBuilder builder, string domain, FlagdProviderOptions options)
        => builder.AddProvider(domain, (sp, domain) =>
        {
            return CreateProvider(sp, domain, options);
        });

    private static FlagdProvider CreateProvider(IServiceProvider provider, string _, FlagdProviderOptions options)
    {
        var logger = provider.GetService<ILogger<FlagdProvider>>();
        logger ??= NullLogger<FlagdProvider>.Instance;

        var config = FlagdConfig.Builder()
            .WithHost(options.Host)
            .WithPort(options.Port)
            .WithTls(options.UseTls)
            .WithCache(options.CacheEnabled)
            .WithMaxCacheSize(options.MaxCacheSize)
            .WithCertificatePath(options.CertificatePath)
            .WithSocketPath(options.SocketPath)
            .WithMaxEventStreamRetries(options.MaxEventStreamRetries)
            .WithResolverType(options.ResolverType)
            .WithSourceSelector(options.SourceSelector)
            .WithLogger(logger)
            .Build();

        return new FlagdProvider(config);
    }
}
