using System.Collections.Generic;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.Common.Utils;

public class State
{
    public ResolverType? ProviderResolverType { get; set; }
    public Api? Api { get; set; }
    public FeatureClient? Client { get; set; }
    public FlagState? Flag { get; set; }
    public object? FlagEvaluationDetailsResult { get; set; }
    public object? FlagResult { get; set; }
    public EvaluationContext EvaluationContext { get; set; } = EvaluationContext.Empty;
    public List<Event> Events { get; } = new();
}
