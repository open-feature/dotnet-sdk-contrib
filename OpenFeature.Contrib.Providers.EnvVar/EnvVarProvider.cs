using OpenFeature.Constant;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.EnvVar;

public class EnvVarProvider : FeatureProvider
{
    private const string Name = "Environment Variable Provider";
    private readonly string Prefix;
    private delegate bool TryParse<TResult>(string value, out TResult result); 

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
    
    private Task<ResolutionDetails<T>> Resolve<T>(string flagKey, T defaultValue, TryParse<T> tryParse)
    {
        var value = Environment.GetEnvironmentVariable(Prefix + flagKey);

        return value == null
            ? Task.FromResult(new ResolutionDetails<T>(flagKey, defaultValue, ErrorType.None, Reason.Default))
            : Task.FromResult(
                tryParse(value, out var parsedValue)
                    ? new ResolutionDetails<T>(flagKey, parsedValue, ErrorType.None, Reason.Static)
                    : new ResolutionDetails<T>(flagKey, defaultValue, ErrorType.ParseError));
    }

    public override Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(string flagKey, bool defaultValue, EvaluationContext? context = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        return Resolve(flagKey, defaultValue, bool.TryParse);
    }

    public override Task<ResolutionDetails<string>> ResolveStringValueAsync(string flagKey, string defaultValue, EvaluationContext? context = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        return Resolve(flagKey, defaultValue, NoopTryParse);

        bool NoopTryParse(string value, out string result)
        {
            result = value;
            return true;
        }
    }

    public override Task<ResolutionDetails<int>> ResolveIntegerValueAsync(string flagKey, int defaultValue, EvaluationContext? context = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        return Resolve(flagKey, defaultValue, int.TryParse);
    }

    public override Task<ResolutionDetails<double>> ResolveDoubleValueAsync(string flagKey, double defaultValue, EvaluationContext? context = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        return Resolve(flagKey, defaultValue, double.TryParse);
    }

    public override Task<ResolutionDetails<Value>> ResolveStructureValueAsync(string flagKey, Value defaultValue, EvaluationContext? context = null,
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