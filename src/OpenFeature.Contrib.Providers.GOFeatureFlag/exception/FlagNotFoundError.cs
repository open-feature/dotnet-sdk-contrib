using System;
using OpenFeature.Constant;
using OpenFeature.Error;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.exception
{
    /// <summary>
    ///     Exception thrown when the flag is not found by GO Feature Flag relay proxy.
    /// </summary>
    public class FlagNotFoundError : FeatureProviderException
    {
        /// <summary>
        ///     Constructor of the exception
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <param name="innerException">Original exception</param>
        public FlagNotFoundError(string message, Exception innerException = null) : base(ErrorType.FlagNotFound,
            message, innerException)
        {
        }
    }
}