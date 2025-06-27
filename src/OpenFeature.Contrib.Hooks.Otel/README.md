# OpenFeature OpenTelemetry hook for .NET

> **⚠️ DEPRECATED**: This library is now deprecated. OpenTelemetry hooks have been moved to the main OpenFeature .NET SDK starting with version 2.7.0. Please migrate to the native hooks provided in the SDK.

## Migration Guide

As of OpenFeature .NET SDK version 2.7.0, OpenTelemetry hooks are now included natively in the SDK and this contrib library is no longer needed. The native hooks have also been updated to match the latest version of the OpenTelemetry semantic conventions. Follow these steps to migrate:

### 1. Update Dependencies

Remove this package:

```xml
<PackageReference Include="OpenFeature.Contrib.Hooks.Otel" Version="..." />
```

Update to the latest OpenFeature SDK:

```xml
<PackageReference Include="OpenFeature" Version="2.7.0" />
```

### 2. Update Using Statements

**Before:**

```csharp
using OpenFeature.Contrib.Hooks.Otel;
```

**After:**

```csharp
using OpenFeature.Hooks;
```

### 3. Update Hook Class Names

The hook classes have been renamed and improved:

| Old Class Name (Contrib) | New Class Name (SDK) | Purpose                                              |
| ------------------------ | -------------------- | ---------------------------------------------------- |
| `TracingHook`            | `TraceEnricherHook`  | Enriches traces with feature flag evaluation details |
| `MetricsHook`            | `MetricsHook`        | Collects metrics for feature flag evaluations        |

### 4. Update Your Code

**Before (using contrib library):**

```csharp
// Tracing
OpenFeature.Api.Instance.AddHooks(new TracingHook());

// Metrics
OpenFeature.Api.Instance.AddHooks(new MetricsHook());
```

**After (using native SDK hooks):**

```csharp
// Tracing - now called TraceEnricherHook
OpenFeature.Api.Instance.AddHooks(new TraceEnricherHook());

// Metrics - same name but from different namespace
OpenFeature.Api.Instance.AddHooks(new MetricsHook());
```

### 5. Updated Metrics

The native `MetricsHook` in the SDK provides enhanced metrics with improved dimensions:

| Metric Name                              | Description                     | Unit           | Dimensions                                  |
| ---------------------------------------- | ------------------------------- | -------------- | ------------------------------------------- |
| `feature_flag.evaluation_requests_total` | Number of evaluation requests   | `{request}`    | `key`, `provider_name`                      |
| `feature_flag.evaluation_success_total`  | Flag evaluation successes       | `{impression}` | `key`, `provider_name`, `reason`, `variant` |
| `feature_flag.evaluation_error_total`    | Flag evaluation errors          | `{impression}` | `key`, `provider_name`                      |
| `feature_flag.evaluation_active_count`   | Active flag evaluations counter | `{evaluation}` | `key`                                       |

### 6. Experimental Status

The hooks in the SDK are marked as **experimental** and may change in future versions. Monitor the [OpenFeature .NET SDK changelog](https://github.com/open-feature/dotnet-sdk/blob/main/CHANGELOG.md) for updates.

### Benefits of Migration

-   **Better Performance**: Native implementation with improved efficiency
-   **Enhanced Metrics**: More detailed metrics with better dimensional data
-   **Active Maintenance**: Regular updates and bug fixes in the main SDK
-   **Latest OpenTelemetry Standards**: Compliance with the latest semantic conventions
-   **Reduced Dependencies**: One less package to manage

## Requirements (Deprecated Library)

-   open-feature/dotnet-sdk v1.5.0 > v2.0.0

> **Note**: For new implementations, use OpenFeature .NET SDK v2.7.0+ with native hooks instead.

## Usage - Traces (Deprecated)

> **⚠️ DEPRECATED**: Use `TraceEnricherHook` from `OpenFeature.Hooks` namespace in the main SDK instead.

For this hook to function correctly a global `TracerProvider` must be set, an example of how to do this can be found below.

The `open telemetry hook` taps into the after and error methods of the hook lifecycle to write `events` and `attributes` to an existing `span`.
For this, an active span must be set in the `Tracer`, otherwise the hook will no-op.

### Example (Deprecated)

> **⚠️ DEPRECATED**: This example uses the deprecated contrib library. See the migration guide above for the new approach.

The following example demonstrates the use of the `OpenTelemetry hook` with the `OpenFeature dotnet-sdk`. The traces are sent to a `jaeger` OTLP collector running at `localhost:4317`.

```csharp
using OpenFeature.Contrib.Providers.Flagd;
using OpenFeature.Contrib.Hooks.Otel;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace OpenFeatureTestApp
{
    class Hello {
        static void Main(string[] args) {

			// set up the OpenTelemetry OTLP exporter
			var tracerProvider = Sdk.CreateTracerProviderBuilder()
                    .AddSource("my-tracer")
                    .ConfigureResource(r => r.AddService("jaeger-test"))
                    .AddOtlpExporter(o =>
                    {
                        o.ExportProcessorType = ExportProcessorType.Simple;
                    })
                    .Build();

			// add the Otel Hook to the OpenFeature instance
		    OpenFeature.Api.Instance.AddHooks(new TracingHook());

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

After running this example, you will be able to see the traces, including the events sent by the hook in your Jaeger UI:

![](./assets/otlp-success.png)

In case something went wrong during a feature flag evaluation, you will see an event containing error details in the span:

![](./assets/otlp-error.png)

## Usage - Metrics (Deprecated)

> **⚠️ DEPRECATED**: Use `MetricsHook` from `OpenFeature.Hooks` namespace in the main SDK instead.

For this hook to function correctly a global `MeterProvider` must be set.
`MetricsHook` performs metric collection by tapping into various hook stages.

Below are the metrics extracted by this hook and dimensions they carry:

| Metric key                             | Description                     | Unit         | Dimensions                          |
| -------------------------------------- | ------------------------------- | ------------ | ----------------------------------- |
| feature_flag.evaluation_requests_total | Number of evaluation requests   | {request}    | key, provider name                  |
| feature_flag.evaluation_success_total  | Flag evaluation successes       | {impression} | key, provider name, reason, variant |
| feature_flag.evaluation_error_total    | Flag evaluation errors          | Counter      | key, provider name                  |
| feature_flag.evaluation_active_count   | Active flag evaluations counter | Counter      | key                                 |

Consider the following code example for usage.

### Example (Deprecated)

> **⚠️ DEPRECATED**: This example uses the deprecated contrib library. See the migration guide above for the new approach.

The following example demonstrates the use of the `OpenTelemetry hook` with the `OpenFeature dotnet-sdk`. The metrics are sent to the `console`.

```csharp
using OpenFeature.Contrib.Providers.Flagd;
using OpenFeature;
using OpenFeature.Contrib.Hooks.Otel;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace OpenFeatureTestApp
{
    class Hello {
        static void Main(string[] args) {

            // set up the OpenTelemetry OTLP exporter
            var meterProvider = Sdk.CreateMeterProviderBuilder()
                    .AddMeter("OpenFeature.Contrib.Hooks.Otel")
                    .ConfigureResource(r => r.AddService("openfeature-test"))
                    .AddConsoleExporter()
                    .Build();

            // add the Otel Hook to the OpenFeature instance
            OpenFeature.Api.Instance.AddHooks(new MetricsHook());

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

After running this example, you should be able to see some metrics being generated into the console.

## License

Apache 2.0 - See [LICENSE](./../../LICENSE) for more information.
