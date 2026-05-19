# Unleash .NET Provider

The Unleash provider allows you to use [Unleash](https://www.getunleash.io/) with the OpenFeature .NET SDK.

# .Net SDK usage

## Requirements

- open-feature/dotnet-sdk v2.x
- Unleash .NET SDK v6.x

## Install dependencies

The first thing we will do is install the **OpenFeature SDK** and the **Unleash Feature Flag provider**.

### .NET Cli

```shell
dotnet add package OpenFeature.Contrib.Providers.Unleash
```

### Package Manager

```shell
NuGet\Install-Package OpenFeature.Contrib.Providers.Unleash
```

### Package Reference

```xml
<PackageReference Include="OpenFeature.Contrib.Providers.Unleash" />
```

### Packet cli

```shell
paket add OpenFeature.Contrib.Providers.Unleash
```

### Cake

```shell
// Install OpenFeature.Contrib.Providers.Unleash as a Cake Addin
#addin nuget:?package=OpenFeature.Contrib.Providers.Unleash

// Install OpenFeature.Contrib.Providers.Unleash as a Cake Tool
#tool nuget:?package=OpenFeature.Contrib.Providers.Unleash
```

## Using the Unleash Provider with the OpenFeature SDK

```csharp
using OpenFeature;
using OpenFeature.Contrib.Providers.Unleash;
using Unleash;

var settings = new UnleashSettings
{
    AppName = "my-app",
    UnleashApi = new Uri("http://localhost:4242/api/"),
    CustomHttpHeaders = new Dictionary<string, string>
    {
        { "Authorization", "*:development.your-api-token" }
    }
};

var provider = new UnleashProvider(settings);

// Set the provider for the OpenFeature SDK
await Api.Instance.SetProviderAsync(provider);

// Get an OpenFeature client
var client = Api.Instance.GetClient();

// Boolean evaluation (uses IsEnabled)
var enabled = await client.GetBooleanValueAsync("my-feature", false);

// String evaluation (uses variant payload)
var value = await client.GetStringValueAsync("my-variant-flag", "default");

// Integer evaluation (parses variant payload)
var count = await client.GetIntegerValueAsync("my-int-flag", 0);

// Double evaluation (parses variant payload)
var rate = await client.GetDoubleValueAsync("my-double-flag", 0.0);
```

## EvaluationContext and Unleash Context relationship

The provider maps OpenFeature `EvaluationContext` fields to `UnleashContext`:

| EvaluationContext Key | Unleash Context Field |
|-----------------------|------------------------|
| `TargetingKey`        | `UserId`               |
| `sessionId`           | `SessionId`            |
| `remoteAddress`       | `RemoteAddress`        |
| `environment`         | `Environment`          |
| `appName`             | `AppName`              |
| `currentTime`         | `CurrentTime`          |
| All other keys        | `Properties`           |

## Variant payload type metadata

When evaluating variants (string, integer, double, structure), the provider exposes the Unleash payload `type` field (e.g., `"string"`, `"number"`, `"json"`, `"csv"`) as `payload-type` in the resolution details flag metadata.

## Events

The provider emits `ProviderConfigurationChanged` events when Unleash fires `TogglesUpdatedEvent` (i.e., when toggle state is refreshed from the server).

## Known issues and limitations

- The provider does not accept an external `IUnleash` instance because lifecycle events (`ReadyEvent`, `ErrorEvent`, `TogglesUpdatedEvent`) can only be subscribed during client construction.
