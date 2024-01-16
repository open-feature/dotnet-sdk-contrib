using System.Threading.Tasks;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.Flagd
{
    internal class InProcessResolver : Resolver {
        public void Init()
        {
            throw new System.NotImplementedException();
        }

        public void Shutdown()
        {
            throw new System.NotImplementedException();
        }

        public Task<ResolutionDetails<bool>> ResolveBooleanValue(string flagKey, bool defaultValue, EvaluationContext context = null)
        {
            throw new System.NotImplementedException();
        }

        public Task<ResolutionDetails<string>> ResolveStringValue(string flagKey, string defaultValue, EvaluationContext context = null)
        {
            throw new System.NotImplementedException();
        }

        public Task<ResolutionDetails<int>> ResolveIntegerValue(string flagKey, int defaultValue, EvaluationContext context = null)
        {
            throw new System.NotImplementedException();
        }

        public Task<ResolutionDetails<double>> ResolveDoubleValue(string flagKey, double defaultValue, EvaluationContext context = null)
        {
            throw new System.NotImplementedException();
        }

        public Task<ResolutionDetails<Value>> ResolveStructureValue(string flagKey, Value defaultValue, EvaluationContext context = null)
        {
            throw new System.NotImplementedException();
        }
    }    
}
