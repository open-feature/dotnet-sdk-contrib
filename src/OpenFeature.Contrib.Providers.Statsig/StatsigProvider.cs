using System;
using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Constant;
using OpenFeature.Model;
using Statsig;
using Statsig.Server;
using Statsig.Server.Evaluation;

namespace OpenFeature.Contrib.Providers.Statsig;

/// <summary>
/// An OpenFeature <see cref="FeatureProvider"/> which enables the use of the Statsig Server-Side SDK for .NET
/// with OpenFeature.
/// </summary>
/// <example>
///     var provider = new StatsigProvider("my-sdk-key"), new StatsigProviderOptions(){LocalMode = false});
///
///     OpenFeature.Api.Instance.SetProvider(provider);
///
///     var client = OpenFeature.Api.Instance.GetClient();
/// </example>
public sealed class StatsigProvider : FeatureProvider
{
    private readonly Metadata _providerMetadata = new Metadata("Statsig provider");
    private readonly string _sdkKey = "secret-"; //Dummy sdk key that works with local mode
    internal readonly ServerDriver ServerDriver;

    /// <summary>
    /// Creates new instance of <see cref="StatsigProvider"/>
    /// </summary>
    /// <param name="sdkKey">SDK Key to access Statsig.</param>
    /// <param name="statsigServerOptions">The StatsigServerOptions to configure the provider.</param>
    public StatsigProvider(string sdkKey = null, StatsigServerOptions statsigServerOptions = null)
    {
        if (sdkKey != null)
        {
            _sdkKey = sdkKey;
        }
        ServerDriver = new ServerDriver(_sdkKey, statsigServerOptions);
    }

    /// <inheritdoc/>
    public override Metadata GetMetadata() => _providerMetadata;

    /// <inheritdoc/>
    public override Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(string flagKey, bool defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
    {
        var result = ServerDriver.GetFeatureGate(context.AsStatsigUser(), flagKey);
        var gateFound = false;
        var responseType = ErrorType.None;

        switch (result.Reason)
        {
            case EvaluationReason.Network:
            case EvaluationReason.LocalOverride:
            case EvaluationReason.Bootstrap:
            case EvaluationReason.DataAdapter:
                gateFound = true;
                break;
            case EvaluationReason.Unrecognized:
                responseType = ErrorType.FlagNotFound;
                break;
            case EvaluationReason.Uninitialized:
                responseType = ErrorType.ProviderNotReady;
                break;
            case EvaluationReason.Unsupported:
                responseType = ErrorType.InvalidContext;
                break;
            case EvaluationReason.Error:
                responseType = ErrorType.General;
                break;
            case null:
                break;
        }
        return Task.FromResult(new ResolutionDetails<bool>(flagKey, gateFound ? result.Value : defaultValue, responseType));
    }

    /// <inheritdoc/>
    public override Task<ResolutionDetails<double>> ResolveDoubleValueAsync(string flagKey, double defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override Task<ResolutionDetails<int>> ResolveIntegerValueAsync(string flagKey, int defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override Task<ResolutionDetails<string>> ResolveStringValueAsync(string flagKey, string defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override Task<ResolutionDetails<Value>> ResolveStructureValueAsync(string flagKey, Value defaultValue, EvaluationContext context = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override async Task InitializeAsync(EvaluationContext context, CancellationToken cancellationToken = default)
    {
        await ServerDriver.Initialize().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        return ServerDriver.Shutdown();
    }
}
