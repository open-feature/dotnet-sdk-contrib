using System;
using OpenFeature.Constant;
using OpenFeature.Error;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.exception
{
    /// <summary>
    ///     Exception throw when we have a type that we are not able to convert.
    /// </summary>
    public class ImpossibleToConvertTypeError : FeatureProviderException
    {
        /// <summary>
        ///     Constructor of the exception
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <param name="innerException">Original exception</param>
        public ImpossibleToConvertTypeError(string message, Exception innerException = null) : base(
            ErrorType.ParseError,
            message, innerException)
        {
        }
    }
}