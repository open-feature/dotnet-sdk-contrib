using System;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.exception;

/// <summary>
///     WasmNotLoaded is thrown when the WASM module is not loaded correctly.
/// </summary>
public class WasmNotLoaded
    : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="WasmNotLoaded" /> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public WasmNotLoaded(string message) : base(message)
    {
    }
}
