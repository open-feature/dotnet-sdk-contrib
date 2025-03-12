# .NET Environment Variable Provider

This provider supports using the OpenFeature SDK to evaluate feature flags backed by environment variables.

## Installation

### .NET CLI

```shell
dotnet add package OpenFeature.Contrib.Providers.EnvVar
```

## Using the ConfigCat Provider with the OpenFeature SDK

The following example shows how to use the Environment Variable provider with the OpenFeature SDK.

```csharp
using System;
using OpenFeature;
using OpenFeature.Contrib.EnvVar;

// If you want to use a prefix for your environment variables, you can supply it in the constructor below.
// For example, if you all your feature flag environment variables will be prefixed with feature-flag- then 
// you would use:
// var envVarProvider = new EnvVarProvider("feature-flag-");
var envVarProvider = new EnvVarProvider();

// Set the Environment Variable provider as the provider for the OpenFeature SDK
await OpenFeature.Api.Instance.SetProviderAsync(envVarProvider);
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
