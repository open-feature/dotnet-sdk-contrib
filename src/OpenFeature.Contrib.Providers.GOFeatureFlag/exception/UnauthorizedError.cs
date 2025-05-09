using System;
using OpenFeature.Constant;
using OpenFeature.Error;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.exception;

/// <summary>
///     Exception throw when we are not authorized to call the API in the relay proxy.
/// </summary>
public class UnauthorizedError : FeatureProviderException
{
    /// <summary>
    ///     Constructor of the exception
    /// </summary>
    /// <param name="message">Message to display</param>
    /// <param name="innerException">Original exception</param>
    public UnauthorizedError(string message, Exception innerException = null) : base(ErrorType.General, message,
        innerException)
    {
    }
}
