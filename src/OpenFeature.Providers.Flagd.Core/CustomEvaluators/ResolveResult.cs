using System.Collections.Generic;

namespace OpenFeature.Providers.Flagd.Core.CustomEvaluators;

// only the operator creates instances, so evaluation context JSON can't forge one
internal sealed class ResolveResult
{
    internal ResolveResult(string variant, string reason, Dictionary<string, object> metadata)
    {
        Variant = variant;
        Reason = reason;
        Metadata = metadata;
    }

    internal string Variant { get; }
    internal string Reason { get; }
    internal Dictionary<string, object> Metadata { get; }
}
