using System;
using System.Net.Http;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag
{
    /// <Summary>
    ///     GoFeatureFlagProviderOptions contains the options to initialise the provider.
    /// </Summary>
    public class GoFeatureFlagProviderOptions
    {
        /// <Summary>
        ///     (mandatory) endpoint contains the DNS of your GO Feature Flag relay proxy
        ///     example: https://mydomain.com/gofeatureflagproxy/
        /// </Summary>
        public string Endpoint { get; set; }

        /// <Summary>
        ///     (optional) timeout we are waiting when calling the go-feature-flag relay proxy API.
        ///     Default: 10000 ms
        /// </Summary>
        public TimeSpan Timeout { get; set; } = new TimeSpan(10000 * TimeSpan.TicksPerMillisecond);

        /// <Summary>
        ///     (optional) If you want to provide your own HttpMessageHandler.
        ///     Default: null
        /// </Summary>
        public HttpMessageHandler HttpMessageHandler { get; set; }
    }
}