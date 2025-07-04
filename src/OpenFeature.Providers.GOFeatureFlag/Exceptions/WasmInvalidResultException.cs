using System;

namespace OpenFeature.Providers.GOFeatureFlag.Exceptions;

/// <summary>
///     WasmFunctionNotFound is thrown when a WebAssembly export does not return a valid output
/// </summary>
public class WasmInvalidResultException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="WasmInvalidResultException" /> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public WasmInvalidResultException(string message)
        : base(message)
    {
    }
}
