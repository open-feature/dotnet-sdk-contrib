# flagd .NET Provider

The flagd Flag provider allows you to connect to your flagd instance.

## Requirements

- open-feature/dotnet-sdk v2.0.0 > v3.0.0

## Install dependencies

We will first install the **OpenFeature SDK** and the **flagd provider**.

### .NET Cli

```shell
dotnet add package OpenFeature.Providers.Flagd
```

### Package Manager

```shell
NuGet\Install-Package OpenFeature.Providers.Flagd
```

### Package Reference

```xml
<PackageReference Include="OpenFeature.Providers.Flagd" />
```

### Packet cli

```shell
paket add OpenFeature.Providers.Flagd
```

### Cake

```shell
// Install OpenFeature.Providers.Flagd as a Cake Addin
#addin nuget:?package=OpenFeature.Providers.Flagd

// Install OpenFeature.Providers.Flagd as a Cake Tool
#tool nuget:?package=OpenFeature.Providers.Flagd
```

## Using the flagd Provider with the OpenFeature SDK

This example assumes that the flagd server is running locally
For example, you can start flagd with the following example configuration:

```shell
flagd start --uri https://raw.githubusercontent.com/open-feature/flagd/main/config/samples/example_flags.json
```

When the flagd service is running, you can use the SDK with the flagd Provider as in the following example console application:

```csharp
using OpenFeature.Providers.Flagd;

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

| Option name                  | Environment variable name                                  | Type    | Default                                 | Values                  |
| ---------------------------- | ---------------------------------------------------------- | ------- | --------------------------------------- | ----------------------- |
| host                         | FLAGD_HOST                                                 | string  | localhost                               |                         |
| port                         | FLAGD_SYNC_PORT (in-process, priority), FLAGD_PORT, default | number  | 8013 (8015 when resolver is in-process) |                         |
| tls                          | FLAGD_TLS                                                  | boolean | false                                   |                         |
| tls certPath                 | FLAGD_SERVER_CERT_PATH                                     | string  |                                         |                         |
| unix socket path             | FLAGD_SOCKET_PATH                                          | string  |                                         |                         |
| Caching                      | FLAGD_CACHE                                                | string  |                                         | lru                     |
| Maximum cache size           | FLAGD_MAX_CACHE_SIZE                                       | number  | 10                                      |                         |
| Maximum event stream retries | FLAGD_MAX_EVENT_STREAM_RETRIES                             | number  | 3                                       |                         |
| Resolver type                | FLAGD_RESOLVER                                             | string  | rpc                                     | rpc, in-process, file   |
| Source selector              | FLAGD_SOURCE_SELECTOR                                      | string  |                                         |                         |
| Offline flag source path     | FLAGD_OFFLINE_FLAG_SOURCE_PATH                             | string  |                                         |                         |
| Hash file change detection   | FLAGD_HASH_FILE_CHANGE                                     | boolean | false                                   |                         |
| Offline poll interval        | FLAGD_OFFLINE_POLL_MS                                      | number  | 5000                                    |                         |
| Deadline                     | FLAGD_DEADLINE_MS                                          | number  | 300000                                  |                         |
| Retry backoff (ms)           | FLAGD_RETRY_BACKOFF_MS                                     | number  | 1000                                    |                         |
| Retry backoff max (ms)       | FLAGD_RETRY_BACKOFF_MAX_MS                                 | number  | 12000                                   |                         |
| Logger                       | n/a                                                        | n/a     |                                         |                         |

> **Note:** The `retryBackoffMs` and `retryBackoffMaxMs` settings control the exponential backoff behavior for stream reconnection in the RPC and in-process resolvers. The backoff starts at `retryBackoffMs` and doubles on each retry, up to a maximum of `retryBackoffMaxMs`. Per the [flagd provider specification](https://flagd.dev/reference/specifications/providers/#stream-reconnection), the default maximum is 12000ms (12 seconds).

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
using OpenFeature.Providers.Flagd;

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

### Configuring retry backoff

You can customize the stream reconnection backoff behavior for both RPC and in-process resolvers:

```csharp
var flagdConfig = new FlagdConfigBuilder()
    .WithRetryBackoffMs(500)           // Initial backoff of 500ms
    .WithRetryBackoffMaxMs(10000)      // Maximum backoff of 10 seconds
    .Build();

