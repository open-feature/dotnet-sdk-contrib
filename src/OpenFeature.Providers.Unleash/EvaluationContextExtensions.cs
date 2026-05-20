using System;
using System.Collections.Generic;
using OpenFeature.Model;
using Unleash;

namespace OpenFeature.Providers.Unleash;

/// <summary>
/// Extension methods for transforming an OpenFeature EvaluationContext into an Unleash UnleashContext.
/// </summary>
internal static class EvaluationContextExtensions
{
    private const string UserIdKey = "userId";
    private const string SessionIdKey = "sessionId";
    private const string RemoteAddressKey = "remoteAddress";
    private const string EnvironmentKey = "environment";
    private const string AppNameKey = "appName";
    private const string CurrentTimeKey = "currentTime";

    /// <summary>
    /// Gets the appName value from the evaluation context, or null if not present.
    /// </summary>
    /// <param name="context">The evaluation context.</param>
    /// <returns>The appName value, or null.</returns>
    public static string? GetAppName(this EvaluationContext? context)
    {
        if (context == null)
        {
            return null;
        }

        return context.TryGetValue(AppNameKey, out var value) ? value?.AsString : null;
    }

    /// <summary>
    /// Transforms an OpenFeature EvaluationContext into an Unleash UnleashContext.
    /// If a baseline context is provided, it is merged with the per-call context
    /// (per-call values take precedence).
    /// </summary>
    /// <param name="context">The per-call evaluation context, may be null.</param>
    /// <param name="baselineContext">The baseline evaluation context from initialization, may be null.</param>
    /// <returns>A new UnleashContext populated from the merged contexts.</returns>
    public static UnleashContext ToUnleashContext(this EvaluationContext? context, EvaluationContext? baselineContext = null)
    {
        EvaluationContext? merged;
        if (baselineContext != null && context != null)
        {
            merged = EvaluationContext.Builder()
                .Merge(baselineContext)
                .Merge(context)
                .Build();
        }
        else
        {
            merged = context ?? baselineContext;
        }

        if (merged == null)
        {
            return new UnleashContext();
        }

        string? userId = null;
        string? sessionId = null;
        string? remoteAddress = null;
        string? environment = null;
        string? appName = null;
        DateTimeOffset? currentTime = null;
        var properties = new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(merged.TargetingKey))
        {
            userId = merged.TargetingKey;
        }

        foreach (var kvp in merged)
        {
            var key = kvp.Key;
            var value = kvp.Value;

            if (value == null)
            {
                continue;
            }

            var stringValue = value.AsString;

            switch (key)
            {
                case UserIdKey:
                    userId = stringValue;
                    break;
                case SessionIdKey:
                    sessionId = stringValue;
                    break;
                case RemoteAddressKey:
                    remoteAddress = stringValue;
                    break;
                case EnvironmentKey:
                    environment = stringValue;
                    break;
                case AppNameKey:
                    appName = stringValue;
                    break;
                case CurrentTimeKey:
                    if (DateTimeOffset.TryParse(stringValue, out var parsed))
                    {
                        currentTime = parsed;
                    }
                    break;
                default:
                    if (stringValue != null)
                    {
                        properties[key] = stringValue;
                    }
                    break;
            }
        }

        return new UnleashContext(appName, environment, userId, sessionId, remoteAddress, currentTime, properties);
    }
}
