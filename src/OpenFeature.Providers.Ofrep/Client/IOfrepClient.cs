using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
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
    /// <remarks>
    /// This generic method is provided for backward compatibility.
    /// For Native AOT scenarios, use the typed methods (EvaluateBooleanFlag, EvaluateStringFlag, etc.) instead.
    /// </remarks>
    [RequiresDynamicCode(
        "Generic flag evaluation for arbitrary types requires runtime JSON serialization metadata. Use the typed evaluation methods for Native AOT scenarios.")]
    [RequiresUnreferencedCode(
        "Generic flag evaluation for arbitrary types may require trimmed type metadata. Use the typed evaluation methods for Native AOT scenarios.")]
    Task<OfrepResponse<T>> EvaluateFlag<T>(string flagKey, T defaultValue, EvaluationContext? context,
        CancellationToken cancellationToken);

    /// <summary>
    /// Evaluates a boolean flag value using the OFREP API. This method is AOT-compatible.
    /// </summary>
    /// <param name="flagKey">The key of the flag to evaluate.</param>
    /// <param name="defaultValue">The default value to return if evaluation fails.</param>
    /// <param name="context">The evaluation context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The evaluated boolean flag response.</returns>
    Task<OfrepResponse<bool>> EvaluateBooleanFlag(string flagKey, bool defaultValue, EvaluationContext? context,
        CancellationToken cancellationToken);

    /// <summary>
    /// Evaluates a string flag value using the OFREP API. This method is AOT-compatible.
    /// </summary>
    /// <param name="flagKey">The key of the flag to evaluate.</param>
    /// <param name="defaultValue">The default value to return if evaluation fails.</param>
    /// <param name="context">The evaluation context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The evaluated string flag response.</returns>
    Task<OfrepResponse<string>> EvaluateStringFlag(string flagKey, string defaultValue, EvaluationContext? context,
        CancellationToken cancellationToken);

    /// <summary>
    /// Evaluates an integer flag value using the OFREP API. This method is AOT-compatible.
    /// </summary>
    /// <param name="flagKey">The key of the flag to evaluate.</param>
    /// <param name="defaultValue">The default value to return if evaluation fails.</param>
    /// <param name="context">The evaluation context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The evaluated integer flag response.</returns>
    Task<OfrepResponse<int>> EvaluateIntegerFlag(string flagKey, int defaultValue, EvaluationContext? context,
        CancellationToken cancellationToken);

    /// <summary>
    /// Evaluates a double flag value using the OFREP API. This method is AOT-compatible.
    /// </summary>
    /// <param name="flagKey">The key of the flag to evaluate.</param>
    /// <param name="defaultValue">The default value to return if evaluation fails.</param>
    /// <param name="context">The evaluation context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The evaluated double flag response.</returns>
    Task<OfrepResponse<double>> EvaluateDoubleFlag(string flagKey, double defaultValue, EvaluationContext? context,
        CancellationToken cancellationToken);

    /// <summary>
    /// Evaluates a structured flag value using the OFREP API. This method is AOT-compatible.
    /// </summary>
    /// <param name="flagKey">The key of the flag to evaluate.</param>
    /// <param name="defaultValue">The default value to return if evaluation fails.</param>
    /// <param name="context">The evaluation context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The evaluated structured flag response.</returns>
    Task<OfrepResponse<JsonElement?>> EvaluateStructureFlag(string flagKey, JsonElement? defaultValue, EvaluationContext? context,
        CancellationToken cancellationToken);
}
