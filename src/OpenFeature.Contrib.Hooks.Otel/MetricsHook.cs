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

        private readonly UpDownCounter<double> _evaluationActiveUpDownCounter;
        private readonly Counter<double> _evaluationRequestCounter;
        private readonly Counter<double> _evaluationSuccessCounter;
        private readonly Counter<double> _evaluationErrorCounter;

        public MetricsHook()
        {
            var meter = new Meter(InstrumentationName, InstrumentationVersion);

            _evaluationActiveUpDownCounter = meter.CreateUpDownCounter<double>(MetricsConstants.ACTIVE_COUNT_NAME, MetricsConstants.UNIT, MetricsConstants.ACTIVE_DESCRIPTION);
            _evaluationRequestCounter = meter.CreateCounter<double>(MetricsConstants.REQUESTS_TOTAL_NAME, MetricsConstants.UNIT, MetricsConstants.REQUESTS_DESCRIPTION);
            _evaluationSuccessCounter = meter.CreateCounter<double>(MetricsConstants.SUCCESS_TOTAL_NAME, MetricsConstants.UNIT, MetricsConstants.SUCCESS_DESCRIPTION);
            _evaluationErrorCounter = meter.CreateCounter<double>(MetricsConstants.ERROR_TOTAL_NAME, MetricsConstants.UNIT, MetricsConstants.ERROR_DESCRIPTION);
        }

        public override Task<EvaluationContext> Before<T>(HookContext<T> context, IReadOnlyDictionary<string, object> hints = null)
        {
            var tagList = new TagList
            {
                { MetricsConstants.KEY_ATTR, context.FlagKey },
                { MetricsConstants.PROVIDER_NAME_ATTR, context.ProviderMetadata.Name }
            };

            _evaluationActiveUpDownCounter.Add(1, tagList);
            _evaluationRequestCounter.Add(1, tagList);

            return base.Before(context, hints);
        }

        public override Task After<T>(HookContext<T> context, FlagEvaluationDetails<T> details, IReadOnlyDictionary<string, object> hints = null)
        {
            var tagList = new TagList
            {
                { MetricsConstants.KEY_ATTR, context.FlagKey },
                { MetricsConstants.PROVIDER_NAME_ATTR, context.ProviderMetadata.Name },
                { MetricsConstants.VARIANT_ATTR, details.Variant ?? details.Value?.ToString() },
                { MetricsConstants.REASON_ATTR, details.Reason ?? "UNKNOWN" }
            };

            _evaluationSuccessCounter.Add(1, tagList);

            return base.After(context, details, hints);
        }

        public override Task Error<T>(HookContext<T> context, Exception error, IReadOnlyDictionary<string, object> hints = null)
        {
            var tagList = new TagList
            {
                { MetricsConstants.KEY_ATTR, context.FlagKey },
                { MetricsConstants.PROVIDER_NAME_ATTR, context.ProviderMetadata.Name },
                { MetricsConstants.EXCEPTION_ATTR, error?.Message ?? "Unknown error" }
            };

            _evaluationErrorCounter.Add(1, tagList);

            return base.Error(context, error, hints);
        }

        public override Task Finally<T>(HookContext<T> context, IReadOnlyDictionary<string, object> hints = null)
        {
            var tagList = new TagList
            {
                { MetricsConstants.KEY_ATTR, context.FlagKey },
                { MetricsConstants.PROVIDER_NAME_ATTR, context.ProviderMetadata.Name }
            };

            _evaluationActiveUpDownCounter.Add(-1, tagList);

            return base.Finally(context, hints);
        }
    }
}
