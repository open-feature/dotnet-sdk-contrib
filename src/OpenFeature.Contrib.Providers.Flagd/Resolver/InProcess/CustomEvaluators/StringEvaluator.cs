using System;
using System.Text.Json.Nodes;
using Json.Logic;

namespace OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess.CustomEvaluators
{
    internal class StartsWith : IRule
    {
        internal StartsWith()
        {
        }

        public JsonNode Apply(JsonNode args, Json.Logic.EvaluationContext context)
        {
            if (!StringEvaluator.isValid(args, context, out string operandA, out string operandB))
            {
                return false;
            }
            return Convert.ToString(operandA).StartsWith(Convert.ToString(operandB));
        }
    }

    internal class EndsWith : IRule
    {

        internal EndsWith()
        {
        }

        public JsonNode Apply(JsonNode args, Json.Logic.EvaluationContext context)
        {
            if (!StringEvaluator.isValid(args, context, out string operandA, out string operandB))
            {
                return false;
            }
            return operandA.EndsWith(operandB);
        }
    }

    internal static class StringEvaluator
    {
        internal static bool isValid(JsonNode args, Json.Logic.EvaluationContext context, out string operandA, out string operandB)
        {
            // check if we have at least 2 arguments
            operandA = null;
            operandB = null;

            if (args.AsArray().Count < 2)
            {
                return false;
            }
            operandA = JsonLogic.Apply(args[0], context).ToString();
            operandB = JsonLogic.Apply(args[1], context).ToString();

            if (!(operandA is string) || !(operandB is string))
            {
                // return false immediately if both operands are not strings.
                return false;
            }

            Convert.ToString(operandA);
            Convert.ToString(operandB);

            return true;
        }
    }
}