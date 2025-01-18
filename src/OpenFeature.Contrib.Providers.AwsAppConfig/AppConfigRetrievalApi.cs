using System;
using System.Threading.Tasks;
using Amazon.AppConfigData;
using Microsoft.Extensions.Caching.Memory;
using Amazon.AppConfigData.Model;

namespace OpenFeature.Contrib.Providers.AwsAppConfig
{
    /// <summary>
    /// Provides functionality to interact with AWS AppConfig Data API with built-in memory caching support.
    /// This class handles configuration retrieval and session management for AWS AppConfig feature flags.
    /// </summary>
    /// <remarks>
    /// This class implements caching mechanisms to optimize performance and reduce API calls to AWS AppConfig.
    /// Key features:
    /// - Caches configuration responses and session data
    /// - Configurable cache duration (defaults to 5 minutes)
    /// - Thread-safe cache operations using GetOrCreateAsync
    /// - Supports manual cache invalidation
    /// - Implements IDisposable for proper resource cleanup
    /// </remarks>
    public class AppConfigRetrievalApi: IRetrievalApi
    {
        /// <summary>
        /// Prefix used for session token cache keys to prevent key collisions.
        /// </summary>
        private const string SESSION_TOKEN_KEY_PREFIX = "session_token";

        /// <summary>
        /// Prefix used for configuration value cache keys to prevent key collisions.
        /// </summary>
        private const string CONFIGURATION_VALUE_KEY_PREFIX = "config_value";

        /// <summary>
        /// Default cache duration in minutes for configuration and session data.
        /// </summary>
        private const double DEFAULT_CACHE_DURATION_MINUTES = 60;

        /// <summary>
        /// AWS AppConfig Data client used to interact with the AWS AppConfig service.
        /// </summary>
        private readonly IAmazonAppConfigData _appConfigDataClient;

        /// <summary>
        /// Memory cache instance used for storing configuration and session data.
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// Cache entry options defining how items are cached, including expiration settings.
        /// </summary>
        private readonly MemoryCacheEntryOptions _cacheOptions;


        /// <summary>
        /// Initializes a new instance of the AppConfigRetrievalApi class.
        /// </summary>
        /// <param name="appConfigDataClient">The AWS AppConfig Data client used to interact with the AWS AppConfig service.</param>
        /// <param name="memoryCache">MemoryCache instance used for caching. If null, Default is instantiated.</param>
        /// <param name="cacheDuration">Optional duration for which items should be cached. Defaults to 5 minutes if not specified.</param>
        /// <exception cref="ArgumentNullException">Thrown when appConfigDataClient is null.</exception>
        public AppConfigRetrievalApi(IAmazonAppConfigData appConfigDataClient, IMemoryCache memoryCache, TimeSpan? cacheDuration = null)
        {
            _appConfigDataClient = appConfigDataClient ?? throw new ArgumentNullException(nameof(appConfigDataClient));
            _memoryCache = memoryCache ?? new MemoryCache(new MemoryCacheOptions());
            
            // Default cache duration of 60 minutes if not specified
            _cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(cacheDuration ?? TimeSpan.FromMinutes(DEFAULT_CACHE_DURATION_MINUTES));
        }

        /// <summary>
        /// Retrieves configuration from AWS AppConfig using the provided feature flag profile.
        /// Results are cached based on the configured cache duration.
        /// </summary>
        /// <param name="profile">The feature flag profile containing application, environment, and configuration identifiers.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the configuration response.</returns>
        /// <remarks>
        /// The configuration is cached using the profile information as part of the cache key.
        /// If AWS returns an empty configuration, it indicates no changes from the previous configuration,
        /// and the cached value will be returned if available.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown when the provided profile is invalid.</exception>
        /// <exception cref="AmazonAppConfigDataException">Thrown when unable to connect to AWS or retrieve configuration.</exception>
        public async Task<GetLatestConfigurationResponse>GetLatestConfigurationAsync(FeatureFlagProfile profile)
        {
            if(!profile.IsValid) throw new ArgumentException("Invalid Feature Flag configuration profile");

            var configKey = BuildConfigurationKey(profile);
            var sessionKey = BuildSessionKey(profile);

            // Build GetLatestConfiguration Request
            var configurationRequest = new GetLatestConfigurationRequest
            {
                ConfigurationToken = await GetSessionToken(profile)
            };

            var response = await _appConfigDataClient.GetLatestConfigurationAsync(configurationRequest);

            // If not NextPollConfigurationToken, something wrong with AWS connection.
            if(string.IsNullOrWhiteSpace(response.NextPollConfigurationToken)) throw new Exception("Unable to connect to AWS");

            // First, update the session token to the newly returned token
            _memoryCache.Set(sessionKey, response.NextPollConfigurationToken);

            if((response.Configuration == null || response.Configuration.Length == 0) 
                && _memoryCache.TryGetValue(configKey, out GetLatestConfigurationResponse configValue))
            {
                // AppConfig returns empty Configuration if value hasn't changed from last retrieval, hence use what's in cache.            
                return configValue;
            }
            else
            {
                // Set the new value returned from AWS.
                _memoryCache.Set(configKey, response);
                return response;
            }            
        }

