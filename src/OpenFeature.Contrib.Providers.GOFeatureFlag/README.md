# GO Feature Flag .NET Provider

GO Feature Flag provider allows you to connect to your GO Feature Flag instance.  

[GO Feature Flag](https://gofeatureflag.org) believes in simplicity and offers a simple and lightweight solution to use feature flags.  
Our focus is to avoid any complex infrastructure work to use GO Feature Flag.

This is a complete feature flagging solution with the possibility to target only a group of users, use any types of flags, store your configuration in various location and advanced rollout functionality. You can also collect usage data of your flags and be notified of configuration changes.

# .Net SDK usage

## Install dependencies

The first things we will do is install the **Open Feature SDK** and the **GO Feature Flag provider**.

### .NET Cli
```shell
dotnet add package OpenFeature.Contrib.Providers.GOFeatureFlag
```
### Package Manager

```shell
NuGet\Install-Package OpenFeature.Contrib.Providers.GOFeatureFlag
```
### Package Reference

```xml
<PackageReference Include="OpenFeature.Contrib.Providers.GOFeatureFlag" />
```
### Packet cli

```shell
paket add OpenFeature.Contrib.Providers.GOFeatureFlag
```

### Cake

```shell
// Install OpenFeature.Contrib.Providers.GOFeatureFlag as a Cake Addin
#addin nuget:?package=OpenFeature.Contrib.Providers.GOFeatureFlag

// Install OpenFeature.Contrib.Providers.GOFeatureFlag as a Cake Tool
#tool nuget:?package=OpenFeature.Contrib.Providers.GOFeatureFlag
```

## Initialize your Open Feature client

To evaluate the flags you need to have an Open Feature configured in you app.
This code block shows you how you can create a client that you can use in your application.

```csharp
using OpenFeature;
using OpenFeature.Contrib.Providers.GOFeatureFlag;

// ...

var goFeatureFlagProvider = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
{
    Endpoint = "http://localhost:1031/",
    Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
});
Api.Instance.SetProvider(goFeatureFlagProvider);
var client = Api.Instance.GetClient("my-app");
```

## Evaluate your flag

This code block explain how you can create an `EvaluationContext` and use it to evaluate your flag.


> In this example we are evaluating a `boolean` flag, but other types are available.
> 
> **Refer to the [Open Feature documentation](https://docs.openfeature.dev/docs/reference/concepts/evaluation-api#basic-evaluation) to know more about it.**

```csharp
// Context of your flag evaluation.
// With GO Feature Flag you MUST have a targetingKey that is a unique identifier of the user.
var userContext = EvaluationContext.Builder()
    .Set("targetingKey", "1d1b9238-2591-4a47-94cf-d2bc080892f1") // user unique identifier (mandatory)
    .Set("firstname", "john")
    .Set("lastname", "doe")
    .Set("email", "john.doe@gofeatureflag.org")
    .Set("admin", true) // this field is used in the targeting rule of the flag "flag-only-for-admin"
    .Set("anonymous", false)
    .Build();

var adminFlag = await client.GetBooleanValue("flag-only-for-admin", false, userContext);
if (adminFlag) {
   // flag "flag-only-for-admin" is true for the user
} else {
  // flag "flag-only-for-admin" is false for the user
}
```
