# Flagsmith Feature Flag .NET Provider

The Flagsmith Flag provider allows you to connect to your Flagsmith instance.  

# .Net SDK usage

## Install dependencies

The first things we will do is install the **Open Feature SDK** and the **Flagsmith Feature Flag provider**.

### .NET Cli
```shell
dotnet add package OpenFeature.Contrib.Providers.Flagsmith
```
### Package Manager

```shell
NuGet\Install-Package OpenFeature.Contrib.Providers.Flagsmith
```
### Package Reference

```xml
<PackageReference Include="OpenFeature.Contrib.Providers.Flagsmith" />
```
### Packet cli

```shell
packet add OpenFeature.Contrib.Providers.Flagsmith
```

### Cake

```shell
// Install OpenFeature.Contrib.Providers.Flagsmith as a Cake Addin
#addin nuget:?package=OpenFeature.Contrib.Providers.Flagsmith

// Install OpenFeature.Contrib.Providers.Flagsmith as a Cake Tool
#tool nuget:?package=OpenFeature.Contrib.Providers.Flagsmith
```

## Using the Flagsmith Provider with the OpenFeature SDK

To create a Flagmith provider you should define provider and Flagsmith settings.

```csharp
using OpenFeature.Contrib.Providers.Flagd;

namespace OpenFeatureTestApp
{
    class Hello {
        static void Main(string[] args) {

            // Additional configs for provider
            var providerConfig = new FlagsmithProviderConfiguration();

            //Flagsmith client configuration
            var flagsmithConfig = new FlagsmithConfiguration
            {
                ApiUrl = "https://edge.api.flagsmith.com/api/v1/",
                EnvironmentKey = string.Empty,
                EnableClientSideEvaluation = false,
                EnvironmentRefreshIntervalSeconds = 60,
                EnableAnalytics = false,
                Retries = 1
            };
            var flagsmithProvider = new FlagsmithProvider(providerConfig, flagsmithConfig);\

            // Set the flagsmithProvider as the provider for the OpenFeature SDK
            OpenFeature.Api.Instance.SetProvider(flagsmithProvider);

            var client = OpenFeature.Api.Instance.GetClient("my-app");

            var val = client.GetBooleanValue("myBoolFlag", false, null);

            // Print the value of the 'myBoolFlag' feature flag
            System.Console.WriteLine(val.Result.ToString());
        }
    }
}
```

You also can create Flagsmith provider using ```HttpClient``` or precreated ```FlagsmithClient```

```csharp
using var httpClient = new HttpClient();
var flagsmithProvider = new FlagsmithProvider(providerConfig, config, httpClient);
```
```csharp
using var flagsmithClient = new FlagsmithClient(flagsmithOptions);
var flagsmithProvider = new FlagsmithProvider(providerConfig, flagsmithClient);
```
### Configuring the FlagsmithProvider

To configure FlagsmithConfiguration just use [an example](https://github.com/Flagsmith/flagsmith-dotnet-client/tree/main/Example) from Flagsmith GitHub.
For FlagsmithProviderConfiguration you can configure next parameters using custom implementation or just ```FlagsmithProviderConfiguration```:
```csharp
public interface IFlagsmithProviderConfiguration
{
    /// <summary>
    /// Key that will be used as identity for Flagsmith requests.
    /// </summary>
    public string TargetingKey { get; }

    /// <summary>
    /// Determines whether to resolve a feature value as a boolean or use
    /// the isFeatureEnabled as the flag itself. These values will be false
    /// and true respectively.
    /// Default: false
    /// </summary>
    public bool UsingBooleanConfigValue { get; }
}
```


