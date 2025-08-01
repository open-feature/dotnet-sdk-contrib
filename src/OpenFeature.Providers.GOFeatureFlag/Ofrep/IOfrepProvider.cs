using System;
using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Model;

namespace OpenFeature.Providers.GOFeatureFlag.Ofrep;

/// <summary>
///     IOrepProvider defines the interface for the OpenFeature provider that interacts with OFREP.
/// </summary>
public interface IOfrepProvider: IDisposable
{
    /// <param name="flagKey">Feature flag key</param>
    /// <param name="defaultValue">Default value</param>
    /// <param name="context">
    ///     <see cref="T:OpenFeature.Model.EvaluationContext" />
    /// </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>
    ///     <see cref="T:OpenFeature.Model.ResolutionDetails`1" />
    /// </returns>
    Task<ResolutionDetails<Value>> ResolveStructureValueAsync(string flagKey, Value defaultValue,
        EvaluationContext? context = null, CancellationToken? cancellationToken = null);

    /// <param name="flagKey">Feature flag key</param>
    /// <param name="defaultValue">Default value</param>
    /// <param name="context">
    ///     <see cref="T:OpenFeature.Model.EvaluationContext" />
    /// </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>
    ///     <see cref="T:OpenFeature.Model.ResolutionDetails`1" />
    /// </returns>
    Task<ResolutionDetails<string>> ResolveStringValueAsync(string flagKey, string defaultValue,
        EvaluationContext? context = null, CancellationToken? cancellationToken = null);

    /// <param name="flagKey">Feature flag key</param>
    /// <param name="defaultValue">Default value</param>
    /// <param name="context">
    ///     <see cref="T:OpenFeature.Model.EvaluationContext" />
    /// </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>
    ///     <see cref="T:OpenFeature.Model.ResolutionDetails`1" />
    /// </returns>
    Task<ResolutionDetails<int>> ResolveIntegerValueAsync(string flagKey, int defaultValue, EvaluationContext? context = null, CancellationToken? cancellationToken = null);

    /// <param name="flagKey">Feature flag key</param>
    /// <param name="defaultValue">Default value</param>
    /// <param name="context">
    ///     <see cref="T:OpenFeature.Model.EvaluationContext" />
    /// </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>
    ///     <see cref="T:OpenFeature.Model.ResolutionDetails`1" />
    /// </returns>
    Task<ResolutionDetails<double>> ResolveDoubleValueAsync(string flagKey, double defaultValue,
        EvaluationContext? context = null, CancellationToken? cancellationToken = null);

    /// <param name="flagKey">Feature flag key</param>
    /// <param name="defaultValue">Default value</param>
    /// <param name="context">
    ///     <see cref="T:OpenFeature.Model.EvaluationContext" />
    /// </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>
    ///     <see cref="T:OpenFeature.Model.ResolutionDetails`1" />
    /// </returns>
    Task<ResolutionDetails<bool>>
        ResolveBooleanValueAsync(string flagKey, bool defaultValue, EvaluationContext? context = null, CancellationToken? cancellationToken = null);


    /// <summary>
    ///     InitializeAsync initializes the OFREP provider with the given evaluation context.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task InitializeAsync(EvaluationContext context);
}
