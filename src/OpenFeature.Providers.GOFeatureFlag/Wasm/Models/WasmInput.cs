using System.Collections.Immutable;
using System.Text.Json.Serialization;
using OpenFeature.Model;
using OpenFeature.Providers.GOFeatureFlag.Models;

namespace OpenFeature.Providers.GOFeatureFlag.Wasm.Bean;

/// <summary>
///     Represents the input to the WASM module, containing the flag key, flag, evaluation context, and flag context.
/// </summary>
public class WasmInput
{
    /// <summary>
    ///     Flag key to be evaluated.
    /// </summary>
    [JsonPropertyName("flagKey")]
    public required string FlagKey { get; set; }

    /// <summary>
    ///     Flag to be evaluated.
    /// </summary>
    [JsonPropertyName("flag")]
    public required Flag Flag { get; set; }
    
    /// <summary>
    ///     Evaluation context for a flag evaluation.
    /// </summary>
    [JsonPropertyName("evalContext")]
    public ImmutableDictionary<string, Value> EvalContext { get; set; } = ImmutableDictionary<string, Value>.Empty;

    /// <summary>
    ///     Flag context containing default SDK value and evaluation context enrichment.
    /// </summary>
    [JsonPropertyName("flagContext")]
    public FlagContext FlagContext { get; set; } = new FlagContext();
}
