using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Logic;

namespace OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess.CustomEvaluators;

internal sealed class StartsWithRule : IRule
{
    public JsonNode Apply(JsonNode args, Json.Logic.EvaluationContext context)
    {
        if (!StringRule.isValid(args, context, out string operandA, out string operandB))
        {
            return false;
        }
        return Convert.ToString(operandA).StartsWith(Convert.ToString(operandB));
    }
}

internal sealed class EndsWithRule : IRule
{
    public JsonNode Apply(JsonNode args, Json.Logic.EvaluationContext context)
    {
        if (!StringRule.isValid(args, context, out string operandA, out string operandB))
        {
            return false;
        }
        return operandA.EndsWith(operandB);
    }
}

internal static class StringRule
{
    internal static bool isValid(JsonNode args, Json.Logic.EvaluationContext context, out string argA, out string argB)
    {
        argA = null;
        argB = null;

        // check if we have at least 2 arguments
        if (args.AsArray().Count < 2)
        {
            return false;
        }

        var nodeA = JsonLogic.Apply(args[0], context);
        var nodeB = JsonLogic.Apply(args[1], context);

        // return false immediately if both operands are not strings
        if (nodeA?.GetValueKind() != JsonValueKind.String || nodeB?.GetValueKind() != JsonValueKind.String)
        {
            return false;
        }

        argA = nodeA.ToString();
        argB = nodeB.ToString();
        return true;
    }
}
