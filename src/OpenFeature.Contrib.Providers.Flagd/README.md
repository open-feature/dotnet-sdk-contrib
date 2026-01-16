# flagd .NET Provider

> **⚠️ DEPRECATED**: This package will be renamed to `OpenFeature.Providers.Flagd` (removing the "Contrib" suffix) in a future release. The current package name will be deprecated in favour of the new package name.

The flagd Flag provider allows you to connect to your flagd instance.

## Requirements

- open-feature/dotnet-sdk v2.0.0 > v3.0.0

## Install dependencies

We will first install the **OpenFeature SDK** and the **flagd provider**.

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

            var val = client.GetBooleanValueAsync("myBoolFlag", false, null);

            // Print the value of the 'myBoolFlag' feature flag
            System.Console.WriteLine(val.Result.ToString());
        }
    }
}
```

## Using the flagd Provider with the OpenFeature SDK and Dependency Injection

You can also use the flagd Provider with the OpenFeature SDK and Dependency Injection. The following example shows how to do this using Microsoft.Extensions.DependencyInjection:

Before you start, make sure you have the `OpenFeature.Hosting` NuGet package installed:

```shell
dotnet add package OpenFeature.Hosting
```

Or with Package Manager:

```shell
NuGet\Install-Package OpenFeature.Hosting
```

Now you can set up Dependency Injection with OpenFeature and the flagd Provider in your `Program.cs` file. When not specifying any configuration options, the flagd Provider will use the default values for the variables as described below.

```csharp
using OpenFeature;
using OpenFeature.DependencyInjection.Providers.Flagd;

namespace OpenFeatureTestApp
{
    class Hello {
        static void Main(string[] args) {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddOpenFeature(config =>
            {
                config.AddHostedFeatureLifecycle()
                    .AddFlagdProvider();
            });

            var app = builder.Build();

            // ... ommitted for brevity
        }
    }
}
```

You can override the default configuration options by specifying properties on the `FlagdProviderOptions` on the `AddFlagdProvider` method.

```csharp
using OpenFeature;
using OpenFeature.DependencyInjection.Providers.Flagd;

namespace OpenFeatureTestApp
{
    class Hello {
        static void Main(string[] args) {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddOpenFeature(config =>
            {
                config.AddHostedFeatureLifecycle()
                    .AddFlagdProvider(o =>
                    {
                        o.Host = builder.Configuration["FlagdProviderOptions:Host"];
                        o.Port = int.Parse(builder.Configuration["FlagdProviderOptions:Port"] ?? "8013");

                        // other configurations can be set here
                    });
            });

            var app = builder.Build();

            // ... ommitted for brevity
        }
    }
}
```

### Configuring the FlagdProvider

The URI of the flagd server to which the `flagd Provider` connects to can either be passed directly to the constructor, or be configured using the following environment variables:

| Option name                  | Environment variable name      | Type    | Default   | Values          |
| ---------------------------- | ------------------------------ | ------- | --------- | --------------- |
| host                         | FLAGD_HOST                     | string  | localhost |                 |
| port                         | FLAGD_PORT                     | number  | 8013      |                 |
| tls                          | FLAGD_TLS                      | boolean | false     |                 |
| tls certPath                 | FLAGD_SERVER_CERT_PATH         | string  |           |                 |
| unix socket path             | FLAGD_SOCKET_PATH              | string  |           |                 |
| Caching                      | FLAGD_CACHE                    | string  |           | lru             |
| Maximum cache size           | FLAGD_MAX_CACHE_SIZE           | number  | 10        |                 |
| Maximum event stream retries | FLAGD_MAX_EVENT_STREAM_RETRIES | number  | 3         |                 |
| Resolver type                | FLAGD_RESOLVER                 | string  | rpc       | rpc, in-process |
| Source selector              | FLAGD_SOURCE_SELECTOR          | string  |           |                 |
| Logger                       | n/a                            | n/a     |           |                 |

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

## In-process resolver type

The flagd provider also supports the [in-process provider mode](https://flagd.dev/reference/specifications/in-process-providers/),
which is activated by setting the `FLAGD_RESOLVER` env var to `IN_PROCESS`.
In this mode, the provider will connect to a service implementing the [flagd.sync.v1 interface](https://github.com/open-feature/flagd-schemas/blob/main/protobuf/flagd/sync/v1/sync.proto)
and subscribe to a feature flag configuration determined by the `FLAGD_SOURCE_SELECTOR`.
After an initial retrieval of the desired flag configuration, the in-process provider will keep the latest known state in memory,
meaning that no requests need to be sent over the network for resolving flags that are part of the flag configuration.
Updates to the flag configuration will be sent via the grpc event stream established between the in-process provider and
the service implementing the `flagd.sync.v1` interface (e.g. [flagd-proxy](https://github.com/open-feature/flagd/tree/main/flagd-proxy)).

Example of using the in-process provider mode:

```csharp
using OpenFeature.Contrib.Providers.Flagd;

namespace OpenFeatureTestApp
{
    class Hello {
        static void Main(string[] args) {

            var flagdConfig = new FlagdConfigBuilder()
                // set the host and port for flagd-proxy
                .WithHost("localhost")
                .WithPort("8015")
                // set the resolver type to 'IN_PROCESS'
                .WithResolverType(ResolverType.IN_PROCESS)
                // provide the flag source selector, e.g. the name of a Flags custom resource which is watched by the flagd-proxy
                .WithSourceSelector("core.openfeature.dev/flags/sample-flags")
                .Build();

            var flagdProvider = new FlagdProvider(flagdConfig);

            // Set the flagdProvider as the provider for the OpenFeature SDK
            OpenFeature.Api.Instance.SetProvider(flagdProvider);

            var client = OpenFeature.Api.Instance.GetClient("my-app");

            var val = client.GetBooleanValueAsync("myBoolFlag", false, null);

            // Print the value of the 'myBoolFlag' feature flag
            System.Console.WriteLine(val.Result.ToString());
        }
    }
}
```

By default the in-process provider will attempt to validate the flag configurations against the [Flags](https://flagd.dev/schema/v0/flags.json) and [targeting](https://flagd.dev/schema/v0/targeting.json) schemas. If validation fails a warning log will be generated. You must configure a logger using the FlagdConfigBuilder. The in-process provider uses the Microsoft.Extensions.Logging abstractions.

```csharp
var logger = loggerFactory.CreateLogger<Program>();
var flagdConfig = new FlagdConfigBuilder()
    .WithLogger(logger)
    .Build();
```
