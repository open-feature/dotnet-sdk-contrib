using System;
using OpenFeature.Constant;
using OpenFeature.Error;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.exception
{
    /// <summary>
    ///     Exception throw when the Evaluation Context is invalid.
    /// </summary>
    public class InvalidEvaluationContext : FeatureProviderException
    {
        /// <summary>
        ///     Constructor of the exception
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <param name="innerException">Original exception</param>
        public InvalidEvaluationContext(string message, Exception innerException = null) : base(
            ErrorType.InvalidContext, message, innerException)
        {
        }
    }
}