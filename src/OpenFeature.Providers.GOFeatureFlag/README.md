# GO Feature Flag - OpenFeature .NET provider

[![nuget](https://img.shields.io/nuget/v/OpenFeature.Providers.GOFeatureFlag?color=blue&style=flat-square&logo=nuget)](https://www.nuget.org/packages/OpenFeature.Providers.GOFeatureFlag)


> [!WARNING]
> This version of the provider requires to use GO Feature Flag relay-proxy `v1.45.0` or above.
> If you have an older version of the relay-proxy, please use the version the package `OpenFeature.Contrib.Providers.GOFeatureFlag` in version `0.2.1` to get the right provider.

This is the official OpenFeature .NET provider for accessing your feature flags with GO Feature Flag.

In conjuction with the [OpenFeature SDK](https://openfeature.dev/docs/reference/concepts/provider) you will be able to
evaluate your feature flags in your **.NET** applications.

For documentation related to flags management in GO Feature Flag, refer to
the [GO Feature Flag documentation website](https://gofeatureflag.org/docs).

### Functionalities:

- Manage the integration of the OpenFeature .NET SDK and GO Feature Flag relay-proxy.
- 2 types of evaluations available:
    - **In process**: fetch the flag configuration from the GO Feature Flag relay-proxy API and evaluate the flags
      directly in the provider.
    - **Remote**: Call the GO Feature Flag relay-proxy for each flag evaluation.
- Collect and send evaluation data to the GO Feature Flag relay-proxy for statistics and monitoring purposes.
- Support the OpenFeature [tracking API](https://openfeature.dev/docs/reference/concepts/tracking/) to associate metrics
  or KPIs with feature flag evaluation contexts.

## Dependency Setup

### .NET Cli

```shell
dotnet add package OpenFeature.Providers.GOFeatureFlag
```

### Package Manager

```shell
NuGet\Install-Package OpenFeature.Providers.GOFeatureFlag
```

### Package Reference

```xml
<PackageReference Include="OpenFeature.Providers.GOFeatureFlag" />
```

### Packet cli

```shell
paket add OpenFeature.Providers.GOFeatureFlag
```

### Cake

```shell
// Install OpenFeature.Providers.GOFeatureFlag as a Cake Addin
#addin nuget:?package=OpenFeature.Providers.GOFeatureFlag

// Install OpenFeature.Providers.GOFeatureFlag as a Cake Tool
#tool nuget:?package=OpenFeature.Providers.GOFeatureFlag
```[GoFeatureFlagProviderOptions.cs](GoFeatureFlagProviderOptions.cs)

## Getting started

### Initialize the provider

GO Feature Flag provider needs to be created and then set in the global OpenFeatureAPI.

The only required option to create a `GoFeatureFlagProvider` is the endpoint to your GO Feature Flag relay-proxy
instance.

```csharp
using OpenFeature.Providers.GOFeatureFlag;

var options = new GoFeatureFlagProviderOptions { Endpoint = "https://gofeatureflag.example.com" };
var provider = new GoFeatureFlagProvider(options);

// Associate the provider with the OpenFeature API
await Api.Instance.SetProviderAsync("client_test", provider);

// Create a client to perform feature flag evaluations
var client = Api.Instance.GetClient("client_test");

// targetingKey is mandatory for each evaluation
var evaluationContext = EvaluationContext.Builder()
        .SetTargetingKey("d45e303a-38c2-11ed-a261-0242ac120002")
        .Set("email", "john.doe@gofeatureflag.org")
        .Build();

// Example of a boolean flag evaluation
var myFeatureFlag = await client.GetBooleanDetailsAsync("my-feature-flag", false, evaluationContext);
```

The evaluation context is the way for the client to specify contextual data that GO Feature Flag uses to evaluate the
feature flags, it allows to define rules on the flag.

The `targetingKey` is mandatory for GO Feature Flag in order to evaluate the feature flag, it could be the id of a user,
a session ID or anything you find relevant to use as identifier during the evaluation.

### Configure the provider

You can configure the provider with several options to customize its behavior. The following options are available:

| name                              | mandatory | Description                                                                                                                                                                                                                                                                                                                                                                     |
|-----------------------------------|-----------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **`Endpoint`**                    | `true`    | endpoint contains the DNS of your GO Feature Flag relay proxy _(ex: https://mydomain.com/gofeatureflagproxy/)_                                                                                                                                                                                                                                                                  |
| **`EvaluationType`**              | `false`   | evaluationType is the type of evaluation you want to use.<ul><li>If you want to have a local evaluation, you should use InProcess.</li><li>If you want to have an evaluation on the relay-proxy directly, you should use Remote.</li></ul>Default: InProcess<br/>                                                                                                               |
| **`Timeout`**                     | `false`   | timeout in millisecond we are waiting when calling the relay proxy API. _(default: `10000`)_                                                                                                                                                                                                                                                                                    |
| **`ApiKey`**                      | `false`   | If the relay proxy is configured to authenticate the requests, you should provide an API Key to the provider. Please ask the administrator of the relay proxy to provide an API Key. _(default: null)_                                                                                                                                                                          |
| **`FlushIntervalMs`**             | `false`   | interval time we publish statistics collection data to the proxy. The parameter is used only if the cache is enabled, otherwise the collection of the data is done directly when calling the evaluation API. default: `1000` ms                                                                                                                                                 |
| **`MaxPendingEvents`**            | `false`   | max pending events aggregated before publishing for collection data to the proxy. When event is added while events collection is full, event is omitted. _(default: `10000`)_                                                                                                                                                                                                   |
| **`DisableDataCollection`**       | `false`   | set to true if you don't want to collect the usage of flags retrieved in the cache. _(default: `false`)_                                                                                                                                                                                                                                                                        |
| **`ExporterMetadata`**            | `false`   | exporterMetadata is the metadata we send to the GO Feature Flag relay proxy when we report the evaluation data usage.                                                                                                                                                                                                                                                           |
| **`EvaluationFlagList`**          | `false`   | If you are using in process evaluation, by default we will load in memory all the flags available in the relay proxy. If you want to limit the number of flags loaded in memory, you can use this parameter. By setting this parameter, you will only load the flags available in the list. <p>If null or empty, all the flags available in the relay proxy will be loaded.</p> |
| **`FlagChangePollingIntervalMs`** | `false`   | interval time we poll the proxy to check if the configuration has changed. It is used for the in process evaluation to check if we should refresh our internal cache. default: `120000`                                                                                                                                                                                         |

### Evaluate a feature flag

The OpenFeature client is used to retrieve values for the current `EvaluationContext`. For example, retrieving a boolean
value for the flag **"my-flag"**:

```csharp
var client = Api.Instance.GetClient("client_test");
var myFeatureFlag = await client.GetBooleanValueAsync("my-feature-flag", false, evaluationContext);
```

GO Feature Flag supports different all OpenFeature supported types of feature flags, it means that you can use all the
accessor directly

```csharp
// Boolean
client.GetBooleanValueAsync("my-flag", false, evaluationContext);

// String
client.GetStringValueAsync("my-flag", "default", evaluationContext);

// Integer
client.GetIntegerValueAsync("my-flag", 1, evaluationContext);

// Double
client.GetDoubleValueAsync("my-flag", 1.1, evaluationContext);

// Object
client.GetObjectValueAsync("my-flag", new Value("any value you want"), evaluationContext);
```

## How it works

### In process evaluation

When the provider is configured to use in process evaluation, it will fetch the flag configuration from the GO Feature
Flag relay-proxy API and evaluate the flags directly in the provider.

The evaluation is done inside the provider using a webassembly module that is compiled from the GO Feature Flag source
code.
The `wasm` module is used to evaluate the flags, and the source code is available in
the [thomaspoignant/go-feature-flag](https://github.com/thomaspoignant/go-feature-flag/tree/main/wasm) repository.

The provider will call the GO Feature Flag relay-proxy API to fetch the flag configuration and then evaluate the flags
using the `wasm` module.

### Remote evaluation

When the provider is configured to use remote evaluation, it will call the GO Feature Flag relay-proxy for each flag
evaluation.

It will perform an HTTP request to the GO Feature Flag relay-proxy API with the flag name and the evaluation context for
each flag evaluation.

## ‼️ .NET Framework Compatibility

To be able to use the GO Feature Flag provider using the In Process mode, you need to use **.NET Core SDK 3.0 SDK or
later**.
If you are using an older version of the .NET Framework, you will only be able to use the remote evaluation mode.
