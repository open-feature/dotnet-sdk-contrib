using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Hooks.Otel
{
    /// <summary>
    /// Represents a hook for capturing metrics related to flag evaluations.
    /// The meter name is "OpenFeature.Contrib.Hooks.Otel".
    /// </summary>
    public class MetricsHook : Hook
    {
        private static readonly AssemblyName AssemblyName = typeof(MetricsHook).Assembly.GetName();
        private static readonly string InstrumentationName = AssemblyName.Name;
        private static readonly string InstrumentationVersion = AssemblyName.Version?.ToString();

        private readonly UpDownCounter<long> _evaluationActiveUpDownCounter;
        private readonly Counter<long> _evaluationRequestCounter;
        private readonly Counter<long> _evaluationSuccessCounter;
        private readonly Counter<long> _evaluationErrorCounter;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsHook"/> class.
        /// </summary>
        public MetricsHook()
        {
            var meter = new Meter(InstrumentationName, InstrumentationVersion);

            _evaluationActiveUpDownCounter = meter.CreateUpDownCounter<long>(MetricsConstants.ActiveCountName, description: MetricsConstants.ActiveDescription);
            _evaluationRequestCounter = meter.CreateCounter<long>(MetricsConstants.RequestsTotalName, "{request}", MetricsConstants.RequestsDescription);
            _evaluationSuccessCounter = meter.CreateCounter<long>(MetricsConstants.SuccessTotalName, "{impression}", MetricsConstants.SuccessDescription);
            _evaluationErrorCounter = meter.CreateCounter<long>(MetricsConstants.ErrorTotalName, description: MetricsConstants.ErrorDescription);
        }

        /// <inheritdoc/>
        public override ValueTask<EvaluationContext> BeforeAsync<T>(HookContext<T> context, IReadOnlyDictionary<string, object> hints = null, CancellationToken cancellationToken = default)
        {
            var tagList = new TagList
            {
                { MetricsConstants.KeyAttr, context.FlagKey },
                { MetricsConstants.ProviderNameAttr, context.ProviderMetadata.Name }
            };

            _evaluationActiveUpDownCounter.Add(1, tagList);
            _evaluationRequestCounter.Add(1, tagList);

            return base.BeforeAsync(context, hints);
        }


        /// <inheritdoc/>
        public override ValueTask AfterAsync<T>(HookContext<T> context, FlagEvaluationDetails<T> details, IReadOnlyDictionary<string, object> hints = null, CancellationToken cancellationToken = default)
        {
            var tagList = new TagList
            {
                { MetricsConstants.KeyAttr, context.FlagKey },
                { MetricsConstants.ProviderNameAttr, context.ProviderMetadata.Name },
                { MetricsConstants.VariantAttr, details.Variant ?? details.Value?.ToString() },
                { MetricsConstants.ReasonAttr, details.Reason ?? "UNKNOWN" }
            };

            _evaluationSuccessCounter.Add(1, tagList);

            return base.AfterAsync(context, details, hints);
        }

        /// <inheritdoc/>
        public override ValueTask ErrorAsync<T>(HookContext<T> context, Exception error, IReadOnlyDictionary<string, object> hints = null, CancellationToken cancellationToken = default)
        {
            var tagList = new TagList
            {
                { MetricsConstants.KeyAttr, context.FlagKey },
                { MetricsConstants.ProviderNameAttr, context.ProviderMetadata.Name },
                { MetricsConstants.ExceptionAttr, error?.Message ?? "Unknown error" }
            };

            _evaluationErrorCounter.Add(1, tagList);

            return base.ErrorAsync(context, error, hints);
        }

        /// <inheritdoc/>
        public override ValueTask FinallyAsync<T>(HookContext<T> context, IReadOnlyDictionary<string, object> hints = null, CancellationToken cancellationToken = default)
        {
            var tagList = new TagList
            {
                { MetricsConstants.KeyAttr, context.FlagKey },
                { MetricsConstants.ProviderNameAttr, context.ProviderMetadata.Name }
            };

            _evaluationActiveUpDownCounter.Add(-1, tagList);

            return base.FinallyAsync(context, hints);
        }
    }
}
