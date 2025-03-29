# OpenFeature.Contrib.Providers.Ofrep

An OpenFeature provider implementation for the OpenFeature REST Evaluation Protocol (OFREP). This provider allows you to
evaluate feature flags through a REST API that follows the OFREP specification.

## Features

- Full implementation of the OpenFeature REST Evaluation Protocol
- Support for all standard flag types (boolean, string, integer, double, object)
- Advanced caching with ETag support to minimize API calls

## Installation

Add the package to your project:

### .NET Cli
```shell
dotnet add package OpenFeature.Contrib.Providers.Ofrep
```
### Package Manager

```shell
NuGet\Install-Package OpenFeature.Contrib.Providers.Ofrep
```
### Package Reference

```xml
<PackageReference Include="OpenFeature.Contrib.Providers.Ofrep" />
```
### Packet cli

```shell
packet add OpenFeature.Contrib.Providers.Ofrep
```

### Cake

```shell
// Install OpenFeature.Contrib.Providers.Ofrep as a Cake Addin
#addin nuget:?package=OpenFeature.Contrib.Providers.Ofrep

// Install OpenFeature.Contrib.Providers.Ofrep as a Cake Tool
#tool nuget:?package=OpenFeature.Contrib.Providers.Ofrep


## Basic Usage

```csharp
using OpenFeature;
using OpenFeature.Contrib.Providers.Ofrep;
using OpenFeature.Contrib.Providers.Ofrep.Configuration;

// Configure the provider
var config = new OfrepConfiguration
{
    BaseUrl = "https://zconfig.company.com/"
};

// Create and register the provider
var provider = new OfrepProvider(config);
Api.Instance.SetProvider(provider);

// Use feature flags
var client = Api.Instance.GetClient();

// Boolean flag
var boolFlag = await client.GetBooleanValue("my-flag", false);

// String flag
var stringFlag = await client.GetStringValue("greeting", "Hello");

// Integer flag
var intFlag = await client.GetIntegerValue("max-retries", 3);

// Double flag
var doubleFlag = await client.GetDoubleValue("sample-rate", 0.1);
```

## Advanced Configuration

```csharp
// Advanced configuration
var config = new OfrepConfiguration
{
    BaseUrl = "https://feature-flags.example.com",
    Timeout = TimeSpan.FromSeconds(10),
    Headers = new Dictionary<string, string>
    {
        ["Custom-Header"] = "value",
        ["Api-Version"] = "v1"
    },
    AuthorizationHeader = "Bearer your-auth-token",
    CacheDuration = TimeSpan.FromSeconds(30),
    MaxCacheSize = 2000,
    EnableAbsoluteExpiration = true
};

// Create and register the provider
var provider = new OfrepProvider(config);
Api.Instance.SetProvider(provider);
```

## Configuration Options

The `OfrepConfiguration` class supports the following options:

- `BaseUrl` (required): The base URL for the OFREP API endpoint
- `Timeout` (optional): HTTP client timeout
- `Headers` (optional): Additional HTTP headers to include in requests
- `AuthorizationHeader` (optional): Authorization header value
- `CacheDuration` (optional): Cache duration for evaluation responses (default: 1000ms)
- `MaxCacheSize` (optional): Maximum number of items to cache (default: 1000)
- `EnableAbsoluteExpiration` (optional): Whether to use absolute expiration (default: false)

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.