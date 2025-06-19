using System;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.v2.exception;

/// <summary>
///     WasmFunctionNotFound is thrown when a WebAssembly export does not return a valid output
/// </summary>
public class WasmInvalidResult : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="WasmInvalidResult" /> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public WasmInvalidResult(string message)
        : base(message)
    {
    }
}
