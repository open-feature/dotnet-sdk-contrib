using System;
using OpenFeature.Constant;
using OpenFeature.Error;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.exception
{
    /// <summary>
    ///     Exception throw when The Evaluation Context does not contains a targetingKey field.
    /// </summary>
    public class InvalidTargetingKey : FeatureProviderException
    {
        /// <summary>
        ///     Constructor of the exception
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <param name="innerException">Original exception</param>
        public InvalidTargetingKey(string message, Exception innerException = null) : base(ErrorType.InvalidContext,
            message, innerException)
        {
        }
    }
}