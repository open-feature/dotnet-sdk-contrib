using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Logic;
using Murmur;

namespace OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess.CustomEvaluators
{
    /// <inheritdoc/>
    public class FractionalEvaluator : IRule
    {
        internal FractionalEvaluator()
        {
        }

        class FractionalEvaluationDistribution
        {
            public string variant;
            public int weight;
        }

        /// <inheritdoc/>
        public JsonNode Apply(JsonNode args, EvaluationContext context)
        {
            // check if we have at least two arguments:
            // 1. the property value
            // 2. the array containing the buckets

            if (args.AsArray().Count == 0)
            {
                return null;
            }

            var flagdProperties = new FlagdProperties(context);

            var bucketStartIndex = 0;

            var arg0 = JsonLogic.Apply(args[0], context);

            string propertyValue;
            if (arg0.GetValueKind() == JsonValueKind.String)
            {
                propertyValue = arg0.ToString();
                bucketStartIndex = 1;
            }
            else
            {
                propertyValue = flagdProperties.FlagKey + flagdProperties.TargetingKey;
            }

            var distributions = new List<FractionalEvaluationDistribution>();
            var distributionSum = 0;

            for (var i = bucketStartIndex; i < args.AsArray().Count; i++)
            {
                var bucket = JsonLogic.Apply(args[i], context);

                if (!(bucket.GetValueKind() == JsonValueKind.Array))
                {
                    continue;
                }

                var bucketArr = bucket.AsArray();

                if (!bucketArr.Any())
                {
                    continue;
                }

                var weight = 1;

                if (bucketArr.Count >= 2 && bucketArr.ElementAt(1).GetValueKind() == JsonValueKind.Number)
                {
                    weight = bucketArr.ElementAt(1).GetValue<int>();
                }

                distributions.Add(new FractionalEvaluationDistribution
                {
                    variant = bucketArr.ElementAt(0).ToString(),
                    weight = weight
                });

                distributionSum += weight;
            }

            var valueToDistribute = propertyValue;
            var murmur32 = MurmurHash.Create32();
            var bytes = Encoding.ASCII.GetBytes(valueToDistribute);
            var hashBytes = murmur32.ComputeHash(bytes);
            var hash = BitConverter.ToInt32(hashBytes, 0);

            var bucketValue = (int)(Math.Abs((float)hash) / Int32.MaxValue * 100);

            var rangeEnd = 0.0;

            foreach (var dist in distributions)
            {
                rangeEnd += 100 * (dist.weight / (float)distributionSum);
                if (bucketValue < rangeEnd)
                {
                    return dist.variant;
                }
            }

            return "";
        }
    }
}