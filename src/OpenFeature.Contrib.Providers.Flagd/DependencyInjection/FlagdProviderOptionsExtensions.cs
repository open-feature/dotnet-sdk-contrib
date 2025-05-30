using System;
using OpenFeature.DependencyInjection.Providers.Flagd;

namespace OpenFeature.Contrib.Providers.Flagd.DependencyInjection;

internal static class FlagdProviderOptionsExtensions
{
    public static FlagdConfig ToFlagdConfig(this FlagdProviderOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options), "FlagdProviderOptions cannot be null.");
        }

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
            .Build();

        return config;
    }
}
