using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Model;
using OpenFeature.Providers.GOFeatureFlag.Models;

namespace OpenFeature.Providers.GOFeatureFlag.Hooks;

/// <summary>
///     Enrich the evaluation context with additional information
/// </summary>
public class EnrichEvaluationContextHook : Hook
{
    private readonly Structure _metadata;

    /// <summary>
    ///     Constructor of the Hook
    /// </summary>
    /// <param name="metadata">metadata to use in order to enrich the evaluation context</param>
    public EnrichEvaluationContextHook(ExporterMetadata metadata)
    {
        if (metadata == null)
        {
            this._metadata = Structure.Empty;
            return;
        }

        this._metadata = metadata.AsStructure();
    }

    /// <summary>
    ///     Enrich the evaluation context with additional information before the evaluation of the flag
    /// </summary>
    /// <param name="context"></param>
    /// <param name="hints"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public override ValueTask<EvaluationContext> BeforeAsync<T>(HookContext<T> context,
        IReadOnlyDictionary<string, object> hints = null, CancellationToken cancellationToken = default)
    {
        var builder = EvaluationContext.Builder();
        if (this._metadata != null && this._metadata.Count != 0)
        {
            builder.Set("gofeatureflag", this._metadata);
        }

        builder.Merge(context.EvaluationContext);
        return new ValueTask<EvaluationContext>(builder.Build());
    }
}
