using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Hooks.Otel;


/// <summary>
/// Stub.
/// </summary>
[ExcludeFromCodeCoverage]
[Obsolete("This class is obsolete and will be removed in a future version. Please use TracingHook instead.")]
public class OtelHook : Hook
{
    private readonly TracingHook _tracingHook = new TracingHook();

    /// <inheritdoc/>
    public override ValueTask AfterAsync<T>(HookContext<T> context, FlagEvaluationDetails<T> details,
        IReadOnlyDictionary<string, object> hints = null, CancellationToken cancellationToken = default)
    {
        _tracingHook.AfterAsync(context, details, hints);

        return default;
    }

    /// <inheritdoc/>
    public override ValueTask ErrorAsync<T>(HookContext<T> context, Exception error,
        IReadOnlyDictionary<string, object> hints = null, CancellationToken cancellationToken = default)
    {
        _tracingHook.ErrorAsync(context, error, hints);

        return default;
    }

}


