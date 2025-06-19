using System;
using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Contrib.Providers.GOFeatureFlag.v2.api;
using OpenFeature.Contrib.Providers.GOFeatureFlag.v2.evaluator;
using OpenFeature.Contrib.Providers.GOFeatureFlag.v2.exception;
using OpenFeature.Contrib.Providers.GOFeatureFlag.v2.service;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.v2;

/// <summary>
///     GoFeatureFlagProvider is the OpenFeature provider for GO Feature Flag.
/// </summary>
public class GoFeatureFlagProvider : FeatureProvider
{
    /// <summary>
    ///     EvaluationService is the service used to evaluate feature flags.
    /// </summary>
    private readonly EvaluationService _evaluationService;

    /// <summary>
    ///     EventPublisher is used to collect events and publish them in batch before they are published.
    /// </summary>
    private readonly EventPublisher _eventPublisher;

    /// <summary>
    ///     Metadata associated with this provider.
    /// </summary>
    private readonly Metadata _providerMetadata = new("GO Feature Flag Provider");

    /// <summary>
    ///     Constructor of the provider.
    /// </summary>
    /// <param name="options">Options used while creating the provider</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOption">if no options are provided, or we have a wrong configuration.</exception>
    public GoFeatureFlagProvider(GoFeatureFlagProviderOptions options)
    {
        ValidateInputOptions(options);
        var api = new GoFeatureFlagApi(options);
        var evaluator = new InProcessEvaluator(
            api, options, this.EventChannel, this._providerMetadata);
        this._evaluationService = new EvaluationService(evaluator);
        this._eventPublisher = new EventPublisher(api, options);
    }

    /// <summary>
    ///     Return the metadata associated with this provider.
    /// </summary>
    public override Metadata GetMetadata()
    {
        return this._providerMetadata;
    }

    /// <summary>
    ///     InitializeAsync is called to initialize the provider.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="cancellationToken"></param>
    public override async Task InitializeAsync(
        EvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        await this._evaluationService.InitializeAsync().ConfigureAwait(false);
        await this._eventPublisher.StartAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     ShutdownAsync is called to shut down the provider and release resources.
    /// </summary>
    /// <param name="cancellationToken"></param>
    public override async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        await this._evaluationService.DisposeAsync().ConfigureAwait(false);
        await this._eventPublisher.StopAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Resolves a boolean feature flag
    /// </summary>
    /// <param name="flagKey">Feature flag key</param>
    /// <param name="defaultValue">Default value</param>
    /// <param name="context">EvaluationContext</param>
    /// <param name="cancellationToken">The CancellationToken.</param>
    /// <returns>ResolutionDetails</returns>
    public override async Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(string flagKey, bool defaultValue,
        EvaluationContext context = null,
        CancellationToken cancellationToken = new())
    {
        return await this._evaluationService.GetEvaluation(flagKey, defaultValue, context)
            .ConfigureAwait(false);
    }

    /// <summary>
    ///     Resolves a string feature flag
    /// </summary>
    /// <param name="flagKey">Feature flag key</param>
    /// <param name="defaultValue">Default value</param>
    /// <param name="context">EvaluationContext</param>
    /// <param name="cancellationToken">The CancellationToken.</param>
    /// <returns>ResolutionDetails</returns>
    public override async Task<ResolutionDetails<string>> ResolveStringValueAsync(string flagKey, string defaultValue,
        EvaluationContext context = null,
        CancellationToken cancellationToken = new())
    {
        return await this._evaluationService.GetEvaluation(flagKey, defaultValue, context)
            .ConfigureAwait(false);
    }

    /// <summary>
    ///     Resolves an int feature flag
    /// </summary>
    /// <param name="flagKey">Feature flag key</param>
    /// <param name="defaultValue">Default value</param>
    /// <param name="context">EvaluationContext</param>
    /// <param name="cancellationToken">The CancellationToken.</param>
    /// <returns>ResolutionDetails</returns>
    public override async Task<ResolutionDetails<int>> ResolveIntegerValueAsync(string flagKey, int defaultValue,
        EvaluationContext context = null,
        CancellationToken cancellationToken = new())
    {
        return await this._evaluationService.GetEvaluation(flagKey, defaultValue, context)
            .ConfigureAwait(false);
    }


    /// <summary>
    ///     Resolves a double feature flag
    /// </summary>
    /// <param name="flagKey">Feature flag key</param>
    /// <param name="defaultValue">Default value</param>
    /// <param name="context">EvaluationContext</param>
    /// <param name="cancellationToken">The CancellationToken.</param>
    /// <returns>ResolutionDetails</returns>
    public override async Task<ResolutionDetails<double>> ResolveDoubleValueAsync(string flagKey, double defaultValue,
        EvaluationContext context = null,
        CancellationToken cancellationToken = new())
    {
        return await this._evaluationService.GetEvaluation(flagKey, defaultValue, context)
            .ConfigureAwait(false);
    }

    /// <summary>
    ///     Resolves an object feature flag
    /// </summary>
    /// <param name="flagKey">Feature flag key</param>
    /// <param name="defaultValue">Default value</param>
    /// <param name="context">EvaluationContext</param>
    /// <param name="cancellationToken">The CancellationToken.</param>
    /// <returns>ResolutionDetails</returns>
    public override async Task<ResolutionDetails<Value>> ResolveStructureValueAsync(string flagKey, Value defaultValue,
        EvaluationContext context = null,
        CancellationToken cancellationToken = new())
    {
        return await this._evaluationService.GetEvaluation(flagKey, defaultValue, context)
            .ConfigureAwait(false);
    }

    public override void Track(string trackingEventName, EvaluationContext? evaluationContext = default,
        TrackingEventDetails? trackingEventDetails = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     validateInputOptions is validating the different options provided when creating the provider.
    /// </summary>
    /// <param name="options">Options used while creating the provider</param>
    /// <exception cref="InvalidOption">if no options are provided, or we have a wrong configuration.</exception>
    private static void ValidateInputOptions(GoFeatureFlagProviderOptions options)
    {
        if (options is null)
        {
            throw new InvalidOption("No options provided");
        }

        if (string.IsNullOrEmpty(options.Endpoint))
        {
            throw new InvalidOption("endpoint is a mandatory field when initializing the provider");
        }

        if (options.FlagChangePollingIntervalMs <= TimeSpan.Zero)
        {
            throw new InvalidOption("FlagChangePollingIntervalMs must be greater than zero");
        }

        if (options.Timeout <= TimeSpan.Zero)
        {
            throw new InvalidOption("Timeout must be greater than zero");
        }

        if (options.FlushIntervalMs <= TimeSpan.Zero)
        {
            throw new InvalidOption("Timeout must be greater than zero");
        }

        if (options.MaxPendingEvents <= 0)
        {
            throw new InvalidOption("MaxPendingEvents must be greater than zero");
        }
    }
}
