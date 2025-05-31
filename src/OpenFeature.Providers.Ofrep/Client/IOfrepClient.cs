using System.Text.Json;
using OpenFeature.Providers.Ofrep.Models;
using OpenFeature.Model;

namespace OpenFeature.Providers.Ofrep.Client;

/// <summary>
/// Interface for the OFREP HTTP client.
/// </summary>
public interface IOfrepClient : IDisposable
{
    /// <summary>
    /// Evaluates a flag value using the OFREP API.
    /// </summary>
    /// <typeparam name="T">The type of the flag value.</typeparam>
    /// <param name="flagKey">The key of the flag to evaluate.</param>
    /// <param name="type">The type of the flag (boolean, string, etc.).</param>
    /// <param name="defaultValue">The default value to return if evaluation fails.</param>
    /// <param name="context">The evaluation context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The evaluated flag response.</returns>
    Task<OfrepResponse<T>> EvaluateFlag<T>(string flagKey, string type, T defaultValue, EvaluationContext context,
        CancellationToken cancellationToken);


    /// <summary>
    /// Evaluates all flags for a given context using the OFREP bulk evaluation API.
    /// </summary>
    /// <param name="context">The evaluation context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The bulk evaluation response containing all evaluated flags.</returns>
    /// <exception cref="OfrepConfigurationException">Thrown if the request fails due to configuration or network issues and no stale cache is available.</exception>
    /// <exception cref="JsonException">Thrown if the response cannot be parsed and no stale cache is available.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled or times out.</exception>
    Task<BulkEvaluationResponse> BulkEvaluate(EvaluationContext context, CancellationToken cancellationToken);
}
