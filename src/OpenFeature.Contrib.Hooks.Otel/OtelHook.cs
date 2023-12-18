using OpenFeature.Model;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System;

namespace OpenFeature.Contrib.Hooks.Otel

{
    /// <summary>
    /// Stub.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [Obsolete("This class is obsolete and will be removed in a future version. Please use TracingHook instead.")]
    public class OtelHook : Hook
    {
        private readonly TracingHook _tracingHook = new TracingHook();

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
            _tracingHook.After(context, details, hints);

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
            _tracingHook.Error(context, error, hints);

            return Task.CompletedTask;
        }

    }
}


