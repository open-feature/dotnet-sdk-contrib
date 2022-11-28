namespace OpenFeature.Contrib.Providers.GOFeatureFlag
{
    /// <summary>
    ///     GoFeatureFlagResponse is the response returned by the relay proxy.
    /// </summary>
    public class GoFeatureFlagResponse
    {
        public bool trackEvents { get; set; }
        public string variationType { get; set; }
        public bool failed { get; set; }
        public string version { get; set; }
        public string reason { get; set; }
        public string errorCode { get; set; }
        public object value { get; set; }
    }
}