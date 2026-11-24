using System;
using System.Collections.Generic;

namespace OpenFeature.Providers.Flagd.Core.CustomEvaluators;

// only the operator creates instances and only it can register their markers, so evaluation context JSON can't forge one
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

    // the value the operator returns in its place and the key ResolveRule looks it up by;
    // unique per result, so a context string can't pass for it
    internal string Marker { get; } = ResolveRule.Sentinel + Guid.NewGuid().ToString("N");
}
