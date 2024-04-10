# Flipt .NET Provider

An OpenFeature Provider which enables the use of the Flipt Server-Side SDK.

# .NET SDK usage

## Install dependencies

We will first install the **flipt provider**.

#### .NET CLI
```shell
dotnet add package OpenFeature.Contrib.Providers.Flipt
```
#### Package Manager

```shell
NuGet\Install-Package OpenFeature.Contrib.Providers.Flipt
```
#### Package Reference

```xml
<PackageReference Include="OpenFeature.Contrib.Providers.Flipt" />
```
#### Paket CLI

```shell
paket add OpenFeature.Contrib.Providers.Flagd
```


## Using the Flipt Provider with the OpenFeature SDK

Using the Flip provider assumes that you have installed and running Flipt service. Instructions on how to do this can be found in the [official documentation](https://docs.flipt.io/self-hosted/overview)

The following example shows how to use the Flipt provider with the OpenFeature SDK.

*appsettings.json*
```json
{
    "OpenFeature": {
        "Provider": {
            "Flipt": {
                "ServiceUri": "http://localhost:9000",
                "Namespace": "default",
                "TargetingKey": "UserId",
                "RequestIdKey": "RequestId",
                "UseBooleanEvaluation": false
            }
        }
    }
}
```

*Program.cs*

```csharp
using OpenFeature.Contrib.Providers.Flipt;

var builder = WebApplication.CreateBuilder(args);

// Get provider config from configuration
var config = builder.Configuration.GetSection("OpenFeature:Provider:Flipt").Get<FliptProviderConfiguration>();

/* Or create config directly:
var config = new FliptProviderConfiguration
{
    ServiceUri = new Uri("http://localhost:9000"),
    Namespace = "default",
    TargetingKey = "UserId",
    RequestIdKey = "RequestId",
    UseBooleanEvaluation = false
};*/

// Create an instance of Flipt provider
var fliptProvider = new FliptProvider(config);

// Set the Flipt Provider as the provider for the OpenFeature SDK
await OpenFeature.Api.Instance.SetProviderAsync(fliptProvider);

// Create OpenFeature client with current provider
var client = OpenFeature.Api.Instance.GetClient("my-app");

// Resolve boolean flag value
var isTestFlagEnabled = await client.GetBooleanValue("test-flag", false, null);

System.Console.WriteLine($"Test flag enabled: {isTestFlagEnabled}");
```

## Boolean flag evaluation
To perform Boolean flag evaluation, such as `GetBooleanValue` or `GetBooleanDetail` you can utilize [Boolean Evaluation](https://docs.flipt.io/reference/evaluation/boolean-evaluation) or [Variant Evaluation](https://docs.flipt.io/reference/evaluation/variant-evaluation) APIs.  

The preferred method can be selected by configuring the `FliptProviderConfiguration.UseBooleanEvaluation` parameter.  

Note that when using the Variant Evaluation API, the value of the variant **must** be a boolean, represented as either `"true"` or `"false"`.

## Using EvaluationContext

Flipt has the concept of [Context](https://docs.flipt.io/concepts#context) where you can evaluate a flag based on properties. The OpenFeature SDK has the concept of an [EvaluationContext](https://openfeature.dev/specification/sections/evaluation-context) which is a dictionary of string keys and values. The Flipt provider will map the EvaluationContext to a Flipt Context.

Since Flipt Context is a simple array of objects like `[{"key": "value"}]`, the provider only supports primitive types such as `number`, `string`, `date-time` and `boolean`. Complex types like `list` or `structure` will be ingnored.

For example context:
```csharp
var context = EvaluationContext.Builder()
    .Set("role", "user")
    .Set("age", 22)
    .Set("privileged", true)
    .Set("created", new DateTime(2011, 8, 23))
    .Build();
```
Transforms into:
```json
{
    "context": [
        {
            "key": "role",
            "value": "user"
        },
        {
            "value": "age",
            "key": "22"
        },
        {
            "value": "privileged",
            "key": "true"
        },
        {
            "key": "created",
            "value": "2011-08-23T00:00:00.0000000"
        }
    ]
}
```

### Set entityId
To pass the [`entityId`](https://docs.flipt.io/concepts#entities) you need to specify a key `FliptProviderConfiguration.TargetingKey`. The value in `EvaluationContext` assigned to this key will be used as the `entityId` request parameter and will not be mapped into context. The value **must** be of string type.  

For example:
```csharp
var config = new FliptProviderConfiguration
{
    ServiceUri = new Uri("http://localhost:9000"),
    Namespace = "default",
    TargetingKey = "UserId"
};
var context = EvaluationContext
    .Builder()
    .Set(config.TargetingKey, "some-user-id")
    .Build();
var isTestFlagEnabled = await client.GetBooleanValue("test-flag", false, context);
```
Request:
```json
{
    "context": [],
    "entity_id": "some-user-id",
    "flag_key": "test-flag",
    "namespace_key": "default",
    "request_id": ""
}
```

### Set requestId
To pass the `requestId` you need to specify a key `FliptProviderConfiguration.RequestIdKey`. The value in `EvaluationContext` assigned to this key will be used as the `entityId` request parameter and will not be mapped into context. The value **must** be of string type.  

If the key is not specified in the configuration, or the context does not contain a value for this key, then `Activity.Current.Id` will be used if it present.

For example:
```csharp
var config = new FliptProviderConfiguration
{
    ServiceUri = new Uri("http://localhost:9000"),
    Namespace = "default",
    TargetingKey = "UserId"
};
var context = EvaluationContext
    .Builder()
    .Set(config.TargetingKey, "some-user-id");
var isTestFlagEnabled = await client.GetBooleanValue("test-flag", false, context);
```
Request:
```json
{
    "context": [],
    "entity_id": "some-user-id",
    "flag_key": "test-flag",
    "namespace_key": "default",
    "request_id": ""
}
```




