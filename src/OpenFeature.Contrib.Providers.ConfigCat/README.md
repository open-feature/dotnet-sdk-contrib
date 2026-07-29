# ConfigCat Feature Flag .NET Provider

The ConfigCat provider allows you to use [ConfigCat](https://configcat.com) with the OpenFeature .NET SDK.

# .NET SDK usage

## Requirements

- open-feature/dotnet-sdk v1.5.0 > v2.0.0

## Install dependencies

The first things we will do is install the **Open Feature SDK** and the **ConfigCat Feature Flag provider**.

### .NET Cli
```shell
dotnet add package OpenFeature.Contrib.Providers.ConfigCat
```
### Package Manager

```shell
NuGet\Install-Package OpenFeature.Contrib.Providers.ConfigCat
```
### Package Reference

```xml
<PackageReference Include="OpenFeature.Contrib.Providers.ConfigCat" />
```
### Packet cli

```shell
paket add OpenFeature.Contrib.Providers.ConfigCat
```

### Cake

```shell
// Install OpenFeature.Contrib.Providers.ConfigCat as a Cake Addin
#addin nuget:?package=OpenFeature.Contrib.Providers.ConfigCat

// Install OpenFeature.Contrib.Providers.ConfigCat as a Cake Tool
#tool nuget:?package=OpenFeature.Contrib.Providers.ConfigCat
```

## Using the ConfigCat Provider with the OpenFeature SDK

The following example shows how to use the ConfigCat provider with the OpenFeature SDK.

```csharp
using System;
using ConfigCat.Client;
using OpenFeature.Contrib.ConfigCat;

var configCatProvider = new ConfigCatProvider("#YOUR-SDK-KEY#");

// Set the configCatProvider as the provider for the OpenFeature SDK
await OpenFeature.Api.Instance.SetProviderAsync(configCatProvider);

var client = OpenFeature.Api.Instance.GetClient();

var isAwesomeFeatureEnabled = await client.GetBooleanValueAsync("isAwesomeFeatureEnabled", false);
if (isAwesomeFeatureEnabled)
{
    doTheNewThing();
}
else
{
    doTheOldThing();
}
```

### Customizing the ConfigCat Provider

The ConfigCat provider can be customized by passing a callback setting up a `ConfigCatClientOptions` object to the constructor.

```csharp
Action<ConfigCat.Client.Configuration.ConfigCatClientOptions> configureConfigCatOptions = (options) =>
{
    options.PollingMode = PollingModes.LazyLoad(cacheTimeToLive: TimeSpan.FromSeconds(10));
    options.Logger = new ConsoleLogger(LogLevel.Info);
    // ...
};

var configCatProvider = new ConfigCatProvider("#YOUR-SDK-KEY#", configureConfigCatOptions);
```

For a full list of options see the [ConfigCat documentation](https://configcat.com/docs/sdk-reference/dotnet/).

### Cleaning up

On application shutdown, clean up the OpenFeature provider and the underlying ConfigCat client.

```csharp
await OpenFeature.Api.Instance.ShutdownAsync();
```

## EvaluationContext and ConfigCat User Object relationship

An <a href="https://openfeature.dev/docs/reference/concepts/evaluation-context" target="_blank">evaluation context</a> in the OpenFeature specification is a container for arbitrary contextual data that can be used as a basis for feature flag evaluation.

The ConfigCat provider translates these evaluation contexts to ConfigCat [User Objects](https://configcat.com/docs/targeting/user-object/), which have three predefined attributes and allow for additional custom attributes.

The following table shows how the attributes are mapped:

| EvaluationContext Key                | ConfigCat User Property |
| ------------------------------------ | ----------------------- |
| `targetingKey`                       | Identifier              |
| `id` (or `Id`, `ID`, etc.)           | Identifier              |
| `identifier` (or `Identifier`, etc.) | Identifier              |
| `email` (or `Email`, etc.)           | Email                   |
| `country` (or `Country`, etc.)       | Country                 |
| Any Other                            | Custom                  |

Remarks:
- If `targetingKey` is specified, it will be mapped to the `Identifier` property, regardless of other identifier attributes.
- If `targetingKey` is not specified, `id` or `identifier` will be mapped to the `Identifier` property, whichever occurs first. These keys are matched case-insensitively, so for example, `ID` or `Identifier` would also work.
- If no identifier attribute is specified, the `Identifier` property will be set to the fallback value `<n/a>`.
- The keys `email` and `country` are also matched case-insensitively. If the same key appears in multiple cases - e.g. `email` and `EMAIL` - the first occurrence will be mapped to the corresponding property.
- All of the above will also be included in the `Custom` dictionary, except for the exact keys `Identifier`, `Email`, and `Country`. This allows them to be referenced using their original names in feature flag rules.
- Other keys are mapped as custom user attributes with their values unchanged. (Although the ConfigCat SDK handles value conversion internally, it's recommended to use the type expected by the referencing feature flag rules. Read more [here](https://configcat.com/docs/sdk-reference/dotnet/#user-object-attribute-types).)
