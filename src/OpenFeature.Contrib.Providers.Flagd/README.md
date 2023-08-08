# flagd Feature Flag .NET Provider

The flagd Flag provider allows you to connect to your flagd instance.  

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

## Using the flagd Provider with the OpenFeature SDK

This example assumes that the flagd server is running locally
For example, you can start flagd with the following example configuration:

```shell
flagd start --uri https://raw.githubusercontent.com/open-feature/flagd/main/config/samples/example_flags.json
```

When the flagd service is running, you can use the SDK with the flagd Provider as in the following example console application:

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

### Configuring the FlagdProvider

The URI of the flagd server to which the `flagd Provider` connects to can either be passed directly to the constructor, or be configured using the following environment variables:

| Option name                  | Environment variable name      | Type    | Default   | Values        |
|------------------------------|--------------------------------|---------|-----------| ------------- |
| host                         | FLAGD_HOST                     | string  | localhost |               |
| port                         | FLAGD_PORT                     | number  | 8013      |               |
| tls                          | FLAGD_TLS                      | boolean | false     |               |
| tls certPath                 | FLAGD_SERVER_CERT_PATH         | string  |           |               |
| unix socket path             | FLAGD_SOCKET_PATH              | string  |           |               |
| Caching                      | FLAGD_CACHE                    | string  |           |     LRU       |
| Maximum cache size           | FLAGD_MAX_CACHE_SIZE           | number  | 10        |               |
| Maximum event stream retries | FLAGD_MAX_EVENT_STREAM_RETRIES | number  | 3         |               |

Note that if `FLAGD_SOCKET_PATH` is set, this value takes precedence, and the other variables (`FLAGD_HOST`, `FLAGD_PORT`, `FLAGD_TLS`, `FLAGD_SERVER_CERT_PATH`) are disregarded.

Note that if you are on `NET462` through `NET48` as the target framework for your project, you are required to enable TLS and supply a certificate path as part of your configuration.  This is a limitation Microsoft has [documented](https://learn.microsoft.com/en-us/aspnet/core/grpc/netstandard?view=aspnetcore-7.0#net-framework).

If you rely on the environment variables listed above, you can use the empty constructor which then configures the provider accordingly:

```csharp
var flagdProvider = new FlagdProvider();
```

Alternatively, if you would like to pass the URI directly, you can initialise it as follows:

```csharp
// either communicate with Flagd over HTTP ...
var flagdProvider = new FlagdProvider(new Uri("http://localhost:8013"));

// ... or use the unix:// prefix if the provider should communicate via a unix socket
var unixFlagdProvider = new FlagdProvider(new Uri("unix://socket.tmp"));  
```

