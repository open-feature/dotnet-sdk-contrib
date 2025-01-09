using System;
using System.Threading.Tasks;
using Amazon.AppConfigData.Model;

namespace OpenFeature.Contrib.Providers.AwsAppConfig
{
    /// <summary>
    /// Defines the contract for interacting with AWS AppConfig Data API with caching support.
    /// </summary>
    public interface IRetrievalApi : IDisposable
    {
        /// <summary>
        /// Retrieves configuration from AWS AppConfig using the provided feature flag profile.
        /// Results are cached based on the configured cache duration.
        /// </summary>
        /// <param name="profile">The feature flag profile containing application, environment, and configuration identifiers.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the configuration response.</returns>
        /// <exception cref="Exception">Thrown when unable to connect to AWS or retrieve configuration.</exception>
        Task<GetLatestConfigurationResponse> GetLatestConfigurationAsync(FeatureFlagProfile profile);

        /// <summary>
        /// Invalidates the cached configuration for the specified feature flag profile.
        /// </summary>
        /// <param name="profile">The feature flag profile whose configuration cache should be invalidated.</param>
        /// <remarks>
        /// This method forces the next GetLatestConfigurationAsync call to fetch fresh data from AWS AppConfig
        /// instead of using cached values.
        /// </remarks>
        void InvalidateConfigurationCache(FeatureFlagProfile profile);

        /// <summary>
        /// Invalidates the cached session token for the specified feature flag profile.
        /// </summary>
        /// <param name="profile">The feature flag profile whose session token cache should be invalidated.</param>
        /// <remarks>
        /// This method forces the next operation to create a new session with AWS AppConfig
        /// instead of using the cached session token.
        /// </remarks>
        void InvalidateSessionCache(FeatureFlagProfile profile);
    }
}

