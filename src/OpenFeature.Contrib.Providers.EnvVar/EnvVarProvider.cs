using System;
using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Constant;
using OpenFeature.Error;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.EnvVar
{
    public class EnvVarProvider : FeatureProvider
    {
        private const string Name = "Environment Variable Provider";
        private readonly string Prefix;
        private delegate bool TryConvert<TResult>(string value, out TResult result); 

        public EnvVarProvider() : this("")
        {
        }

        public EnvVarProvider(string prefix)
        {
            Prefix = prefix; 
        }
    
        public override Metadata GetMetadata()
        {
            return new Metadata(Name);
        }
    
        private Task<ResolutionDetails<T>> Resolve<T>(string flagKey, T defaultValue, TryConvert<T> tryConvert)
        {
            var envVarName = $"{Prefix}{flagKey}";  
            var value = Environment.GetEnvironmentVariable(envVarName);

            if (value == null)
                return Task.FromResult(new ResolutionDetails<T>(flagKey, defaultValue, ErrorType.None, Reason.Default));
        
            if (!tryConvert(value, out var convertedValue))
                throw new FeatureProviderException(ErrorType.TypeMismatch, $"Could not convert the value of environment variable '{envVarName}' to {typeof(T)}");

            return Task.FromResult(new ResolutionDetails<T>(flagKey, convertedValue, ErrorType.None, Reason.Static));
        }

        public override Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(string flagKey, bool defaultValue, EvaluationContext context = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Resolve(flagKey, defaultValue, bool.TryParse);
        }

        public override Task<ResolutionDetails<string>> ResolveStringValueAsync(string flagKey, string defaultValue, EvaluationContext context = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Resolve(flagKey, defaultValue, NoopTryParse);

            bool NoopTryParse(string value, out string result)
            {
                result = value;
                return true;
            }
        }

        public override Task<ResolutionDetails<int>> ResolveIntegerValueAsync(string flagKey, int defaultValue, EvaluationContext context = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Resolve(flagKey, defaultValue, int.TryParse);
        }

        public override Task<ResolutionDetails<double>> ResolveDoubleValueAsync(string flagKey, double defaultValue, EvaluationContext context = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Resolve(flagKey, defaultValue, double.TryParse);
        }

        public override Task<ResolutionDetails<Value>> ResolveStructureValueAsync(string flagKey, Value defaultValue, EvaluationContext context = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Resolve(flagKey, defaultValue, ConvertStringToValue);

            bool ConvertStringToValue(string s, out Value value)
            {
                value = new Value(s);
                return true;
            }
        }
    }
}