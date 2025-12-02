using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.Flagd;

internal class SyncMetadataHook : Hook
{
    private readonly Func<EvaluationContext> _evaluationContextSupplier;

    public SyncMetadataHook(Func<EvaluationContext> evaluationContextSupplier)
    {
        this._evaluationContextSupplier = evaluationContextSupplier;
    }

    public override ValueTask<EvaluationContext> BeforeAsync<T>(HookContext<T> context, IReadOnlyDictionary<string, object> hints = null, CancellationToken cancellationToken = default)
    {
        return new ValueTask<EvaluationContext>(this._evaluationContextSupplier());
    }
}
