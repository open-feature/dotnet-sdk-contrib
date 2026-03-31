using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Logic;
using Murmur;

namespace OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess.CustomEvaluators;

/// <inheritdoc/>
internal sealed class FractionalEvaluator : IRule
{
    private const int MaxWeight = int.MaxValue; // 2,147,483,647

    class FractionalEvaluationDistribution
    {
        public JsonNode variant;
        public int weight;
    }

    /// <inheritdoc/>
    public JsonNode Apply(JsonNode args, EvaluationContext context)
    {
        if (args.AsArray().Count == 0)
        {
            return null;
        }

        var flagdProperties = new FlagdProperties(context);

        var bucketStartIndex = 0;

        var arg0 = JsonLogic.Apply(args[0], context);

        string propertyValue;
        if (arg0 != null && arg0.GetValueKind() == JsonValueKind.String)
        {
            propertyValue = arg0.ToString();
            bucketStartIndex = 1;
        }
        else
        {
            propertyValue = flagdProperties.FlagKey + flagdProperties.TargetingKey;
        }

        var distributions = new List<FractionalEvaluationDistribution>();
        long totalWeight = 0;

        for (var i = bucketStartIndex; i < args.AsArray().Count; i++)
        {
            var bucketNode = JsonLogic.Apply(args[i], context);

            if (bucketNode == null || bucketNode.GetValueKind() != JsonValueKind.Array)
            {
                continue;
            }

            var bucketArr = bucketNode.AsArray();

            if (!bucketArr.Any())
            {
                continue;
            }

            // resolve variant: accept string, number, bool, or null
            var variantNode = bucketArr.ElementAt(0);
            JsonNode variant;
            if (variantNode == null)
            {
                variant = null;
            }
            else
            {
                var kind = variantNode.GetValueKind();
                if (kind == JsonValueKind.String
                    || kind == JsonValueKind.Number
                    || kind == JsonValueKind.True
                    || kind == JsonValueKind.False)
                {
                    variant = variantNode;
                }
                else
                {
                    // unsupported variant type (object, array); skip
                    continue;
                }
            }

            var weight = 1;

            if (bucketArr.Count >= 2)
            {
                var weightNode = bucketArr.ElementAt(1);
                if (weightNode != null && weightNode.GetValueKind() == JsonValueKind.Number)
                {
                    var weightDouble = weightNode.GetValue<double>();

                    // weights must be integers within valid range
                    if (weightDouble != Math.Floor(weightDouble) || weightDouble > MaxWeight)
                    {
                        return null;
                    }

                    // negative weights can be the result of rollout calculations, so we clamp to 0 rather than returning an error
                    weight = (int)Math.Max(0, weightDouble);
                }
            }

            distributions.Add(new FractionalEvaluationDistribution
            {
                variant = variant,
                weight = weight
            });

            totalWeight += weight;
        }

        // total weight must not exceed MaxInt32
        if (totalWeight > MaxWeight || totalWeight == 0)
        {
            return null;
        }

        var valueToDistribute = propertyValue;
        var murmur32 = MurmurHash.Create32();
        var bytes = Encoding.ASCII.GetBytes(valueToDistribute);
        var hashBytes = murmur32.ComputeHash(bytes);

        // treat hash as unsigned 32-bit
        var hashUint = BitConverter.ToUInt32(hashBytes, 0);

        // high-precision bucketing: map hash to [0, totalWeight)
        // (hashUint * totalWeight) >> 32
        var bucket = ((ulong)hashUint * (ulong)totalWeight) >> 32;

        ulong rangeEnd = 0;

        foreach (var dist in distributions)
        {
            rangeEnd += (ulong)dist.weight;
            if (bucket < rangeEnd)
            {
                return dist.variant;
            }
        }

        return null;
    }
}
