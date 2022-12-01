namespace OpenFeature.Contrib.Providers.GOFeatureFlag.exception
{
    /// <summary>
    ///     Exception throw when the options of the provider are invalid.
    /// </summary>
    public class InvalidOption : GoFeatureFlagException
    {
        /// <summary>
        ///     Constructor of the exception
        /// </summary>
        /// <param name="message">Message to display</param>
        public InvalidOption(string message) : base(message)
        {
        }
    }
}