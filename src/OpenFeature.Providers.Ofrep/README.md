# OpenFeature.Providers.Ofrep

An OpenFeature provider implementation for the OpenFeature REST Evaluation Protocol (OFREP). This provider allows you to
evaluate feature flags through a REST API that follows the OFREP specification.

## Features

-   Full implementation of the OpenFeature REST Evaluation Protocol
-   Support for all standard flag types (boolean, string, integer, double, object)

## Installation

Add the package to your project:

### .NET Cli

```shell
dotnet add package OpenFeature.Providers.Ofrep
```

## Basic Usage

```csharp
using OpenFeature;
using OpenFeature.Providers.Ofrep;
using OpenFeature.Providers.Ofrep.Configuration;

// Configure the provider
var config = new OfrepOptions("https://zconfig.company.com/");

// Create and register the provider
var provider = new OfrepProvider(config);
await Api.Instance.SetProviderAsync(provider);

// Use feature flags
var client = Api.Instance.GetClient();

// Boolean flag
var boolFlag = await client.GetBooleanValueAsync("my-flag", false);

// String flag
var stringFlag = await client.GetStringValueAsync("greeting", "Hello");

// Integer flag
var intFlag = await client.GetIntegerValueAsync("max-retries", 3);

// Double flag
var doubleFlag = await client.GetDoubleValueAsync("sample-rate", 0.1);
```

## Advanced Configuration

```csharp
// Advanced configuration
var config = new OfrepOptions("https://feature-flags.example.com")
{
    Timeout = TimeSpan.FromSeconds(10),
    Headers = new Dictionary<string, string>
    {
        ["Custom-Header"] = "value",
        ["Api-Version"] = "v1"
    }
};

// Create and register the provider
var provider = new OfrepProvider(config);
await Api.Instance.SetProviderAsync(provider);
```

### TimeProvider Support

For testing and scenarios where you need to control time-related behavior (such as rate limiting), you can provide a custom `TimeProvider`:

```csharp
// Using a custom TimeProvider for testing
var customTimeProvider = new FakeTimeProvider(); // From Microsoft.Extensions.TimeProvider.Testing
var provider = new OfrepProvider(config, customTimeProvider);
```

When using dependency injection, the provider will automatically use any registered `TimeProvider` from the service container.

## Configuration Options

The `OfrepOptions` class supports the following options:

-   `BaseUrl` (required): The base URL for the OFREP API endpoint
-   `Timeout` (optional): HTTP client timeout. The default value is 10 seconds.
-   `Headers` (optional): Additional HTTP headers to include in requests

## Environment Variable Configuration

The OFREP provider supports configuration via environment variables, enabling zero-code configuration for containerized deployments and CI/CD pipelines.

### Supported Environment Variables

| Variable | Required | Description | Example |
|----------|----------|-------------|---------|
| `OFREP_ENDPOINT` | Yes | The OFREP server endpoint URL | `http://localhost:8080` |
| `OFREP_HEADERS` | No | HTTP headers in `Key=Value,Key2=Value2` format | `Authorization=Bearer token,X-Api-Key=abc123` |
| `OFREP_TIMEOUT_MS` | No | Request timeout in milliseconds (default: 10000) | `5000` |

### Usage with Environment Variables

```csharp
using OpenFeature;
using OpenFeature.Providers.Ofrep;

// Create provider using environment variables (no explicit configuration needed)
var provider = new OfrepProvider();
await Api.Instance.SetProviderAsync(provider);
```

Or explicitly load from environment:

```csharp
using OpenFeature.Providers.Ofrep.Configuration;

// Explicitly create options from environment variables
var options = OfrepOptions.FromEnvironment();
var provider = new OfrepProvider(options);
```

### Header Format and URL Encoding

The `OFREP_HEADERS` environment variable uses a simple `Key=Value` format separated by commas. URL encoding is supported for compatibility with systems that encode the entire value, but note that commas are used as header separators and cannot be included in values.

**Examples:**

```bash
# Simple headers
export OFREP_HEADERS="Authorization=Bearer token123,Content-Type=application/json"

# Header value containing equals sign (e.g., base64) - no encoding needed for additional = in value
export OFREP_HEADERS="Authorization=Bearer abc123=="

# URL-encoded equals sign in the value
export OFREP_HEADERS="X-Data=key%3Dvalue"

# Multiple headers with special characters
export OFREP_HEADERS="Authorization=Bearer token,X-Api-Key=abc123"
```

### Dependency Injection with IConfiguration

When using dependency injection, the provider can read configuration from `IConfiguration`, which supports environment variables via `AddEnvironmentVariables()`:

```csharp
// In Program.cs or Startup.cs
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddOpenFeature(featureBuilder =>
{
    featureBuilder.AddOfrepProvider(options =>
    {
        // Options will fall back to OFREP_ENDPOINT, OFREP_HEADERS, OFREP_TIMEOUT_MS
        // from IConfiguration (which includes environment variables)
    });
});
```

### Configuration Precedence

When multiple configuration sources are available, the following precedence order applies (highest to lowest):

1. **Programmatic configuration** (explicit `OfrepOptions` or `OfrepProviderOptions` values)
2. **IConfiguration** (when using DI, includes appsettings.json, user secrets, etc.)
3. **Environment variables** (direct `Environment.GetEnvironmentVariable` fallback)

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.
