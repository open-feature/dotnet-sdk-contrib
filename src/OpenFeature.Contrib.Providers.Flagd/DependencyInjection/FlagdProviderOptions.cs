using OpenFeature.Contrib.Providers.Flagd;

namespace OpenFeature.DependencyInjection.Providers.Flagd;

/// <summary>
/// Configuration options for the Flagd provider.
/// </summary>
public record FlagdProviderOptions
{
    /// <summary>
    /// The host for the provider to connect to. Defaults to "localhost".
    /// </summary>
    public string Host { get; init; } = "localhost";

    /// <summary>
    /// The Port property of the config. Defaults to 8013.
    /// </summary>
    public int Port { get; init; } = 8013;

    /// <summary>
    /// Use TLS for communication between the provider and the host. Defaults to false.
    /// </summary>
    public bool UseTls { get; init; } = false;

    /// <summary>
    /// 
    /// </summary>
    public bool CacheEnabled { get; init; } = false;

    /// <summary>
    /// The maximum size of the cache. Defaults to 10.
    /// </summary>
    public int MaxCacheSize { get; init; } = 10;

    /// <summary>
    /// Path to the certificate file. Defaults to empty string.
    /// </summary>
    public string CertificatePath { get; init; } = string.Empty;

    /// <summary>
    /// Path to the socket. Defaults to empty string.
    /// </summary>
    public string SocketPath { get; init; } = string.Empty;

    /// <summary>
    /// Maximum number of times the connection to the event stream should be re-attempted. Defaults to 3.
    /// </summary>
    public int MaxEventStreamRetries { get; init; } = 3;

    /// <summary>
    /// Which type of resolver to use. Defaults to <see cref="ResolverType.RPC"/>.
    /// </summary>
    public ResolverType ResolverType { get; init; } = ResolverType.RPC;

    /// <summary>
    /// Source selector for the in-process provider. Defaults to empty string.
    /// </summary>
    public string SourceSelector { get; init; } = string.Empty;
}
