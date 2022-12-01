using System;
using OpenFeature.Constant;
using OpenFeature.Error;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.exception
{
    /// <summary>
    ///     Exception throw when the type we received from GO Feature Flag is different than the one expected.
    /// </summary>
    public class TypeMismatchError : FeatureProviderException
    {
        /// <summary>
        ///     Constructor of the exception
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <param name="innerException">Original exception</param>
        public TypeMismatchError(string message, Exception innerException = null) : base(ErrorType.TypeMismatch,
            message, innerException)
        {
        }
    }
}