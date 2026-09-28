using System;
using System.Collections.Generic;
using System.Formats.Cbor;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Logic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Murmur;

namespace OpenFeature.Providers.Flagd.Resolver.InProcess.CustomEvaluators;

/// <inheritdoc/>
internal sealed class FractionalEvaluator : IRule
{
    private const int MaxWeight = int.MaxValue; // 2,147,483,647
    private readonly ILogger _logger;

    class FractionalEvaluationDistribution
    {
        public JsonNode variant;
        public int weight;
    }

    internal FractionalEvaluator(ILogger logger = null)
    {
        _logger = logger ?? NullLogger.Instance;
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

        JsonNode propertyValue;
        var arg0Kind = arg0?.GetValueKind();

        if (arg0 == null || arg0Kind == JsonValueKind.Null)
        {
            _logger.LogDebug("Invalid arguments for fractional targeting: first argument is null");
            return null;
        }
        else if (arg0Kind == JsonValueKind.String
            || arg0Kind == JsonValueKind.True
            || arg0Kind == JsonValueKind.False
            || arg0Kind == JsonValueKind.Number
            || arg0Kind == JsonValueKind.Object)
        {
            propertyValue = arg0;
            bucketStartIndex = 1;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(flagdProperties.TargetingKey))
            {
                _logger.LogDebug("Missing fallback targeting key");
                return null;
            }
            propertyValue = new JsonArray(flagdProperties.FlagKey, flagdProperties.TargetingKey);
            bucketStartIndex = 0;
        }

        var distributions = new List<FractionalEvaluationDistribution>();
        long totalWeight = 0;

        for (var i = bucketStartIndex; i < args.AsArray().Count; i++)
        {
            var bucketNode = JsonLogic.Apply(args[i], context);

            if (bucketNode == null || bucketNode.GetValueKind() != JsonValueKind.Array)
            {
                return null;
            }

            var bucketArr = bucketNode.AsArray();

            if (!bucketArr.Any())
            {
                return null;
            }

            // resolve variant: accept string, number, bool, or null
            var variantNode = JsonLogic.Apply(bucketArr.ElementAt(0), context);
            JsonNode variant;
            if (variantNode == null || variantNode.GetValueKind() == JsonValueKind.Null)
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
                    // unsupported variant type (object, array)
                    return null;
                }
            }
            var weight = 1;

            if (bucketArr.Count >= 2)
            {
                var weightNode = JsonLogic.Apply(bucketArr.ElementAt(1), context);
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
                else
                {
                    // weight is not a number
                    return null;
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

        var murmur32 = MurmurHash.Create32();
        byte[] bytes;
        try
        {
            bytes = EncodeNodeToCbor(propertyValue);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to encode property value to CBOR for fractional evaluation");
            return null;
        }

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

    private static byte[] EncodeNodeToCbor(JsonNode node)
    {
        var writer = new CborWriter(CborConformanceMode.Canonical);
        WriteNode(writer, node);
        return writer.Encode();
    }

    private static void WriteNode(CborWriter writer, JsonNode node)
    {
        if (node == null || node.GetValueKind() == JsonValueKind.Null)
        {
            writer.WriteNull();
            return;
        }

        switch (node.GetValueKind())
        {
            case JsonValueKind.True:
                writer.WriteBoolean(true);
                break;
            case JsonValueKind.False:
                writer.WriteBoolean(false);
                break;
            case JsonValueKind.String:
                writer.WriteTextString(node.GetValue<string>());
                break;
            case JsonValueKind.Number:
                if (node.AsValue().TryGetValue<long>(out var longVal))
                {
                    writer.WriteInt64(longVal);
                }
                else if (node.AsValue().TryGetValue<ulong>(out var ulongVal))
                {
                    writer.WriteUInt64(ulongVal);
                }
                else
                {
                    var doubleVal = node.GetValue<double>();
                    if (!double.IsInfinity(doubleVal)
                        && doubleVal == Math.Floor(doubleVal)
                        && doubleVal >= long.MinValue
                        && doubleVal <= long.MaxValue)
                    {
                        writer.WriteInt64((long)doubleVal);
                    }
                    else
                    {
                        writer.WriteDouble(doubleVal);
                    }
                }
                break;
            case JsonValueKind.Array:
                var arr = node.AsArray();
                writer.WriteStartArray(arr.Count);
                foreach (var item in arr)
                {
                    WriteNode(writer, item);
                }
                writer.WriteEndArray();
                break;
            case JsonValueKind.Object:
                var obj = node.AsObject();
                writer.WriteStartMap(obj.Count);
                foreach (var kvp in obj)
                {
                    writer.WriteTextString(kvp.Key);
                    WriteNode(writer, kvp.Value);
                }
                writer.WriteEndMap();
                break;
            default:
                throw new ArgumentException($"Unsupported JsonValueKind: {node.GetValueKind()}");
        }
    }
}
