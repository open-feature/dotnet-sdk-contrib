using System;
using System.Runtime.InteropServices;
using JsonLogic.Net;
using Newtonsoft.Json.Linq;
using OpenFeature.Error;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess.CustomEvaluators
{
    internal class StringEvaluator
    {
        internal StringEvaluator()
        {
        }

        internal object StartsWith(IProcessJsonLogic p, JToken[] args, object data)
        {
            if (!isValid(p, args, data, out string operandA, out string operandB))
            {
                return false;
            };
            return Convert.ToString(operandA).StartsWith(Convert.ToString(operandB));
        }

        internal object EndsWith(IProcessJsonLogic p, JToken[] args, object data)
        {
            if (!isValid(p, args, data, out string operandA, out string operandB))
            {
                return false;
            };
            return operandA.EndsWith(operandB);
        }

        private bool isValid(IProcessJsonLogic p, JToken[] args, object data, out string operandA, out string operandB)
        {
            // check if we have at least 2 arguments
            operandA = null;
            operandB = null;

            if (args.Length < 2)
            {
                return false;
            }
            operandA = p.Apply(args[0], data) as string;
            operandB = p.Apply(args[1], data) as string;

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