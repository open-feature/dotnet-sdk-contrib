using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using OpenFeature.Error;
using OpenFeature.Model;

namespace OpenFeature.Providers.Flagd.Core;

/// <summary>
/// The in-process flagd evaluation engine: holds a flag configuration and resolves flags against it.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="FlagdCore"/> performs no I/O. Fetching, syncing and watching flag configurations is the
/// responsibility of the concrete provider, which hands each configuration to
/// <see cref="SetConfigurations"/>.
/// </para>
/// <para>
/// The typed <c>Resolve*</c> methods are synchronous and return <see cref="ResolutionDetails{T}"/>. They throw
/// the OpenFeature <see cref="FeatureProviderException"/> subtypes (for example flag not found or type
/// mismatch) that a provider is expected to surface to the SDK.
/// </para>
/// <para>
/// Instances are safe for concurrent use. Resolutions may run while <see cref="SetConfigurations"/> is applying
/// an update; each resolution sees either the previous or the new configuration in full, never a mix of both.
/// </para>
/// </remarks>
public sealed class FlagdCore
{
    private readonly JsonEvaluator _evaluator;
    private readonly IJsonSchemaValidator _schemaValidator;
    private readonly object _syncLock = new object();

    /// <summary>
    /// Creates an engine with the default options.
    /// </summary>
    public FlagdCore()
        : this(new FlagdCoreOptions())
    {
    }

    /// <summary>
    /// Creates an engine with the given options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="options"/> is <c>null</c>.</exception>
    public FlagdCore(FlagdCoreOptions options)
        : this(options, null)
    {
    }

    internal FlagdCore(FlagdCoreOptions options, IJsonSchemaValidator schemaValidator)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _schemaValidator = schemaValidator
                           ?? new JsonSchemaValidator(options.Logger ?? NullLogger.Instance);
        _evaluator = new JsonEvaluator(options.SourceSelector, _schemaValidator);
    }

    /// <summary>
    /// Loads the flag JSON schemas used to validate configurations.
    /// </summary>
    /// <remarks>
    /// Schema validation only reports problems as warnings on <see cref="FlagdCoreOptions.Logger"/>; it never
    /// rejects a configuration. Until this method has completed, configurations are not validated against the
    /// schemas. Calling it is optional.
    /// </remarks>
    /// <param name="cancellationToken">A token to cancel loading the schemas.</param>
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return _schemaValidator.InitializeAsync(cancellationToken);
    }

    /// <summary>
    /// Replaces the loaded flags and flag-set metadata with the given flagd flag configuration.
    /// </summary>
    /// <param name="json">A flagd flag configuration in JSON.</param>
    /// <returns>
    /// The keys of flags that were added, removed or whose definition changed. When the flag-set metadata changes,
    /// every flag is reported, because it is merged into each flag's resolved metadata.
    /// </returns>
    /// <exception cref="ArgumentNullException">If <paramref name="json"/> is <c>null</c>.</exception>
    /// <exception cref="ParseErrorException">
    /// If the configuration cannot be parsed. The previously loaded flags and metadata are left untouched.
    /// </exception>
    public IReadOnlyList<string> SetConfigurations(string json)
    {
        if (json == null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        lock (_syncLock)
        {
            try
            {
                return _evaluator.Sync(FlagConfigurationUpdateType.ALL, json);
            }
            catch (ParseErrorException)
            {
                throw;
            }
            catch (Exception ex) when (ex is System.Text.Json.JsonException
                                       || ex is InvalidOperationException
                                       || ex is FormatException)
            {
                throw new ParseErrorException($"Unable to parse flagd configuration: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Gets the keys of all currently loaded flags.
    /// </summary>
    /// <returns>A snapshot of the flag keys; empty when no configuration has been loaded.</returns>
    public IReadOnlyList<string> GetFlagKeys()
    {
        return new List<string>(_evaluator.Flags.Keys);
    }

    /// <summary>
    /// Gets the flag-set level metadata of the loaded configuration (for example <c>version</c>).
    /// </summary>
    /// <returns>The metadata; empty when none was provided.</returns>
    public IReadOnlyDictionary<string, object> GetFlagSetMetadata()
    {
        return _evaluator.GetFlagSetMetadata();
    }

    /// <summary>Resolves a boolean flag.</summary>
    /// <param name="flagKey">The flag key.</param>
    /// <param name="defaultValue">The value returned when the flag is disabled or has no default variant.</param>
    /// <param name="context">The evaluation context, if any.</param>
    public ResolutionDetails<bool> ResolveBoolean(string flagKey, bool defaultValue,
        EvaluationContext context = null)
    {
        return _evaluator.ResolveBooleanValueAsync(flagKey, defaultValue, context);
    }

    /// <summary>Resolves a string flag.</summary>
    /// <param name="flagKey">The flag key.</param>
    /// <param name="defaultValue">The value returned when the flag is disabled or has no default variant.</param>
    /// <param name="context">The evaluation context, if any.</param>
    public ResolutionDetails<string> ResolveString(string flagKey, string defaultValue,
        EvaluationContext context = null)
    {
        return _evaluator.ResolveStringValueAsync(flagKey, defaultValue, context);
    }

    /// <summary>Resolves an integer flag.</summary>
    /// <param name="flagKey">The flag key.</param>
    /// <param name="defaultValue">The value returned when the flag is disabled or has no default variant.</param>
    /// <param name="context">The evaluation context, if any.</param>
    public ResolutionDetails<int> ResolveInteger(string flagKey, int defaultValue,
        EvaluationContext context = null)
    {
        return _evaluator.ResolveIntegerValueAsync(flagKey, defaultValue, context);
    }

    /// <summary>Resolves a double flag.</summary>
    /// <param name="flagKey">The flag key.</param>
    /// <param name="defaultValue">The value returned when the flag is disabled or has no default variant.</param>
    /// <param name="context">The evaluation context, if any.</param>
    public ResolutionDetails<double> ResolveDouble(string flagKey, double defaultValue,
        EvaluationContext context = null)
    {
        return _evaluator.ResolveDoubleValueAsync(flagKey, defaultValue, context);
    }

    /// <summary>Resolves a structure flag.</summary>
    /// <param name="flagKey">The flag key.</param>
    /// <param name="defaultValue">The value returned when the flag is disabled or has no default variant.</param>
    /// <param name="context">The evaluation context, if any.</param>
    public ResolutionDetails<Value> ResolveStructure(string flagKey, Value defaultValue,
        EvaluationContext context = null)
    {
        return _evaluator.ResolveStructureValueAsync(flagKey, defaultValue, context);
    }
}
