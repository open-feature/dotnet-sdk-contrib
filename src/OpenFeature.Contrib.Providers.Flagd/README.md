# Flagd Feature Flag .NET Provider

The Flagd Flag provider allows you to connect to your Flagd instance.  

# .Net SDK usage

## Install dependencies

The first things we will do is install the **Open Feature SDK** and the **GO Feature Flag provider**.

### .NET Cli
```shell
dotnet add package OpenFeature.Contrib.Providers.Flagd
```
### Package Manager

```shell
NuGet\Install-Package OpenFeature.Contrib.Providers.Flagd
```
### Package Reference

```xml
<PackageReference Include="OpenFeature.Contrib.Providers.Flagd" />
```
### Packet cli

```shell
paket add OpenFeature.Contrib.Providers.Flagd
```

### Cake

```shell
// Install OpenFeature.Contrib.Providers.Flagd as a Cake Addin
#addin nuget:?package=OpenFeature.Contrib.Providers.Flagd

// Install OpenFeature.Contrib.Providers.Flagd as a Cake Tool
#tool nuget:?package=OpenFeature.Contrib.Providers.Flagd
```

## Using the FlagdProvider with the OpenFeature SDK

This example assumes that the flagd server is running locally
For example, you can start flagd with the following example configuration:

```shell
flagd start --uri https://raw.githubusercontent.com/open-feature/flagd/main/config/samples/example_flags.json
```

When the flagd service is running, you can use the SDK with the FlagdProvider as in the following example console application:

```csharp
using OpenFeature.Contrib.Providers.Flagd;

namespace OpenFeatureTestApp
{
    class Hello {
        static void Main(string[] args) {
            var flagdProvider = new FlagdProvider(new Uri("http://localhost:8013"));

            // Set the flagdProvider as the provider for the OpenFeature SDK
            OpenFeature.Api.Instance.SetProvider(flagdProvider);

            var client = OpenFeature.Api.Instance.GetClient("my-app");

            var val = client.GetBooleanValue("myBoolFlag", false, null);

            // Print the value of the 'myBoolFlag' feature flag
            System.Console.WriteLine(val.Result.ToString());
        }
    }
}
```