var flagdProvider = new FlagdProvider(flagdConfig);
```

Or via environment variables:

```shell
export FLAGD_RETRY_BACKOFF_MS=500
export FLAGD_RETRY_BACKOFF_MAX_MS=10000
```

## File resolver type

The flagd provider supports a **file-based resolver mode**, which reads flag definitions from a local JSON file.
This is useful for local development, testing, or air-gapped environments where flags are distributed as files
(e.g., via ConfigMaps, volume mounts, or file sync).

The file resolver is activated by setting the `FLAGD_RESOLVER` environment variable to `file` and providing the
path to the flag definition file via `FLAGD_OFFLINE_FLAG_SOURCE_PATH`:

```shell
export FLAGD_RESOLVER=file
export FLAGD_OFFLINE_FLAG_SOURCE_PATH=/etc/flags/my-flags.json
```

Or by configuring the provider programmatically:

```csharp
using OpenFeature.Providers.Flagd;

var flagdConfig = new FlagdConfigBuilder()
    .WithResolverType(ResolverType.FILE)
    .WithOfflineFlagSourcePath("/etc/flags/my-flags.json")
    .Build();

var flagdProvider = new FlagdProvider(flagdConfig);
OpenFeature.Api.Instance.SetProvider(flagdProvider);
```

The file resolver watches for changes to the flag file and automatically reloads the configuration when changes are
detected. By default, it polls the file's modification time and size at a regular interval. Modification-time polling
is used by default because native file system event APIs are unreliable in the environments this resolver typically
targets (e.g. Linux overlay/NFS mounts and bind-mounted ConfigMaps), where events are frequently missed.

### Hash-based file change detection

In some environments, the file's modification time may not be updated reliably (e.g. certain network or virtual file
systems). For these cases, you can opt in to content-based change detection using MurmurHash:

```csharp
var flagdConfig = new FlagdConfigBuilder()
    .WithResolverType(ResolverType.FILE)
    .WithOfflineFlagSourcePath("/etc/flags/my-flags.json")
    .WithUseHashFileChangeDetection(true)
    .Build();
```

Or via environment variable:

```shell
export FLAGD_HASH_FILE_CHANGE=true
```

When enabled, the provider polls the file at a regular interval and compares content hashes rather than the file's
modification time. This is more robust against unreliable timestamps but has a slightly higher I/O cost due to
periodic full-file reads.

### Tuning the file watcher intervals

Two timing-related settings can be tuned for the file resolver. Both are expressed in **milliseconds**:

- **Offline poll interval** (`FLAGD_OFFLINE_POLL_MS`, default `5000` / 5 seconds) — how often the watcher polls the
  file for changes. Applies to both the modification-time watcher (default) and the hash-based watcher.
- **Deadline** (`FLAGD_DEADLINE_MS`, default `300000` / 5 minutes) — the maximum time to wait during initialization
  for the flag file to become available before timing out. Applies regardless of the watcher mode.

```shell
export FLAGD_OFFLINE_POLL_MS=30000
export FLAGD_DEADLINE_MS=60000
```

Or programmatically (values in milliseconds):

```csharp
var flagdConfig = new FlagdConfigBuilder()
    .WithResolverType(ResolverType.FILE)
    .WithOfflineFlagSourcePath("/etc/flags/my-flags.json")
    .WithUseHashFileChangeDetection(true)
    .WithOfflinePollIntervalMs(30000)
    .WithDeadlineMs(60000)
    .Build();
```
