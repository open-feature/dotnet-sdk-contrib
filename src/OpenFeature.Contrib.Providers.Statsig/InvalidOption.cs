using System;

namespace OpenFeature.Contrib.Providers.Statsig
{
    /// <summary>
    ///     Exception throw when the options of the provider are invalid.
    /// </summary>
    public class StatsigProviderException : Exception
    {
        /// <summary>
        ///     Constructor of the exception
        /// </summary>
        /// <param name="message">Message to display</param>
        public StatsigProviderException(string message) : base(message)
        {
        }
    }
}