using System;

namespace OpenFeature.Providers.GOFeatureFlag.exception;

/// <summary>
///     WasmNotLoaded is thrown when the WASM module is not loaded correctly.
/// </summary>
public class WasmNotLoadedException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="WasmNotLoadedException" /> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public WasmNotLoadedException(string message) : base(message)
    {
    }
}
