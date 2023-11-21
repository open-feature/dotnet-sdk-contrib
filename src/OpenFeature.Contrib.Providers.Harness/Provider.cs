using System;
using System.Threading.Tasks;
using io.harness.cfsdk.client.api;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.Harness;


/// <summary>
/// HarnessProvider is the .NET provider implementation for the Harness feature flag SDK
/// </summary>
public class Provider : FeatureProvider
{
    private const string HarnessProviderName = "Harness Provider";

    private readonly Metadata _metadata = new (HarnessProviderName);
    private readonly ICfClient _client;
    
    /// <summary>
    ///     Constructor of the Harness provider.
    /// </summary>
    public Provider(ICfClient client)
    {
        _client = client;
    }
    
    /// <inheritdoc/>
    public override Metadata GetMetadata()
    {
        return this._metadata;
    }
    
    /// <inheritdoc/>
    public override Task<ResolutionDetails<bool>> ResolveBooleanValue(string flagKey, bool defaultValue, EvaluationContext context = null)
    {
        var result = _client.boolVariation(flagKey, HarnessAdapter.CreateTarget(context), defaultValue);
        return Task.FromResult(HarnessAdapter.HarnessResponse(flagKey, result));
    }

    /// <inheritdoc/>
    public override Task<ResolutionDetails<string>> ResolveStringValue(string flagKey, string defaultValue, EvaluationContext context = null)
    {
        var result = _client.stringVariation(flagKey, HarnessAdapter.CreateTarget(context), defaultValue);
        return Task.FromResult(HarnessAdapter.HarnessResponse(flagKey, result));
    }

    /// <inheritdoc/>
    public override Task<ResolutionDetails<int>> ResolveIntegerValue(string flagKey, int defaultValue, EvaluationContext context = null)
    {
        var result = _client.numberVariation(flagKey, HarnessAdapter.CreateTarget(context), defaultValue);
        return Task.FromResult(HarnessAdapter.HarnessResponse(flagKey, Convert.ToInt32(result)));
    }

    /// <inheritdoc/>
    public override Task<ResolutionDetails<double>> ResolveDoubleValue(string flagKey, double defaultValue, EvaluationContext context = null)
    {
        var result = _client.numberVariation(flagKey, HarnessAdapter.CreateTarget(context), defaultValue);
        return Task.FromResult(HarnessAdapter.HarnessResponse(flagKey, result));
    }

    /// <inheritdoc/>
    public override Task<ResolutionDetails<Value>> ResolveStructureValue(string flagKey, Value defaultValue, EvaluationContext context = null)
    { 
        // TODO need to implement this
        return Task.FromResult(HarnessAdapter.HarnessResponse(flagKey, defaultValue));
    }


}