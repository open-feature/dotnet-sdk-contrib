using System;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using OpenFeature.Model;
using Value = OpenFeature.Model.Value;

namespace OpenFeature.Contrib.Providers.Flagd
{
    internal interface Resolver
    {
        void Init();
        void Shutdown();

        Task<ResolutionDetails<bool>> ResolveBooleanValue(string flagKey, bool defaultValue,
            EvaluationContext context = null);

        Task<ResolutionDetails<string>> ResolveStringValue(string flagKey, string defaultValue,
            EvaluationContext context = null);

        Task<ResolutionDetails<int>> ResolveIntegerValue(string flagKey, int defaultValue,
            EvaluationContext context = null);

        Task<ResolutionDetails<double>> ResolveDoubleValue(string flagKey, double defaultValue,
            EvaluationContext context = null);

        Task<ResolutionDetails<Value>> ResolveStructureValue(string flagKey, Value defaultValue,
            EvaluationContext context = null);
    }
}
