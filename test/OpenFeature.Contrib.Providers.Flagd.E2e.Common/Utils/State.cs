using System.Collections.Generic;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.Common.Utils;

#nullable enable
public class State
{
    public ResolverType? ProviderResolverType { get; set; }
    public FlagdConfigBuilder? FlagdConfig { get; set; }
    public Api? Api { get; set; }
    public FeatureClient? Client { get; set; }
    public FlagState? Flag { get; set; }
    public object? FlagEvaluationDetailsResult { get; set; }
    public object? FlagResult { get; set; }
    public EvaluationContextBuilder EvaluationContextBuilder { get; set; } = EvaluationContext.Builder();
    public List<Event> Events { get; } = new();
}
