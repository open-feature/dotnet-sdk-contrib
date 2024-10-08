# Statsig Feature Flag .NET Provider

The Statsig Flag provider allows you to connect to Statsig. Please note this is a minimal implementation - only `ResolveBooleanValueAsync` is implemented.

# .Net SDK usage

## Requirements

- open-feature/dotnet-sdk v1.5.0 > v2.0.0

## Install dependencies

The first things we will do is install the **Open Feature SDK** and the **Statsig Feature Flag provider**.

### .NET Cli
```shell
dotnet add package OpenFeature.Contrib.Providers.Statsig
```
### Package Manager

```shell
NuGet\Install-Package OpenFeature.Contrib.Providers.Statsig
```
### Package Reference

```xml
<PackageReference Include=" OpenFeature.Contrib.Providers.Statsig" />
```
### Packet cli

```shell
paket add OpenFeature.Contrib.Providers.Statsig
```

### Cake

```shell
// Install OpenFeature.Contrib.Providers.Statsig as a Cake Addin
#addin nuget:?package= OpenFeature.Contrib.Providers.Statsig

// Install OpenFeature.Contrib.Providers.Statsig as a Cake Tool
#tool nuget:?package= OpenFeature.Contrib.Providers.Statsig
```

## Using the Statsig Provider with the OpenFeature SDK

The following example shows how to use the Statsig provider with the OpenFeature SDK.

```csharp
using OpenFeature;
using OpenFeature.Contrib.Providers.Statsig;
using System;

StatsigProvider statsigProvider = new StatsigProvider("#YOUR-SDK-KEY#");

// Set the statsigProvider as the provider for the OpenFeature SDK
await Api.Instance.SetProviderAsync(statsigProvider);

var eb = EvaluationContext.Builder();
eb.SetTargetingKey("john@doe.acme");

IFeatureClient client = Api.Instance.GetClient(context: eb.Build());

bool isMyAwesomeFeatureEnabled = await client.GetBooleanValueAsync("isMyAwesomeFeatureEnabled", false);

if (isMyAwesomeFeatureEnabled)
{
    Console.WriteLine("New Feature enabled!");
}

```

### Customizing the Statsig Provider

The Statsig provider can be customized by passing a `StatsigServerOptions` object to the constructor.

```csharp
var statsigProvider = new StatsigProvider("#YOUR-SDK-KEY#", new StatsigServerOptions() { LocalMode = true });
```

For a full list of options see the [Statsig documentation](https://docs.statsig.com/server/dotnetSDK#statsig-options).

## EvaluationContext and Statsig User relationship

Statsig has the concept of a [StatsigUser](https://docs.statsig.com/client/concepts/user) where you can evaluate a flag based on properties. The OpenFeature SDK has the concept of an EvaluationContext which is a dictionary of string keys and values. The Statsig provider will map the EvaluationContext to a StatsigUser.

The following parameters are mapped to the corresponding Statsig pre-defined parameters

| EvaluationContext Key | Statsig User Parameter    |
|-----------------------|---------------------------|
| `appVersion`          | `AppVersion`              |
| `country`             | `Country`                 |
| `email`               | `Email`                   |
| `ip`                  | `Ip`                      |
| `locale`              | `Locale`                  |
| `userAgent`           | `UserAgent`               |
| `privateAttributes`   | `PrivateAttributes`       |

## Known issues and limitations
- Only `ResolveBooleanValueAsync` implemented for now
