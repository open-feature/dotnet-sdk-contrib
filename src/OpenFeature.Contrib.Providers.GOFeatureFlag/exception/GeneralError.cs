using System;
using OpenFeature.Constant;
using OpenFeature.Error;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.exception
{
    /// <summary>
    ///     Exception throw when we don't have a specific case.
    /// </summary>
    public class GeneralError : FeatureProviderException
    {
        /// <summary>
        ///     Constructor of the exception
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <param name="innerException">Original exception</param>
        public GeneralError(string message, Exception innerException = null) : base(ErrorType.General, message,
            innerException)
        {
        }
    }
}