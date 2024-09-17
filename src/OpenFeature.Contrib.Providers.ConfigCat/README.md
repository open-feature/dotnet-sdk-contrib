# ConfigCat Feature Flag .NET Provider

The ConfigCat Flag provider allows you to connect to your ConfigCat instance.

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
The ConfigCat provider translates these evaluation contexts to ConfigCat [User Objects](https://configcat.com/docs/targeting/user-object/).

The ConfigCat User Object has a few pre-defined attributes that can be used to evaluate a flag. These are:

| Attribute    | Description                                                                                                    |
|--------------|----------------------------------------------------------------------------------------------------------------|
| `Identifier` | *REQUIRED*. Unique identifier of a user in your application. Can be any `string` value, even an email address. |
| `Email`      | The email address of the user.                                                                                 |
| `Country`    | The country of the user.                                                                                       |

Since `EvaluationContext` is a simple dictionary, the provider will try to match the keys to ConfigCat user attributes following the table below in a case-insensitive manner.

| EvaluationContext Key | ConfigCat User Attribute |
|-----------------------|--------------------------|
| `id`                  | `Identifier`             |
| `identifier`          | `Identifier`             |
| `email`               | `Email`                  |
| `country`             | `Country`                |
| Any other             | `Custom`                 |