using System.Text.Json.Serialization;

namespace OpenFeature.Providers.Ofrep.Models;

/// <summary>
/// Represents a response from the OFREP API for a single flag evaluation.
/// </summary>
/// <typeparam name="T">The type of the flag value.</typeparam>
public class OfrepResponse<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OfrepResponse{T}"/> class with the specified value.
    /// </summary>
    /// <param name="key">The key of the flag that was evaluated.</param>
    /// <param name="value">The value to be wrapped in the OFREP response.</param>
    public OfrepResponse(string key, T value)
    {
        this.Value = value;
        this.Key = key;
    }

    /// <summary>
    /// The key of the flag that was evaluated.
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; }

    /// <summary>
    /// An error code indicating the reason for evaluation failure, if any.
    /// </summary>
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    /// <summary>
    /// A detailed error message accompanying the error code, if any.
    /// </summary>
    [JsonPropertyName("errorDetails")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The evaluated value of the flag.
    /// </summary>
    [JsonPropertyName("value")]
    public T Value { get; set; }

    /// <summary>
    /// The reason for the evaluation result (e.g., STATIC, TARGETING_MATCH).
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    /// <summary>
    /// The specific variant of the flag that was evaluated.
    /// </summary>
    [JsonPropertyName("variant")]
    public string? Variant { get; set; }

    /// <summary>
    /// Additional metadata associated with the evaluation result.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}
