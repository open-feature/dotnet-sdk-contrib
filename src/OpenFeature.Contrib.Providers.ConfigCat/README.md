# ConfigCat Feature Flag .NET Provider

The ConfigCat Flag provider allows you to connect to your ConfigCat instance.

# .Net SDK usage

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
using OpenFeature.Contrib.Providers.ConfigCat;

namespace OpenFeatureTestApp
{
    class Hello {
        static void Main(string[] args) {
            var configCatProvider = new ConfigCatProvider("#YOUR-SDK-KEY#");

            // Set the configCatProvider as the provider for the OpenFeature SDK
            OpenFeature.Api.Instance.SetProvider(configCatProvider);

            var client = OpenFeature.Api.Instance.GetClient();

            var val = client.GetBooleanValue("isMyAwesomeFeatureEnabled", false);

            if(isMyAwesomeFeatureEnabled)
            {
                doTheNewThing();
            }
            else
            {
                doTheOldThing();
            }
        }
    }
}
```

### Customizing the ConfigCat Provider

The ConfigCat provider can be customized by passing a `ConfigCatClientOptions` object to the constructor.

```csharp
var configCatOptions = new ConfigCatClientOptions
{
    PollingMode = PollingModes.ManualPoll;
    Logger = new ConsoleLogger(LogLevel.Info);
};

var configCatProvider = new ConfigCatProvider("#YOUR-SDK-KEY#", configCatOptions);
```

For a full list of options see the [ConfigCat documentation](https://configcat.com/docs/sdk-reference/dotnet/).

## EvaluationContext and ConfigCat User relationship

ConfigCat has the concept of Users where you can evaluate a flag based on properties. The OpenFeature SDK has the concept of an EvaluationContext which is a dictionary of string keys and values. The ConfigCat provider will map the EvaluationContext to a ConfigCat User.

The ConfigCat User has a few pre-defined parameters that can be used to evaluate a flag. These are:

| Parameter | Description                                                                                                                     |
|-----------|---------------------------------------------------------------------------------------------------------------------------------|
| `Id`      | *REQUIRED*. Unique identifier of a user in your application. Can be any `string` value, even an email address.                  |
| `Email`   | Optional parameter for easier targeting rule definitions.                                                                       |
| `Country` | Optional parameter for easier targeting rule definitions.                                                                       |
| `Custom`  | Optional dictionary for custom attributes of a user for advanced targeting rule definitions. E.g. User role, Subscription type. |

Since EvaluationContext is a simple dictionary, the provider will try to match the keys to the ConfigCat User parameters following the table below in a case-insensitive manner.

| EvaluationContext Key | ConfigCat User Parameter |
|-----------------------|--------------------------|
| `id`                  | `Id`                     |
| `identifier`          | `Id`                     |
| `email`               | `Email`                  |
| `country`             | `Country`                |