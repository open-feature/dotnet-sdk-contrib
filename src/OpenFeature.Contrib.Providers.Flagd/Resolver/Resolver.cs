using System;
using System.Threading.Tasks;
using OpenFeature.Model;
using Value = OpenFeature.Model.Value;

namespace OpenFeature.Contrib.Providers.Flagd.Resolver;

internal interface Resolver
{
    event EventHandler<FlagdProviderEvent> ProviderEvent;

    Task Init();

    Task Shutdown();

    Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(string flagKey, bool defaultValue,
        EvaluationContext context = null);

    Task<ResolutionDetails<string>> ResolveStringValueAsync(string flagKey, string defaultValue,
        EvaluationContext context = null);

    Task<ResolutionDetails<int>> ResolveIntegerValueAsync(string flagKey, int defaultValue,
        EvaluationContext context = null);

    Task<ResolutionDetails<double>> ResolveDoubleValueAsync(string flagKey, double defaultValue,
        EvaluationContext context = null);

    Task<ResolutionDetails<Value>> ResolveStructureValueAsync(string flagKey, Value defaultValue,
        EvaluationContext context = null);
}
