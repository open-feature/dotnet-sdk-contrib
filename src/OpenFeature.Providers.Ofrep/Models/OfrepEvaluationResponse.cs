using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenFeature.Providers.Ofrep.Models;

/// <summary>
/// Represents a raw OFREP evaluation response where the value field may be omitted when deferring to code defaults.
/// </summary>
internal sealed class OfrepEvaluationResponse
{
    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("errorDetails")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("value")]
    public JsonElement Value { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("variant")]
    public string? Variant { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}
