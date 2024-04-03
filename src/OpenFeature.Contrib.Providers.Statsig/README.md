# Statsig Feature Flag .NET Provider

The Statsig Flag provider allows you to connect to Statsig. Please note this is a minimal implementation - only `ResolveBooleanValue` is implemented.

# .Net SDK usage

## Install dependencies

The first things we will do is install the **Open Feature SDK** and the **Statsig Feature Flag provider**.

### .NET Cli
```shell
dotnet add package OpenFeature.Contrib.Provider.Statsig
```
### Package Manager

```shell
NuGet\Install-Package OpenFeature.Contrib.Provider.Statsig
```
### Package Reference

```xml
<PackageReference Include=" OpenFeature.Contrib.Provider.Statsig" />
```
### Packet cli

```shell
paket add OpenFeature.Contrib.Provider.Statsig
```

### Cake

```shell
// Install OpenFeature.Contrib.Provider.Statsig as a Cake Addin
#addin nuget:?package= OpenFeature.Contrib.Provider.Statsig

// Install OpenFeature.Contrib.Provider.Statsig as a Cake Tool
#tool nuget:?package= OpenFeature.Contrib.Provider.Statsig
```

## Using the Statsig Provider with the OpenFeature SDK

The following example shows how to use the Statsig provider with the OpenFeature SDK.

```csharp
using OpenFeature;
using OpenFeature.Contrib.Provider.Statsig;
using System;

StatsigProvider statsigProvider = new StatsigProvider("#YOUR-SDK-KEY#");

// Set the statsigProvider as the provider for the OpenFeature SDK
await Api.Instance.SetProviderAsync(statsigProvider);

IFeatureClient client = OpenFeature.Api.Instance.GetClient();

bool isMyAwesomeFeatureEnabled = await client.GetBooleanValue("isMyAwesomeFeatureEnabled", false);

if (isMyAwesomeFeatureEnabled)
{
    Console.WriteLine("New Feature enabled!");
}

```

### Customizing the Statsig Provider

The Statsig provider can be customized by passing a `Action<StatsigServerOptions>` object to the constructor.

```csharp
var statsigProvider = new StatsigProvider("#YOUR-SDK-KEY#", options => options.LocalMode = true);
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
- Only `ResolveBooleanValue` implemented for now

- Gate BooleanEvaluation with default value true cannot fallback to true.
  https://github.com/statsig-io/dotnet-sdk/issues/33
