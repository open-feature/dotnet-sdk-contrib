using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Contrib.Providers.GOFeatureFlag.api;
using OpenFeature.Contrib.Providers.GOFeatureFlag.evaluator;
using OpenFeature.Contrib.Providers.GOFeatureFlag.exception;
using OpenFeature.Contrib.Providers.GOFeatureFlag.extensions;
using OpenFeature.Contrib.Providers.GOFeatureFlag.hooks;
using OpenFeature.Contrib.Providers.GOFeatureFlag.model;
using OpenFeature.Contrib.Providers.GOFeatureFlag.ofrep;
using OpenFeature.Contrib.Providers.GOFeatureFlag.service;
using OpenFeature.Model;

[assembly: InternalsVisibleTo("OpenFeature.Providers.GOFeatureFlag.Test")]

namespace OpenFeature.Contrib.Providers.GOFeatureFlag;

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

    private readonly GoFeatureFlagProviderOptions _options;

    /// <summary>
    ///     Metadata associated with this provider.
    /// </summary>
    private readonly Metadata _providerMetadata = new("GO Feature Flag Provider");


    private IImmutableList<Hook> _hooks = ImmutableArray.Create<Hook>();

    /// <summary>
    ///     Constructor of the provider.
    /// </summary>
    /// <param name="options">Options used while creating the provider</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOption">if no options are provided, or we have a wrong configuration.</exception>
    public GoFeatureFlagProvider(GoFeatureFlagProviderOptions options) : this(options, null)
    {
        // we don't do anything here, the internal constructor will do the job.
    }

    /// <summary>
    ///     This constructor is used to create the provider with a custom OFREP provider.
    /// </summary>
    /// <param name="options">Options used while creating the provider</param>
    /// <param name="ofrepProvider">The OFREP provider should be set only for test purposes</param>
    internal GoFeatureFlagProvider(GoFeatureFlagProviderOptions options, IOfrepProvider ofrepProvider = null)
    {
        ValidateInputOptions(options);
#if NETFRAMEWORK
    if (options.EvaluationType == EvaluationType.InProcess)
        {
            throw new InvalidOption(
                "InProcess evaluation is not supported with .NET Framework.");
        }
#endif
        var api = new GoFeatureFlagApi(options);
        var evaluator = this.GetEvaluator(options, api, ofrepProvider);
        this._evaluationService = new EvaluationService(evaluator);
        this._eventPublisher = new EventPublisher(api, options);
        this._options = options;
    }


    /// <summary>
    ///     Return the metadata associated with this provider.
    /// </summary>
    public override Metadata GetMetadata()
    {
        return this._providerMetadata;
    }

    /// <summary>
    ///     GetEvaluator is used to get the evaluator based on the evaluation type specified in the options.
    /// </summary>
    /// <param name="options">Provider options.</param>
    /// <param name="api">API layer to access the relay proxy.</param>
    /// <param name="ofrepProvider">Optional custom OFREP provider (used for test)</param>
    /// <returns></returns>
    private IEvaluator GetEvaluator(GoFeatureFlagProviderOptions options, GoFeatureFlagApi api,
        IOfrepProvider ofrepProvider = null)
    {
        if (options.EvaluationType == EvaluationType.Remote)
        {
            return new RemoteEvaluator(options, ofrepProvider);
        }

        return new InProcessEvaluator(
            api, options, this.EventChannel, this._providerMetadata);
    }

    /// <summary>
    ///     List of hooks to use for this provider
    /// </summary>
    /// <returns></returns>
    public override IImmutableList<Hook> GetProviderHooks()
    {
        return this._hooks.ToImmutableList();
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
        this._hooks = this._hooks.Add(new EnrichEvaluationContextHook(this._options.ExporterMetadata));
        await this._evaluationService.InitializeAsync().ConfigureAwait(false);
        await this._eventPublisher.StartAsync().ConfigureAwait(false);

        // In case of remote evaluation, we don't need to send the data to the collector
        // because the relay-proxy will collect events directly server side.
        if (!this._options.DisableDataCollection && this._options.EvaluationType == EvaluationType.InProcess)
        {
            this._hooks = this._hooks.Add(new DataCollectorHook(this._evaluationService, this._eventPublisher));
        }
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
        return await this._evaluationService.GetEvaluationAsync(flagKey, defaultValue, context)
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
        return await this._evaluationService.GetEvaluationAsync(flagKey, defaultValue, context)
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
        return await this._evaluationService.GetEvaluationAsync(flagKey, defaultValue, context)
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
        return await this._evaluationService.GetEvaluationAsync(flagKey, defaultValue, context)
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
        return await this._evaluationService.GetEvaluationAsync(flagKey, defaultValue, context)
            .ConfigureAwait(false);
    }

    /// <summary>
    ///     Track a user action or application state, usually representing a business objective or outcome. The implementation
    ///     of this method is optional.
    /// </summary>
    /// <param name="trackingEventName">The name associated with this tracking event</param>
    /// <param name="evaluationContext">The evaluation context used in the evaluation of the flag (optional)</param>
    /// <param name="trackingEventDetails">Data pertinent to the tracking event (Optional)</param>
    public override void Track(string trackingEventName, EvaluationContext evaluationContext = default,
        TrackingEventDetails trackingEventDetails = default)
    {
        var trackingEvent =
            new TrackingEvent
            {
                EvaluationContext = evaluationContext?.AsDictionary(),
                UserKey = evaluationContext != null ? evaluationContext.TargetingKey : "undefined-targetingKey",
                ContextKind = evaluationContext.IsAnonymous() ? "anonymousUser" : "user",
                Key = trackingEventName,
                TrackingEventDetails = trackingEventDetails?.AsDictionary() ??
                                       new Dictionary<string, Value>().ToImmutableDictionary(),
                CreationDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        this._eventPublisher.AddEvent(trackingEvent);
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
