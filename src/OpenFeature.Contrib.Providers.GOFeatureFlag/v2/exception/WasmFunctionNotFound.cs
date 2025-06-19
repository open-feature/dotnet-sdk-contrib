using System;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.v2.exception;

/// <summary>
///     WasmFunctionNotFound is thrown when a WebAssembly module does not export a required function.
/// </summary>
public class WasmFunctionNotFound : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="WasmFunctionNotFound" /> class with a specified error message.
    /// </summary>
    /// <param name="functionName">The name of the function not found.</param>
    public WasmFunctionNotFound(string functionName)
        : base($"Wasm module does not export '{functionName}' function.")
    {
    }
}
