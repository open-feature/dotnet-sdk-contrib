using System;
using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Constant;
using OpenFeature.Error;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.EnvVar;

/// <summary>
/// An OpenFeature provider using environment variables.
/// </summary>
public sealed class EnvVarProvider : FeatureProvider
{
    private const string Name = "Environment Variable Provider";
    private readonly string _prefix;
    private delegate bool TryConvert<TResult>(string value, out TResult result);

    /// <summary>
    /// Creates a new instance of <see cref="EnvVarProvider"/>
    /// </summary>
    public EnvVarProvider() : this(string.Empty)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="EnvVarProvider"/>
    /// </summary>
    /// <param name="prefix">A prefix which will be used when evaluating environment variables</param>
    public EnvVarProvider(string prefix)
    {
        _prefix = prefix;
    }

    /// <inheritdoc/>
    public override Metadata GetMetadata()
    {
        return new Metadata(Name);
    }

    private Task<ResolutionDetails<T>> Resolve<T>(string flagKey, T defaultValue, TryConvert<T> tryConvert)
    {
        var envVarName = $"{_prefix}{flagKey}";
        var value = Environment.GetEnvironmentVariable(envVarName);

        if (value == null)
            return Task.FromResult(new ResolutionDetails<T>(flagKey, defaultValue, ErrorType.FlagNotFound, Reason.Error, string.Empty, $"Unable to find environment variable '{envVarName}'"));

        if (!tryConvert(value, out var convertedValue))
            throw new FeatureProviderException(ErrorType.TypeMismatch, $"Could not convert the value of environment variable '{envVarName}' to {typeof(T)}");

        return Task.FromResult(new ResolutionDetails<T>(flagKey, convertedValue, ErrorType.None, Reason.Static));
    }

    /// <inheritdoc/>
    public override Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(string flagKey, bool defaultValue, EvaluationContext context = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        return Resolve(flagKey, defaultValue, bool.TryParse);
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public override Task<ResolutionDetails<int>> ResolveIntegerValueAsync(string flagKey, int defaultValue, EvaluationContext context = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        return Resolve(flagKey, defaultValue, int.TryParse);
    }

    /// <inheritdoc/>
    public override Task<ResolutionDetails<double>> ResolveDoubleValueAsync(string flagKey, double defaultValue, EvaluationContext context = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        return Resolve(flagKey, defaultValue, double.TryParse);
    }

    /// <inheritdoc/>
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
