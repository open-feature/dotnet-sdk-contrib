using System;
using OpenFeature.Constant;
using OpenFeature.Error;

namespace OpenFeature.Providers.GOFeatureFlag.Exceptions;

/// <summary>
///     Exception throw when we have a type that we are not able to convert.
/// </summary>
public class ImpossibleToConvertTypeException : FeatureProviderException
{
    /// <summary>
    ///     Constructor of the exception
    /// </summary>
    /// <param name="message">Message to display</param>
    /// <param name="innerException">Original exception</param>
    public ImpossibleToConvertTypeException(string message, Exception innerException = null) : base(
        ErrorType.ParseError,
        message, innerException)
    {
    }
}
