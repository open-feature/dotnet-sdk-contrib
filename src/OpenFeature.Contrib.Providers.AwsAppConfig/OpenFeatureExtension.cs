using OpenFeature;
using Microsoft.Extensions.DependencyInjection;
using Amazon.AppConfigData;

namespace OpenFeature.Contrib.Providers.AwsAppConfig
{
    public static class OpenFeatureExtension
    {        
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


