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

## Configuration Options

The `OfrepOptions` class supports the following options:

-   `BaseUrl` (required): The base URL for the OFREP API endpoint
-   `Timeout` (optional): HTTP client timeout
-   `Headers` (optional): Additional HTTP headers to include in requests

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.