        /// <summary>
        /// Invalidates the cached configuration for the specified feature flag profile.
        /// </summary>
        /// <param name="profile">The feature flag profile whose configuration cache should be invalidated.</param>
        /// <remarks>
        /// This method forces the next GetLatestConfigurationAsync call to fetch fresh data from AWS AppConfig
        /// instead of using cached values.
        /// </remarks>
        public void InvalidateConfigurationCache(FeatureFlagProfile profile)
        {
            _memoryCache.Remove(BuildConfigurationKey(profile));
        }

        /// <summary>
        /// Invalidates the cached session token for the specified feature flag profile.
        /// </summary>
        /// <param name="profile">The feature flag profile whose session token cache should be invalidated.</param>
        /// <remarks>
        /// This method forces the next operation to create a new session with AWS AppConfig
        /// instead of using the cached session token.
        /// </remarks>
        public void InvalidateSessionCache(FeatureFlagProfile profile)
        {
            _memoryCache.Remove(BuildSessionKey(profile));
        }

        /// <summary>
        /// Releases all resources used by the AppConfigRetrievalApi instance.
        /// </summary>
        /// <remarks>
        /// This method ensures proper cleanup of the memory cache when the instance is disposed.
        /// </remarks>
        public void Dispose()
        {
            if (_memoryCache is IDisposable disposableCache)
            {
                disposableCache.Dispose();
            }
        }

        /// <summary>
        /// Retrieves or creates a new session token for the specified feature flag profile.
        /// </summary>
        /// <param name="profile">The feature flag profile for which to get a session token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the session token.</returns>
        /// <exception cref="ArgumentException">Thrown when the provided profile is invalid.</exception>
        /// <remarks>
        /// Session tokens are cached according to the configured cache duration to minimize API calls to AWS AppConfig.
        /// </remarks>
        private async Task<string> GetSessionToken(FeatureFlagProfile profile)
        {
            if(!profile.IsValid) throw new ArgumentException("Invalid Feature Flag configuration profile");

            return await _memoryCache.GetOrCreateAsync(BuildSessionKey(profile), async entry =>
            {
                entry.SetOptions(_cacheOptions);

                var request = new StartConfigurationSessionRequest
                {
                    ApplicationIdentifier = profile.ApplicationIdentifier,
                    EnvironmentIdentifier = profile.EnvironmentIdentifier,
                    ConfigurationProfileIdentifier = profile.ConfigurationProfileIdentifier
                };

                var sessionResponse = await _appConfigDataClient.StartConfigurationSessionAsync(request);
                // We only need Initial Configuration Token from starting the session.
                return sessionResponse.InitialConfigurationToken;
            });
        }

        /// <summary>
        /// Retrieves or creates a new session token for the specified feature flag profile.
        /// </summary>
        /// <param name="profile">The feature flag profile for which to get a session token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the session token.</returns>
        /// <exception cref="ArgumentException">Thrown when the provided profile is invalid.</exception>
        /// <remarks>
        /// Session tokens are cached according to the configured cache duration to minimize API calls to AWS AppConfig.
        /// </remarks>
        private string BuildSessionKey(FeatureFlagProfile profile)
        {
            return $"{SESSION_TOKEN_KEY_PREFIX}_{profile}";
        }

        /// <summary>
        /// Builds a cache key for configuration values based on the feature flag profile.
        /// </summary>
        /// <param name="profile">The feature flag profile to use in the key generation.</param>
        /// <returns>A unique cache key for the configuration value.</returns>
        private string BuildConfigurationKey(FeatureFlagProfile profile)
        {
            return $"{CONFIGURATION_VALUE_KEY_PREFIX}_{profile}";
        }
    }
}