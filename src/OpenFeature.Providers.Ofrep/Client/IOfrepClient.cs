using OpenFeature.Model;
using OpenFeature.Providers.Ofrep.Models;

namespace OpenFeature.Providers.Ofrep.Client;

/// <summary>
/// Interface for the OFREP HTTP client.
/// </summary>
internal interface IOfrepClient : IDisposable
{
    /// <summary>
    /// Evaluates a flag value using the OFREP API.
    /// </summary>
    /// <typeparam name="T">The type of the flag value.</typeparam>
    /// <param name="flagKey">The key of the flag to evaluate.</param>
    /// <param name="defaultValue">The default value to return if evaluation fails.</param>
    /// <param name="context">The evaluation context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The evaluated flag response.</returns>
    Task<OfrepResponse<T>> EvaluateFlag<T>(string flagKey, T defaultValue, EvaluationContext? context,
        CancellationToken cancellationToken);
}
