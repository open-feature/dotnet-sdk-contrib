using System;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.exception
{
    /// <summary>
    ///     GoFeatureFlagException is the root exception of GO Feature Flag provider.
    /// </summary>
    public abstract class GoFeatureFlagException : Exception
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        public GoFeatureFlagException()
        {
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="message">Message of your exception</param>
        public GoFeatureFlagException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="message">Message of your exception</param>
        /// <param name="inner">Root exception.</param>
        public GoFeatureFlagException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}