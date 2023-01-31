namespace OpenFeature.Contrib.Providers.GOFeatureFlag
{
    /// <summary>
    ///     GoFeatureFlagResponse is the response returned by the relay proxy.
    /// </summary>
    public class GoFeatureFlagResponse
    {
        /// <summary>
        ///     trackEvent is true when this call was tracked in GO Feature Flag.
        /// </summary>
        public bool trackEvents { get; set; }

        /// <summary>
        ///     variationType contains the name of the variation used for this flag.
        /// </summary>
        public string variationType { get; set; }

        /// <summary>
        ///     failed is true if GO Feature Flag had an issue.
        /// </summary>
        public bool failed { get; set; }

        /// <summary>
        ///     version of the flag used (optional)
        /// </summary>
        public string version { get; set; }

        /// <summary>
        ///     reason used to choose this variation.
        /// </summary>
        public string reason { get; set; }

        /// <summary>
        ///     errorCode is empty if everything went ok.
        /// </summary>
        public string errorCode { get; set; }

        /// <summary>
        ///     value contains the result of the flag.
        /// </summary>
        public object value { get; set; }
    }
}