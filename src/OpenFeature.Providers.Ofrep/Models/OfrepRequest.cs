using System.Text.Json.Serialization;

namespace OpenFeature.Providers.Ofrep.Models;

/// <summary>
/// Represents a request sent to the OFREP API.
/// </summary>
internal class OfrepRequest
{
    /// <summary>
    /// Gets or sets the evaluation context for the request.
    /// </summary>
    [JsonPropertyName("context")]
    public object? Context { get; set; }
}
