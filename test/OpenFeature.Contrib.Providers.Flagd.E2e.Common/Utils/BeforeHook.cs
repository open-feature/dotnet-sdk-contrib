using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.Common.Utils;

#nullable enable
public class BeforeHook : Hook
{
    private readonly EvaluationContext context;

    public BeforeHook(EvaluationContext context)
    {
        this.context = context;
    }

    public override ValueTask<EvaluationContext> BeforeAsync<T>(HookContext<T> context, IReadOnlyDictionary<string, object>? hints = null, CancellationToken cancellationToken = default)
    {
        return new ValueTask<EvaluationContext>(this.context);
    }
}
