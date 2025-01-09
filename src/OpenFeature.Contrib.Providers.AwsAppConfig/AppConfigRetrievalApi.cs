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
    public class AppConfigRetrievalApi
    {
        private const string SESSION_TOKEN_KEY_PREFIX = "session_token";
        private const string CONFIGURATION_VALUE_KEY_PREFIX = "config_value";
        private const double DEFAULT_CACHE_DURATION_MINUTES = 60;

        /// <summary>
        /// The AWS AppConfig Data client used to interact with the AWS AppConfig service.
        /// </summary>
        private readonly IAmazonAppConfigData _appConfigDataClient;

        /// <summary>
        /// The memory cache instance used for storing configuration and session data.
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
        /// <param name="cacheDuration">Optional duration for which items should be cached. Defaults to 5 minutes if not specified.</param>
        /// <exception cref="ArgumentNullException">Thrown when appConfigDataClient is null.</exception>
        public AppConfigRetrievalApi(IAmazonAppConfigData appConfigDataClient, TimeSpan? cacheDuration = null)
        {
            _appConfigDataClient = appConfigDataClient ?? throw new ArgumentNullException(nameof(appConfigDataClient));
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            
            // Default cache duration of 60 minutes if not specified
            _cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(cacheDuration ?? TimeSpan.FromMinutes(DEFAULT_CACHE_DURATION_MINUTES));
        }

        /// <summary>
        /// Retrieves configuration from AWS AppConfig using the provided configuration token.
        /// Results are cached based on the configured cache duration.
        /// </summary>
        /// <param name="configurationToken">The configuration token obtained from a configuration session.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the configuration response.</returns>
        /// <remarks>
        /// The configuration is cached using the configuration token as part of the cache key.
        /// Subsequent calls with the same token will return the cached result until it expires.
        /// </remarks>
        public async Task<GetLatestConfigurationResponse>GetLatestConfigurationAsync(FeatureFlagProfile profile)
        {
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

            if(response.Configuration == null && _memoryCache.TryGetValue(configKey, out GetLatestConfigurationResponse configValue))
            {
                // AppConfig returns null for Configuration if value hasn't changed from last retrieval, hence use what's in cache.            
                return configValue;
            }
            else
            {
                // Set the new value returned from AWS.
                _memoryCache.Set(configKey, response);
                return response;
            }
            
            
        }    

        public void InvalidateConfigurationCache(FeatureFlagProfile profile)
        {
            _memoryCache.Remove(BuildConfigurationKey(profile));
        }

        public void InvalidateSessionCache(FeatureFlagProfile profile)
        {
            _memoryCache.Remove(BuildSessionKey(profile));
        }

        // Implement IDisposable to properly clean up the MemoryCache
        public void Dispose()
        {
            if (_memoryCache is IDisposable disposableCache)
            {
                disposableCache.Dispose();
            }
        }
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

        private string BuildSessionKey(FeatureFlagProfile profile)
        {
            return $"{SESSION_TOKEN_KEY_PREFIX}_{profile}";
        }

        private string BuildConfigurationKey(FeatureFlagProfile profile)
        {
            return $"{CONFIGURATION_VALUE_KEY_PREFIX}_{profile}";
        }
    }
}