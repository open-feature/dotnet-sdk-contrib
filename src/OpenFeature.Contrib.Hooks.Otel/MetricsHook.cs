using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Hooks.Otel
{
    public class MetricsHook : Hook
    {
        // support a functional means of adding custom attributes to metrics, as in the JS implementation, which allows consumers to provide a function that takes the flag metadata 
        // and returns OTel attributes


        public override Task<EvaluationContext> Before<T>(HookContext<T> context, IReadOnlyDictionary<string, object> hints = null)
        {
            // evaluationActiveUpDownCounter
            // evaluationRequestCounter
            return base.Before(context, hints);
        }

        public override Task After<T>(HookContext<T> context, FlagEvaluationDetails<T> details, IReadOnlyDictionary<string, object> hints = null)
        {
            // evaluationSuccessCounter
            return base.After(context, details, hints);
        }

        public override Task Error<T>(HookContext<T> context, Exception error, IReadOnlyDictionary<string, object> hints = null)
        {
            // evaluationErrorCounter
            return base.Error(context, error, hints);
        }

        public override Task Finally<T>(HookContext<T> context, IReadOnlyDictionary<string, object> hints = null)
        {
            // evaluationActiveUpDownCounter
            return base.Finally(context, hints);
        }
    }
}

