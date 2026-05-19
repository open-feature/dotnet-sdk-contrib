using System;
using System.Collections.Generic;
using OpenFeature.Model;
using Unleash;

namespace OpenFeature.Providers.Unleash;

/// <summary>
/// Transforms an OpenFeature EvaluationContext into an Unleash UnleashContext.
/// </summary>
internal static class ContextTransformer
{
    private const string UserIdKey = "userId";
    private const string SessionIdKey = "sessionId";
    private const string RemoteAddressKey = "remoteAddress";
    private const string EnvironmentKey = "environment";
    private const string AppNameKey = "appName";
    private const string CurrentTimeKey = "currentTime";

    /// <summary>
    /// Transforms an OpenFeature EvaluationContext into an Unleash UnleashContext.
    /// </summary>
    /// <param name="context">The OpenFeature evaluation context, may be null.</param>
    /// <returns>A new UnleashContext populated from the evaluation context.</returns>
    public static UnleashContext Transform(EvaluationContext context)
    {
        if (context == null)
        {
            return new UnleashContext();
        }

        string userId = null;
        string sessionId = null;
        string remoteAddress = null;
        string environment = null;
        string appName = null;
        DateTimeOffset? currentTime = null;
        var properties = new Dictionary<string, string>();

        // Map targeting key to userId if present
        if (!string.IsNullOrEmpty(context.TargetingKey))
        {
            userId = context.TargetingKey;
        }

        foreach (var kvp in context)
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
