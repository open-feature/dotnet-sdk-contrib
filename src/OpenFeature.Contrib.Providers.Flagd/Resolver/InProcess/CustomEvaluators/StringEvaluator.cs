using System;
using JsonLogic.Net;
using Newtonsoft.Json.Linq;

namespace OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess.CustomEvaluators
{
    internal class StringEvaluator
    {
        internal object StartsWith(IProcessJsonLogic p, JToken[] args, object data)
        {
            // check if we have at least 2 arguments
            if (args.Length < 2)
            {
                return false;
            }
            return p.Apply(args[0], data).ToString().StartsWith(p.Apply(args[1], data).ToString());
        }

        internal object EndsWith(IProcessJsonLogic p, JToken[] args, object data)
        {
            // check if we have at least 2 arguments
            if (args.Length < 2)
            {
                return false;
            }
            return p.Apply(args[0], data).ToString().EndsWith(p.Apply(args[1], data).ToString());
        }
    }
}