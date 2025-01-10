using OpenFeature;
using Microsoft.Extensions.DependencyInjection;
using Amazon.AppConfigData;

namespace OpenFeature.Contrib.Providers.AwsAppConfig
{
    /// <summary>
    /// Provides extension methods for configuring OpenFeature with AWS AppConfig integration.
    /// This extension enables feature flag management using AWS AppConfig as the provider.
    /// </summary>
    public static class OpenFeatureExtension
    {
        /// <summary>
        /// Extension method for adding AppConfigProvider to the Serivce Collection.
        /// </summary>        
        /// <param name="application">Name of the application for AWS AppConfig</param>
        /// <param name="environment">Name of the environment for AWS AppConfig</param>
        /// <returns>The configured OpenFeature API instance.</returns>        
        public static IServiceCollection AddAppConfigProvider(this IServiceCollection services, 
        string application, string environment)
        {
            services.AddOpenFeature(featureBuilder => {
                var provider = services.BuildServiceProvider();
                var appConfigDataClient = provider.GetService<IAmazonAppConfigData>();
                var appConfigRetrievalApi = new AppConfigRetrievalApi(appConfigDataClient);

                Api.Instance.SetProviderAsync(new AppConfigProvider(appConfigRetrievalApi, application, environment));

            });
            return services;
        }
    
    }
}


