namespace OpenFeature.Contrib.Providers.GOFeatureFlag
{
    /// <summary>
    ///     GOFeatureFlagRequest is the object formatting the request to the relay proxy.
    /// </summary>
    /// <typeparam name="T">Type of the default value.</typeparam>
    public class GOFeatureFlagRequest<T>
    {
        /// <summary>
        ///     GoFeatureFlagUser is the representation of the user.
        /// </summary>
        public GoFeatureFlagUser User { get; set; }

        /// <summary>
        ///     default value if we have an error.
        /// </summary>
        public T DefaultValue { get; set; }
    }
}