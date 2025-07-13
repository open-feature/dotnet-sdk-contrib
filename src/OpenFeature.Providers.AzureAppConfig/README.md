# Azure App Configuration Provider for .NET

[![NuGet version](https://badge.fury.io/nu/OpenFeature.Providers.AzureAppConfig.svg)](https://badge.fury.io/nu/OpenFeature.Providers.AzureAppConfig)

The Azure App Configuration provider allows you to connect to your Azure App Configuration instance for feature flag management.

## .NET SDK usage

### Requirements

-   OpenFeature SDK v2.3.0 or later
-   Supported target frameworks:
    -   .NET 8.0
    -   .NET 9.0
    -   .NET Standard 2.0
    -   .NET Framework 4.6.2

### Install dependencies

The first things we will do is install the **OpenFeature SDK** and the **Azure App Configuration provider**.

#### .NET CLI

```shell
dotnet add package OpenFeature.Providers.AzureAppConfig
```

#### Package Manager

```shell
NuGet\Install-Package OpenFeature.Providers.AzureAppConfig
```

#### Package Reference

```xml
<PackageReference Include="OpenFeature.Providers.AzureAppConfig" />
```

## Overview

This provider allows you to use Azure App Configuration feature flags with OpenFeature. It supports:

-   **Feature Flags**: Simple Azure App Configuration feature flags with variants
-   **Boolean Values Only**: This provider focuses exclusively on boolean feature flags
-   **Variant Support**: Optional support for named variants (defaults to basic enabled/disabled)

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
    Label = "Production"
};

var provider = new AzureAppConfigProvider(connectionString, options);
```

### Using with Evaluation Context

The evaluation context is not used in the current implementation but is available for future enhancements:

```csharp
var context = EvaluationContext.Builder()
    .Set("userId", "user123")
    .Build();

var isEnabled = await client.GetBooleanValueAsync("my-feature", false, context);
```

## Feature Flag Support

The provider supports simple Azure App Configuration feature flags with optional variants:

### Basic Feature Flag

A simple feature flag without variants (uses basic enabled/disabled):

```json
{
    "id": "my-feature",
    "enabled": true
}
```

### Feature Flag with Variants

A feature flag with explicit On/Off variants:

```json
{
    "id": "my-feature",
    "enabled": true,
    "variants": [
        {
            "name": "Off",
            "configuration_value": false
        },
        {
            "name": "On",
            "configuration_value": true
        }
    ]
}
```

When enabled, the provider will:

1. Look for an "On" variant if variants are defined
2. Return the variant's `configuration_value`
3. Default to `true` if no "On" variant is found or no variants are defined

When disabled, the provider always returns `false`.

## Configuration Options

| Option              | Description                    | Default                   |
| ------------------- | ------------------------------ | ------------------------- |
| `FeatureFlagPrefix` | Prefix for feature flag keys   | `.appconfig.featureflag/` |
| `Label`             | Label filter for feature flags | `null`                    |

## Supported Data Types

-   **Boolean**: Feature flags only - other data types will return `ErrorType.TypeMismatch`

## Error Handling

The provider handles common Azure App Configuration errors:

-   **404 Not Found**: Returns `ErrorType.FlagNotFound`
-   **Authentication Errors**: Returns `ErrorType.General`
-   **Network Errors**: Returns `ErrorType.General`
-   **Type Mismatch**: Returns `ErrorType.TypeMismatch` when parsing fails

## Best Practices

1. **Connection Management**: Reuse the `ConfigurationClient` instance across multiple provider instances
2. **Caching**: Consider implementing caching for frequently accessed feature flags
3. **Error Handling**: Always provide sensible default values for feature flags
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

## Contributing

This project is part of the [OpenFeature .NET SDK Contributions](https://github.com/open-feature/dotnet-sdk-contrib) repository. For development and contribution guidelines, please see the main repository's [CONTRIBUTING.md](../../CONTRIBUTING.md).

## License

Apache 2.0 - See [LICENSE](../../LICENSE) for more information.
