#nullable enable
using System.Collections.Generic;

namespace OpenFeature.Providers.Flagd.Core.CustomEvaluators;

// internal and only created by the operator; parsed JSON never holds one, so evaluation context data can't forge one
internal sealed class ResolveResult
{
    internal ResolveResult(string? variant, string? reason, Dictionary<string, object>? metadata)
    {
        Variant = variant;
        Reason = reason;
        Metadata = metadata;
    }

    internal string? Variant { get; }
    internal string? Reason { get; }
    internal Dictionary<string, object>? Metadata { get; }
}
