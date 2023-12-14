using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Hooks.Otel
{
    public class MetricsHook : Hook
    {
        private const string FEATURE_FLAG = "feature_flag";
        private const string EXCEPTION_ATTR = "exception";

        private const string ACTIVE_COUNT_NAME = FEATURE_FLAG + ".evaluation_active_count";
        private const string REQUESTS_TOTAL_NAME = FEATURE_FLAG + ".evaluation_requests_total";
        private const string SUCCESS_TOTAL_NAME = FEATURE_FLAG + ".evaluation_success_total";
        private const string ERROR_TOTAL_NAME = FEATURE_FLAG + ".evaluation_error_total";

        private const string KEY_ATTR = FEATURE_FLAG + ".key";
        private const string PROVIDER_NAME_ATTR = FEATURE_FLAG + ".provider_name";
        private const string VARIANT_ATTR = FEATURE_FLAG + ".variant";
        private const string REASON_ATTR = FEATURE_FLAG + ".reason";

        // support a functional means of adding custom attributes to metrics, as in the JS implementation, which allows consumers to provide a function that takes the flag metadata 
        // and returns OTel attributes
        public MetricsHook()
        {
        }


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

