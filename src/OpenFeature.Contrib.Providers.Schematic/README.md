# Schematic .NET Provider

The Schematic provider allows you to connect to your Schematic instance through the OpenFeature SDK

# .Net SDK usage

## Requirements

- open-feature/dotnet-sdk v1.5.0 > v2.0.0

## Install dependencies

The first things we will do is install the **Open Feature SDK** and the **Schematic OpenFeature provider**.

### .NET Cli
```shell
dotnet add package OpenFeature.Contrib.Providers.Schematic
```
### Package Manager

```shell
NuGet\Install-Package OpenFeature.Contrib.Providers.Schematic
```
### Package Reference

```xml
<PackageReference Include="OpenFeature.Contrib.Providers.Schematic" />
```
### Paket cli

```shell
paket add OpenFeature.Contrib.Providers.Schematic
```

### Cake

```shell
// Install OpenFeature.Contrib.Providers.Schematic as a Cake Addin
#addin nuget:?package=OpenFeature.Contrib.Providers.Schematic

// Install OpenFeature.Contrib.Providers.Schematic as a Cake Tool
#tool nuget:?package=OpenFeature.Contrib.Providers.Schematic
```

## Using the Schematic Provider with the OpenFeature SDK

To use Schematic as an OpenFeature provider, define your provider and Schematic settings.

```csharp
using OpenFeature;
using OpenFeature.Contrib.Providers.Schematic;
using System;

var schematicProvider = new SchematicFeatureProvider("your-api-key");

// Set the schematicProvider as the provider for the OpenFeature SDK
await OpenFeature.Api.Instance.SetProviderAsync(flagsmithProvider);

// Get an OpenFeature client
var client = OpenFeature.Api.Instance.GetClient("my-app");

// Set company and/or user context
var context = EvaluationContext.Builder()
    .Set("company", new Dictionary<string, string> { { "id", "your-company-id" } })
    .Set("user", new Dictionary<string, string> { { "id", "your-user-id" } })
    .Build();

// Evaluate a flag
var val = await client.GetBooleanValueAsync("your-flag-key", false, context);

// Print the value of the 'your-flag-key' feature flag
Console.WriteLine(val);
```

You can also provide additional configuration options to the provider to manage caching behavior, offline mode, and other capabilities:

```csharp
using OpenFeature;
using OpenFeature.Contrib.Providers.Schematic;
using System;

var options = new ClientOptions
{
    Offline = true,
    FlagDefaults = new Dictionary<string, bool>
    {
        { "some-flag-key", true }
    }
};

var schematicProvider = new SchematicFeatureProvider("your-api-key", options);

// Set the schematicProvider as the provider for the OpenFeature SDK
await OpenFeature.Api.Instance.SetProviderAsync(flagsmithProvider);

// Get an OpenFeature client
var client = OpenFeature.Api.Instance.GetClient("my-app");

// Set company and/or user context
var context = EvaluationContext.Builder()
    .Set("company", new Dictionary<string, string> { { "id", "your-company-id" } })
    .Set("user", new Dictionary<string, string> { { "id", "your-user-id" } })
    .Build();

// Evaluate a flag
var val = await client.GetBooleanValueAsync("your-flag-key", false, context);

// Print the value of the 'your-flag-key' feature flag
Console.WriteLine(val);
```
