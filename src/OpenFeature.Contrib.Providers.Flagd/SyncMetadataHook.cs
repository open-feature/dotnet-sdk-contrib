using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.Flagd;

/// <summary>
/// Provides a synchronous hook that supplies metadata using a specified evaluation context callback.
/// </summary>
internal class SyncMetadataHook : Hook
{
    private readonly Func<EvaluationContext> _evaluationContextSupplier;

    /// <summary>
    /// Initializes a new instance of the SyncMetadataHook class using the specified evaluation context callback.
    /// </summary>
    /// <param name="evaluationContextCallback">A delegate that provides an EvaluationContext instance used by the hook. Cannot be null.</param>
    public SyncMetadataHook(Func<EvaluationContext> evaluationContextCallback)
    {
        this._evaluationContextSupplier = evaluationContextCallback ?? throw new ArgumentNullException(nameof(evaluationContextCallback));
    }

    /// <inheritdoc />
    public override ValueTask<EvaluationContext> BeforeAsync<T>(HookContext<T> context, IReadOnlyDictionary<string, object> hints = null, CancellationToken cancellationToken = default)
    {
        return new ValueTask<EvaluationContext>(this._evaluationContextSupplier());
    }
}
