using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Model;
using OpenFeature.Providers.GOFeatureFlag.extensions;
using OpenFeature.Providers.GOFeatureFlag.model;
using OpenFeature.Providers.GOFeatureFlag.service;

namespace OpenFeature.Providers.GOFeatureFlag.hooks;

/// <summary>
///     DataCollectorHook is a hook that collects data during the evaluation of feature flags.
/// </summary>
public class DataCollectorHook : Hook
{
    private readonly EvaluationService _evaluationService;
    private readonly EventPublisher _eventPublisher;

    /// <summary>
    ///     DataCollectorHook is a hook that collects data during the evaluation of feature flags.
    /// </summary>
    /// <param name="evaluationService">service to evaluate the flag</param>
    /// <param name="eventPublisher"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public DataCollectorHook(EvaluationService evaluationService, EventPublisher eventPublisher)
    {
        this._evaluationService = evaluationService ?? throw new ArgumentNullException(nameof(evaluationService));
        this._eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
    }

    /// <summary>Called immediately after successful flag evaluation.</summary>
    /// <param name="context">Provides context of innovation</param>
    /// <param name="details">Flag evaluation information</param>
    /// <param name="hints">Caller provided data</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" />.</param>
    /// <typeparam name="T">Flag value type (bool|number|string|object)</typeparam>
    public override ValueTask AfterAsync<T>(
        HookContext<T> context,
        FlagEvaluationDetails<T> details,
        IReadOnlyDictionary<string, object> hints = null,
        CancellationToken cancellationToken = default)
    {
        if (!this._evaluationService.IsFlagTrackable(context.FlagKey))
        {
            // If the flag is not trackable, we do not need to collect data.
            return new ValueTask();
        }

        var eventToPublish = new FeatureEvent
        {
            Key = context.FlagKey,
            ContextKind = context.EvaluationContext.IsAnonymous() ? "anonymousUser" : "user",
            DefaultValue = false,
            Variation = details.Variant,
            Value = details.Value,
            UserKey = context.EvaluationContext.TargetingKey,
            CreationDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        this._eventPublisher.AddEvent(eventToPublish);
        return new ValueTask();
    }

    /// <summary>
    ///     Called immediately after an unsuccessful flag evaluation.
    /// </summary>
    /// <param name="context">Provides context of innovation</param>
    /// <param name="error">Exception representing what went wrong</param>
    /// <param name="hints">Caller provided data</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" />.</param>
    /// <typeparam name="T">Flag value type (bool|number|string|object)</typeparam>
    public override ValueTask ErrorAsync<T>(
        HookContext<T> context,
        Exception error,
        IReadOnlyDictionary<string, object> hints = null,
        CancellationToken cancellationToken = default)
    {
        if (!this._evaluationService.IsFlagTrackable(context.FlagKey))
        {
            // If the flag is not trackable, we do not need to collect data.
            return new ValueTask();
        }

        var eventToPublish = new FeatureEvent
        {
            Key = context.FlagKey,
            ContextKind = context.EvaluationContext.IsAnonymous() ? "anonymousUser" : "user",
            DefaultValue = true,
            Variation = "SdkDefault",
            Value = context.DefaultValue,
            UserKey = context.EvaluationContext.TargetingKey,
            CreationDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        this._eventPublisher.AddEvent(eventToPublish);
        return new ValueTask();
    }
}
