using System;
using System.Text.Json.Nodes;
using Json.Logic;

namespace OpenFeature.Providers.Flagd.Resolver.InProcess.CustomEvaluators;

/// <summary>
/// Catch-all rule for unresolved $ref references.
/// If a $ref was not replaced during the evaluator transform step, this rule throws so the caller gets a PARSE_ERROR.
/// This behavior is consistent with other implementations (though the mechanism differs).
/// </summary>
internal sealed class UnresolvedRefRule : IRule
{
    public JsonNode Apply(JsonNode args, EvaluationContext context)
    {
        var evaluatorName = args?.ToString() ?? "unknown";
        throw new InvalidOperationException(
            $"Unresolved $ref: evaluator '{evaluatorName}' was not found.");
    }
}
