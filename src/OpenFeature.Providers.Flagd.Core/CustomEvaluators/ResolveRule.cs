using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Logic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace OpenFeature.Providers.Flagd.Core.CustomEvaluators;

internal sealed class ResolveRule : IRule
{
    private static readonly HashSet<string> ReservedMetadataKeys = new HashSet<string> { "flagSetId", "version" };

    private readonly ILogger _logger;

    internal ResolveRule(ILogger logger = null)
    {
        _logger = logger ?? NullLogger.Instance;
    }

    public JsonNode Apply(JsonNode args, EvaluationContext context)
    {
        // a nested rule that throws fails the evaluation (PARSE_ERROR), like it would outside resolve,
        // so only the shape of the evaluated arguments is validated below
        var evaluated = JsonLogic.Apply(args, context);

        ResolveResult result;
        try
        {
            result = Parse(evaluated);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Invalid resolve configuration: {Message}", ex.Message);
            return null;
        }

        // the node wraps the result itself, which JSON input can't produce
        return JsonValue.Create(result);
    }

    internal static bool TryGetResult(JsonNode node, out ResolveResult result)
    {
        result = null;
        return node is JsonValue value && value.TryGetValue(out result);
    }

    private ResolveResult Parse(JsonNode args)
    {
        var arr = args?.GetValueKind() == JsonValueKind.Array ? args.AsArray() : null;
        if (arr == null || arr.Count < 1 || arr.Count > 3)
        {
            throw new ArgumentException("arguments must be an array of 1 to 3 items");
        }

        var variantNode = arr[0];
        string variant;
        var vKind = variantNode?.GetValueKind() ?? JsonValueKind.Null;
        switch (vKind)
        {
            case JsonValueKind.String:
                variant = variantNode.GetValue<string>();
                break;
            case JsonValueKind.True:
                variant = "true";
                break;
            case JsonValueKind.False:
                variant = "false";
                break;
            case JsonValueKind.Number:
                variant = variantNode.ToString();
                break;
            case JsonValueKind.Null:
                // same as a plain null targeting result
                variant = null;
                break;
            default:
                throw new ArgumentException($"variant must be a string, boolean, number, or null; got {vKind}");
        }

        string reason = null;
        if (arr.Count >= 2)
        {
            var reasonNode = arr[1];
            var rKind = reasonNode?.GetValueKind() ?? JsonValueKind.Null;
            if (rKind == JsonValueKind.String)
            {
                reason = reasonNode.GetValue<string>();
            }
            else if (rKind != JsonValueKind.Null)
            {
                throw new ArgumentException($"reason must be a string or null; got {rKind}");
            }
        }

        Dictionary<string, object> metadata = null;
        if (arr.Count >= 3)
        {
            metadata = ParseMetadata(arr[2]);
        }

        return new ResolveResult(variant, reason, metadata);
    }

    private Dictionary<string, object> ParseMetadata(JsonNode pairsNode)
    {
        var pKind = pairsNode?.GetValueKind() ?? JsonValueKind.Null;
        if (pKind == JsonValueKind.Null)
        {
            return null;
        }
        if (pKind != JsonValueKind.Array)
        {
            throw new ArgumentException("metadata must be an array of [key, value] pairs");
        }

        var result = new Dictionary<string, object>();
        foreach (var pair in pairsNode.AsArray())
        {
            if (pair == null || pair.GetValueKind() != JsonValueKind.Array)
            {
                throw new ArgumentException("metadata pair must be an array");
            }
            var pairArr = pair.AsArray();
            if (pairArr.Count != 2)
            {
                throw new ArgumentException("metadata pair must have exactly 2 elements");
            }

            var keyNode = pairArr[0];
            if (keyNode == null || keyNode.GetValueKind() != JsonValueKind.String)
            {
                throw new ArgumentException("metadata key must be a string");
            }
            var key = keyNode.GetValue<string>();

            var valNode = pairArr[1];
            var vaKind = valNode?.GetValueKind() ?? JsonValueKind.Null;
            object value;
            switch (vaKind)
            {
                case JsonValueKind.String:
                    value = valNode.GetValue<string>();
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    value = valNode.GetValue<bool>();
                    break;
                case JsonValueKind.Number:
                    // computed numbers may be backed by decimal, which GetValue<double> rejects
                    var d = double.Parse(valNode.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture);
                    if (double.IsNaN(d) || double.IsInfinity(d))
                    {
                        throw new ArgumentException($"metadata value for key '{key}' must be finite");
                    }
                    value = d;
                    break;
                default:
                    throw new ArgumentException($"metadata value for key '{key}' must be a string, finite number, or boolean; got {vaKind}");
            }

            if (ReservedMetadataKeys.Contains(key))
            {
                _logger.LogDebug("Ignoring reserved metadata key '{Key}' in resolve", key);
                continue;
            }

            result[key] = value;
        }

        return result.Count > 0 ? result : null;
    }
}
