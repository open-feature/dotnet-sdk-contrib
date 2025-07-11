# Azure App Configuration Provider for .NET

An OpenFeature provider for Azure App Configuration that supports both feature flags and configuration values.

## Overview

This provider allows you to use Azure App Configuration as a feature flag and configuration provider for OpenFeature. It supports:

-   **Feature Flags**: Azure App Configuration feature flags with conditions and filters
-   **Configuration Values**: Regular configuration key-value pairs
-   **Targeting**: User and group-based targeting
-   **Percentage Rollouts**: Gradual feature rollouts

## Installation

Add the NuGet package to your project:

```bash
dotnet add package OpenFeature.Providers.AzureAppConfig
```

## Usage

### Basic Setup

```csharp
using OpenFeature;
using OpenFeature.Providers.AzureAppConfig;

// Using connection string
var connectionString = "Endpoint=https://your-app-config.azconfig.io;Id=...;Secret=...";
var provider = new AzureAppConfigProvider(connectionString);

await OpenFeature.Api.Instance.SetProviderAsync(provider);
var client = OpenFeature.Api.Instance.GetClient();

// Use feature flags
var isEnabled = await client.GetBooleanValueAsync("my-feature", false);

// Use configuration values
var setting = await client.GetStringValueAsync("my-setting", "default-value");
```

### Using Azure Identity

```csharp
using Azure.Data.AppConfiguration;
using Azure.Identity;
using OpenFeature.Providers.AzureAppConfig;

var endpoint = "https://your-app-config.azconfig.io";
var credential = new DefaultAzureCredential();
var configClient = new ConfigurationClient(new Uri(endpoint), credential);

var provider = new AzureAppConfigProvider(configClient);
await OpenFeature.Api.Instance.SetProviderAsync(provider);
```

### Custom Configuration

```csharp
var options = new AzureAppConfigProviderOptions
{
    FeatureFlagPrefix = ".appconfig.featureflag/",
    ConfigurationPrefix = "MyApp:",
    Label = "Production"
};

var provider = new AzureAppConfigProvider(connectionString, options);
```

### Using with Evaluation Context

```csharp
var context = EvaluationContext.Builder()
    .Set("userId", "user123")
    .Set("groups", new List<string> { "beta-users", "premium-users" })
    .Build();

var isEnabled = await client.GetBooleanValueAsync("beta-feature", false, context);
```

## Feature Flag Support

The provider supports Azure App Configuration feature flags with the following filters:

### Percentage Filter

Enable a feature for a percentage of users:

```json
{
    "id": "my-feature",
    "enabled": true,
    "conditions": {
        "client_filters": [
            {
                "name": "Microsoft.Percentage",
                "parameters": {
                    "Value": 50
                }
            }
        ]
    }
}
```

### Targeting Filter

Enable a feature for specific users or groups:

```json
{
    "id": "my-feature",
    "enabled": true,
    "conditions": {
        "client_filters": [
            {
                "name": "Microsoft.Targeting",
                "parameters": {
                    "Audience": {
                        "Users": ["user1", "user2"],
                        "Groups": ["beta-users", "premium-users"],
                        "DefaultRolloutPercentage": 25
                    }
                }
            }
        ]
    }
}
```

## Configuration Options

| Option                | Description                           | Default                   |
| --------------------- | ------------------------------------- | ------------------------- |
| `FeatureFlagPrefix`   | Prefix for feature flag keys          | `.appconfig.featureflag/` |
| `ConfigurationPrefix` | Prefix for configuration keys         | `""` (empty)              |
| `Label`               | Label filter for configuration values | `null`                    |

## Supported Data Types

-   **Boolean**: Feature flags and boolean configuration values
-   **String**: Text configuration values
-   **Integer**: Numeric configuration values
-   **Double**: Floating-point configuration values
-   **Structure**: JSON configuration values parsed as OpenFeature Value objects

## Error Handling

The provider handles common Azure App Configuration errors:

-   **404 Not Found**: Returns `ErrorType.FlagNotFound`
-   **Authentication Errors**: Returns `ErrorType.General`
-   **Network Errors**: Returns `ErrorType.General`
-   **Type Mismatch**: Returns `ErrorType.TypeMismatch` when parsing fails

## Best Practices

1. **Connection Management**: Reuse the `ConfigurationClient` instance across multiple provider instances
2. **Caching**: Consider implementing caching for frequently accessed configuration values
3. **Error Handling**: Always provide sensible default values for feature flags and configuration
4. **Security**: Use Azure Identity (Managed Identity, Service Principal) instead of connection strings in production
5. **Monitoring**: Monitor Azure App Configuration usage and implement proper logging

## Example: ASP.NET Core Integration

```csharp
using Azure.Data.AppConfiguration;
using Azure.Identity;
using OpenFeature;
using OpenFeature.Providers.AzureAppConfig;

var builder = WebApplication.CreateBuilder(args);

// Configure Azure App Configuration
var endpoint = builder.Configuration["AzureAppConfig:Endpoint"];
var configClient = new ConfigurationClient(new Uri(endpoint), new DefaultAzureCredential());

// Configure OpenFeature
var provider = new AzureAppConfigProvider(configClient);
await OpenFeature.Api.Instance.SetProviderAsync(provider);

builder.Services.AddSingleton(OpenFeature.Api.Instance.GetClient());

var app = builder.Build();

// Use in controllers
app.MapGet("/", async (IFeatureClient featureClient) =>
{
    var isNewFeatureEnabled = await featureClient.GetBooleanValueAsync("new-ui", false);
    return isNewFeatureEnabled ? "New UI" : "Classic UI";
});

app.Run();
```

## License

Apache 2.0 - See [LICENSE](../../LICENSE) for more information.
