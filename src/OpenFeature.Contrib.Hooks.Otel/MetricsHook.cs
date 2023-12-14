using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Threading.Tasks;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Hooks.Otel
{
    public class MetricsHook : Hook
    {
        private static readonly AssemblyName AssemblyName = typeof(MetricsHook).Assembly.GetName();
        private static readonly string InstrumentationName = AssemblyName.Name;
        private static readonly string InstrumentationVersion = AssemblyName.Version.ToString();

        private const string EXCEPTION_ATTR = "exception";

        private const string ACTIVE_COUNT_NAME = "feature_flag.evaluation_active_count";
        private const string REQUESTS_TOTAL_NAME = "feature_flag.evaluation_requests_total";
        private const string SUCCESS_TOTAL_NAME = "feature_flag.evaluation_success_total";
        private const string ERROR_TOTAL_NAME = "feature_flag.evaluation_error_total";

        private const string KEY_ATTR = "feature_flag.key";
        private const string PROVIDER_NAME_ATTR = "feature_flag.provider_name";
        private const string VARIANT_ATTR = "feature_flag.variant";
        private const string REASON_ATTR = "feature_flag.reason";

        private readonly UpDownCounter<double> _evaluationActiveUpDownCounter;
        private readonly Counter<double> _evaluationRequestCounter;
        private readonly Counter<double> _evaluationSuccessCounter;
        private readonly Counter<double> _evaluationErrorCounter;

        public MetricsHook()
        {
            var meter = new Meter(InstrumentationName, InstrumentationVersion);

            _evaluationActiveUpDownCounter = meter.CreateUpDownCounter<double>(ACTIVE_COUNT_NAME);
            _evaluationRequestCounter = meter.CreateCounter<double>(REQUESTS_TOTAL_NAME);
            _evaluationSuccessCounter = meter.CreateCounter<double>(SUCCESS_TOTAL_NAME);
            _evaluationErrorCounter = meter.CreateCounter<double>(ERROR_TOTAL_NAME);
        }


        public override Task<EvaluationContext> Before<T>(HookContext<T> context, IReadOnlyDictionary<string, object> hints = null)
        {
            var tagList = new TagList
            {
                { KEY_ATTR, context.FlagKey },
                { PROVIDER_NAME_ATTR, context.ProviderMetadata.Name }
            };

            _evaluationActiveUpDownCounter.Add(1, tagList);
            _evaluationRequestCounter.Add(1, tagList);

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

