# OpenFeature AWS AppConfig Provider

This package provides an AWS AppConfig provider implementation for OpenFeature, allowing you to manage feature flags using AWS AppConfig.

## Requirements

- open-features/dotnet-sdk
- .NET Core 3.1 and above
- AWSSDK.AppConfigData for talking to AWS AppConfig
- AWS Account and Access keys / permissions for AWS AppConfig to work with
- Microsoft.Extensions.Caching.Memory for caching local copy of AppConfig configuration

## Installation

Install the package via NuGet:

```shell
dotnet add package OpenFeature.Contrib.Providers.AwsAppConfig
```

## Config Key
A very important stuff to understand here is the way AWS Appconfig structure is designed. 

Application
└── Environment
    └── ConfigurationProfileId
        └── FeatureFlag
            └── Attribute

## Usage

### Basic Setup

AWS nuget package `AWSSDK.AppConfigData` is needed for talking to AWS AppConfig.

```csharp
namespace OpenFeatureTestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create the appliation builder per the application type. Here's example from 
            // web application
            var builder = WebApplication.CreateBuilder(args);
            ...

            // Add AWS AppConfig client
            builder.Services.AddAWSService<IAmazonAppConfigData>();

            // Add in-memory cache provider
            builder.services.AddSingleton<IMemoryCache, MemoryCache>()

            // Add OpenFeature with AWS AppConfig provider
            builder.Services.AddOpenFeature();

            var app = builder.Build();

            // Configure OpenFeature provider for AWS AppCOnfig
            var appConfigDataClient = app.Services.GetRequiredService<IAmazonAppConfigData>();
            var appConfigRetrievalApi = new AppConfigRetrievalApi(appConfigDataClient);

            // Replace these values with your AWS AppConfig settings
            const string application = "YourApplication";
            const string environment = "YourEnvironment";            

            await Api.Instance.SetProviderAsync(
                new AppConfigProvider(appConfigRetrievalApi, application, environment)
            );            
        }
    }
}
```

### Example Usage

#### Example endpoints using feature flags

```csharp
// Example endpoints using feature flags
app.MapGet("/feature-status", async (IFeatureClient featureClient) =>
{
    var key = new AppConfigKey(configurationProfileId, flagKey, attributeName);
    var isEnabled = await featureClient.GetBooleanValue(key.ToKeyString(), false);
    return Results.Ok(new { FeatureEnabled = isEnabled });
})
.WithName("GetFeatureStatus")
.WithOpenApi();

app.MapGet("/feature-config", async (IFeatureClient featureClient) =>
{
    var key = new AppConfigKey(configurationProfileId, flagKey, attributeName);
    var config = await featureClient.GetStringValue(key.ToKeyString(), "default");
    return Results.Ok(new { Configuration = config });
})
.WithName("GetFeatureConfig")
.WithOpenApi();
```

#### Example endpoint with feature flag controlling behavior

```csharp
// Example endpoint with feature flag controlling behavior
app.MapGet("/protected-feature", async (IFeatureClient featureClient) =>
{
    var isFeatureEnabled = await featureClient.GetBooleanValue("protected-feature", false);
    
    if (!isFeatureEnabled)
    {
        return Results.NotFound(new { Message = "Feature not available" });
    }

    return Results.Ok(new { Message = "Feature is enabled!" });
})
.WithName("ProtectedFeature")
.WithOpenApi();
```
