using OpenFeatureSDK;

namespace OpenFeatureContrib.Providers.Flagd
{
    /// <summary>
    /// A stub class.
    /// </summary>
    public class Stub
    {
        /// <summary>
        /// Get the provider name.
        /// </summary>
        public static string GetProviderName()
        {
            return OpenFeature.Instance.GetProviderMetadata().Name;
        }
    }
}


