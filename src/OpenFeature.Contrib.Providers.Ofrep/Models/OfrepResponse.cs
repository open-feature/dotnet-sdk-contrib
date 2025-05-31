using System.Text.Json.Serialization;

namespace OpenFeature.Contrib.Providers.Ofrep.Models;

/// <summary>
/// Represents a response from the OFREP API for a single flag evaluation.
/// Inherits common evaluation properties from <see cref="OfrepEvaluationBase{T}"/>.
/// </summary>
/// <typeparam name="T">The type of the flag value.</typeparam>
public class OfrepResponse<T> : OfrepEvaluationBase<T>
{
    /// <summary>
    /// An error code indicating the reason for evaluation failure, if any.
    /// </summary>
    [JsonPropertyName("errorCode")]
    public string ErrorCode { get; set; }

    /// <summary>
    /// A detailed error message accompanying the error code, if any.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string ErrorMessage { get; set; }
}
