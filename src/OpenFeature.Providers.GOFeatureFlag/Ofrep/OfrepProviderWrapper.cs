using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Model;
using OpenFeature.Providers.Ofrep;

namespace OpenFeature.Providers.GOFeatureFlag.Ofrep;

/// <summary>
///     OfrepProviderWrapper is a wrapper for the OfrepProvider that implements the IOfrepProvider interface.
///     The goal of this wrapper is to provide a consistent interface for the OFREP provider, allowing it to be overriden
///     or extended without modifying the original provider.
/// </summary>
public class OfrepProviderWrapper : IOfrepProvider
{
    private readonly OfrepProvider _provider;

    /// <summary>
    ///     Constructor of the OfrepProviderWrapper
    /// </summary>
    /// <param name="provider"></param>
    public OfrepProviderWrapper(OfrepProvider provider)
    {
        this._provider = provider;
    }

    /// <param name="flagKey">Feature flag key</param>
    /// <param name="defaultValue">Default value</param>
    /// <param name="context">
    ///     <see cref="T:OpenFeature.Model.EvaluationContext" />
    /// </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>
    ///     <see cref="T:OpenFeature.Model.ResolutionDetails`1" />
    /// </returns>
    public Task<ResolutionDetails<Value>> ResolveStructureValueAsync(string flagKey, Value defaultValue,
        EvaluationContext? context = null, CancellationToken? cancellationToken = null)
    {
        return this._provider.ResolveStructureValueAsync(flagKey, defaultValue, context, cancellationToken ?? CancellationToken.None);
    }

    /// <param name="flagKey">Feature flag key</param>
    /// <param name="defaultValue">Default value</param>
    /// <param name="context">
    ///     <see cref="T:OpenFeature.Model.EvaluationContext" />
    /// </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>
    ///     <see cref="T:OpenFeature.Model.ResolutionDetails`1" />
    /// </returns>
    public Task<ResolutionDetails<string>> ResolveStringValueAsync(string flagKey, string defaultValue,
        EvaluationContext? context = null, CancellationToken? cancellationToken = null)
    {
        return this._provider.ResolveStringValueAsync(flagKey, defaultValue, context, cancellationToken ?? CancellationToken.None);
    }

    /// <param name="flagKey">Feature flag key</param>
    /// <param name="defaultValue">Default value</param>
    /// <param name="context">
    ///     <see cref="T:OpenFeature.Model.EvaluationContext" />
    /// </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>
    ///     <see cref="T:OpenFeature.Model.ResolutionDetails`1" />
    /// </returns>
    public Task<ResolutionDetails<int>> ResolveIntegerValueAsync(string flagKey, int defaultValue,
        EvaluationContext? context = null, CancellationToken? cancellationToken = null)
    {
        return this._provider.ResolveIntegerValueAsync(flagKey, defaultValue, context, cancellationToken ?? CancellationToken.None);
    }

    /// <param name="flagKey">Feature flag key</param>
    /// <param name="defaultValue">Default value</param>
    /// <param name="context">
    ///     <see cref="T:OpenFeature.Model.EvaluationContext" />
    /// </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>
    ///     <see cref="T:OpenFeature.Model.ResolutionDetails`1" />
    /// </returns>
    public Task<ResolutionDetails<double>> ResolveDoubleValueAsync(string flagKey, double defaultValue,
        EvaluationContext? context = null, CancellationToken? cancellationToken = null)
    {
        return this._provider.ResolveDoubleValueAsync(flagKey, defaultValue, context, cancellationToken ?? CancellationToken.None);
    }

    /// <param name="flagKey">Feature flag key</param>
    /// <param name="defaultValue">Default value</param>
    /// <param name="context">
    ///     <see cref="T:OpenFeature.Model.EvaluationContext" />
    /// </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>
    ///     <see cref="T:OpenFeature.Model.ResolutionDetails`1" />
    /// </returns>
    public Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(string flagKey, bool defaultValue,
        EvaluationContext? context = null, CancellationToken? cancellationToken = null)
    {
        return this._provider.ResolveBooleanValueAsync(flagKey, defaultValue, context, cancellationToken ?? CancellationToken.None);
    }

    /// <summary>
    ///     InitializeAsync initializes the OFREP provider with the given evaluation context.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public Task InitializeAsync(EvaluationContext context)
    {
        return this._provider.InitializeAsync(context);
    }

    /// <summary>
    /// Dispose method to clean up resources used by the OfrepProviderWrapper.
    /// </summary>
    public void Dispose()
    {
        this._provider.Dispose();
    }
}
