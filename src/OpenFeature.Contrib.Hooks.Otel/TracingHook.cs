using OpenFeature.Model;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using OpenTelemetry.Trace;

namespace OpenFeature.Contrib.Hooks.Otel

{
    /// <summary>
    /// Stub.
    /// </summary>
    public class TracingHook : Hook
    {

        /// <summary>
        ///     After is executed after a feature flag has been evaluated.
        /// </summary>
        /// <param name="context">The hook context</param>
        /// <param name="details">The result of the feature flag evaluation</param>
        /// <param name="hints">Hints for the feature flag evaluation</param>
        /// <returns>An awaitable Task object</returns>
        public override Task After<T>(HookContext<T> context, FlagEvaluationDetails<T> details,
            IReadOnlyDictionary<string, object> hints = null)
        {
            Activity.Current?
                .SetTag("feature_flag.key", details.FlagKey)
                .SetTag("feature_flag.variant", details.Variant)
                .SetTag("feature_flag.provider_name", context.ProviderMetadata.Name)
                .AddEvent(new ActivityEvent("feature_flag", tags: new ActivityTagsCollection
                {
                    ["feature_flag.key"] = details.FlagKey,
                    ["feature_flag.variant"] = details.Variant,
                    ["feature_flag.provider_name"] = context.ProviderMetadata.Name
                }));

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Error is executed when an error during a feature flag evaluation occured.
        /// </summary>
        /// <param name="context">The hook context</param>
        /// <param name="error">The exception thrown by feature flag provider</param>
        /// <param name="hints">Hints for the feature flag evaluation</param>
        /// <returns>An awaitable Task object</returns>
        public override Task Error<T>(HookContext<T> context, System.Exception error,
            IReadOnlyDictionary<string, object> hints = null)
        {
            Activity.Current?.RecordException(error);

            return Task.CompletedTask;
        }

    }
}


