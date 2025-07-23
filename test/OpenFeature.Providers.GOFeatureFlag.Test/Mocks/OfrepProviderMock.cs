using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Constant;
using OpenFeature.Model;
using OpenFeature.Providers.GOFeatureFlag.Ofrep;

namespace OpenFeature.Providers.GOFeatureFlag.Test.Mocks;

public class OfrepProviderMock : IOfrepProvider
{
    public EvaluationContext LastEvaluationContext { get; private set; }

    public Task<ResolutionDetails<Value>> ResolveStructureValueAsync(string flagKey, Value defaultValue,
        EvaluationContext context, CancellationToken? cancellationToken = null)
    {
        this.LastEvaluationContext = context;
        return Task.FromResult(new ResolutionDetails<Value>(
            flagKey,
            new Value("this is a test value"),
            ErrorType.None,
            Reason.TargetingMatch,
            "enabled",
            null,
            new ImmutableMetadata(new Dictionary<string, object>
            {
                { "test", new Value("this is a test value") },
                { "test2", new Value(42) },
                { "test3", new Value(true) },
                { "test4", new Value(3.14) }
            })
        ));
    }

    public Task<ResolutionDetails<string>> ResolveStringValueAsync(string flagKey, string defaultValue,
        EvaluationContext context, CancellationToken? cancellationToken = null)
    {
        this.LastEvaluationContext = context;
        return Task.FromResult(new ResolutionDetails<string>(
            flagKey,
            "this is a test value",
            ErrorType.None,
            Reason.TargetingMatch,
            "enabled",
            null,
            new ImmutableMetadata(new Dictionary<string, object>
            {
                { "test", new Value("this is a test value") },
                { "test2", new Value(42) },
                { "test3", new Value(true) },
                { "test4", new Value(3.14) }
            })
        ));
    }

    public Task<ResolutionDetails<int>> ResolveIntegerValueAsync(string flagKey, int defaultValue,
        EvaluationContext context, CancellationToken? cancellationToken = null)
    {
        this.LastEvaluationContext = context;
        return Task.FromResult(new ResolutionDetails<int>(
            flagKey,
            12,
            ErrorType.None,
            Reason.TargetingMatch,
            "enabled",
            null,
            new ImmutableMetadata(new Dictionary<string, object>
            {
                { "test", new Value("this is a test value") },
                { "test2", new Value(42) },
                { "test3", new Value(true) },
                { "test4", new Value(3.14) }
            })
        ));
    }

    public Task<ResolutionDetails<double>> ResolveDoubleValueAsync(string flagKey, double defaultValue,
        EvaluationContext context, CancellationToken? cancellationToken = null)
    {
        this.LastEvaluationContext = context;
        return Task.FromResult(new ResolutionDetails<double>(
            flagKey,
            12.21,
            ErrorType.None,
            Reason.TargetingMatch,
            "enabled",
            null,
            new ImmutableMetadata(new Dictionary<string, object>
            {
                { "test", new Value("this is a test value") },
                { "test2", new Value(42) },
                { "test3", new Value(true) },
                { "test4", new Value(3.14) }
            })
        ));
    }

    public Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(string flagKey, bool defaultValue,
        EvaluationContext context, CancellationToken? cancellationToken = null)
    {
        this.LastEvaluationContext = context;
        return Task.FromResult(new ResolutionDetails<bool>(
            flagKey,
            true,
            ErrorType.None,
            Reason.TargetingMatch,
            "enabled",
            null,
            new ImmutableMetadata(new Dictionary<string, object>
            {
                { "test", new Value("this is a test value") },
                { "test2", new Value(42) },
                { "test3", new Value(true) },
                { "test4", new Value(3.14) }
            })
        ));
    }

    public Task InitializeAsync(EvaluationContext context)
    {
        return Task.CompletedTask;
    }
}
