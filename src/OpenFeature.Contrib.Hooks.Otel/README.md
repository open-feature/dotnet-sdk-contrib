# OpenFeature OpenTelemetry hook for .NET

> **⚠️ DEPRECATED**: This library has been deprecated and is no longer maintained. OpenTelemetry hooks have been moved to the main OpenFeature .NET SDK starting with version 2.7.0.

## Migration Required

**This package is no longer maintained.** The OpenTelemetry functionality has been integrated directly into the OpenFeature .NET SDK as of version 2.7.0.

### How to Migrate

1. **Remove this package**:

    ```xml
    <!-- Remove this line from your .csproj -->
    <PackageReference Include="OpenFeature.Contrib.Hooks.Otel" Version="..." />
    ```

2. **Update to OpenFeature SDK v2.7.0+**:

    ```xml
    <PackageReference Include="OpenFeature" Version="2.7.0" />
    ```

3. **Update your code**:

    **Before (deprecated):**

    ```csharp
    using OpenFeature.Contrib.Hooks.Otel;

    // Add hooks
    OpenFeature.Api.Instance.AddHooks(new TracingHook());
    OpenFeature.Api.Instance.AddHooks(new MetricsHook());
    ```

    **After (SDK v2.7.0+):**

    ```csharp
    using OpenFeature.Hooks;

    // Add hooks with updated names
    OpenFeature.Api.Instance.AddHooks(new TraceEnricherHook());
    OpenFeature.Api.Instance.AddHooks(new MetricsHook());
    ```

### Key Changes

-   `TracingHook` → `TraceEnricherHook`
-   `MetricsHook` remains the same name but uses `OpenFeature.Hooks` namespace
-   Improved performance and compliance with latest OpenTelemetry standards
-   Enhanced metrics with better dimensional data

### Benefits of Migration

-   **Active Maintenance**: Regular updates and bug fixes in the main SDK
-   **Better Performance**: Native implementation with improved efficiency
-   **Enhanced Metrics**: More detailed metrics with better dimensional data
-   **Latest Standards**: Compliance with the latest OpenTelemetry semantic conventions
-   **Reduced Dependencies**: One less package to manage

## Documentation

For complete documentation on using OpenTelemetry hooks with OpenFeature, please refer to the [OpenFeature .NET SDK documentation](https://github.com/open-feature/dotnet-sdk).

## License

Apache 2.0 - See [LICENSE](./../../LICENSE) for more information.
