using System;

namespace OpenFeature.Providers.GOFeatureFlag.exception;

/// <summary>
///     WasmFunctionNotFound is thrown when a WebAssembly module does not export a required function.
/// </summary>
public class WasmFunctionNotFoundException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="WasmFunctionNotFoundException" /> class with a specified error message.
    /// </summary>
    /// <param name="functionName">The name of the function not found.</param>
    public WasmFunctionNotFoundException(string functionName)
        : base($"Wasm module does not export '{functionName}' function.")
    {
    }
}
