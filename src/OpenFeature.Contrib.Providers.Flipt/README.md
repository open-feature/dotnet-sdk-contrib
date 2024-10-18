# Flipt .NET Provider

The flipt provider allows you to connect to your Flipt instance through the OpenFeature SDK

# .Net SDK usage

## Requirements

- open-feature/dotnet-sdk v1.5.0 > v2.0.0

## Install dependencies

The first thing we will do is install the **OpenFeature SDK** and the **Flipt Feature Flag provider**.

### .NET Cli

```shell
dotnet add package OpenFeature.Contrib.Providers.Flipt
```

### Package Manager

```shell
NuGet\Install-Package OpenFeature.Contrib.Providers.Flipt
```

### Package Reference

```xml
<PackageReference Include="OpenFeature.Contrib.Providers.Flipt" />
```

### Packet cli

```shell
packet add OpenFeature.Contrib.Providers.Flipt
```

### Cake

```shell
// Install OpenFeature.Contrib.Providers.Flipt as a Cake Addin
#addin nuget:?package=OpenFeature.Contrib.Providers.Flipt

// Install OpenFeature.Contrib.Providers.Flipt as a Cake Tool
#tool nuget:?package=OpenFeature.Contrib.Providers.Flipt
```

## Using the Flipt Provider with the OpenFeature SDK

To create a Flipt provider, you should define the provider and pass in the instance `url` (required), `defaultNamespace` and
`token`.

```csharp
using OpenFeature.Contrib.Providers.Flipt;
using OpenFeature.Model;

// namespace and clientToken is optional
var featureProvider = new FliptProvider("http://localhost:8080", "default-namespace", "client-token");

// Set the featureProvider as the provider for the OpenFeature SDK
await OpenFeature.Api.Instance.SetProviderAsync(featureProvider);

// Get an OpenFeature client
var client = OpenFeature.Api.Instance.GetClient();

// Optional: set EntityId and updated context
var context = EvaluationContext.Builder()
    .SetTargetingKey("flipt EntityId")
    .Set("extra-data-1", "extra-data-1-value")
    .Build();

// Evaluate a flag
var val = await client.GetBooleanValueAsync("myBoolFlag", false, context);

// Print the value of the 'myBoolFlag' feature flag
Console.WriteLine(val);
```

# Contribution

## Code setup

Since the official [flipt-csharp](https://github.com/flipt-io/flipt-server-sdks/tree/main/flipt-csharp) only supports
dotnet 8.0, it was not utilized in this provider as OpenFeature aims to support a bigger range of dotnet versions.

### Rest Client using OpenAPI

To work around this incompatibility, the openapi specification
of [Flipt](https://github.com/flipt-io/flipt/blob/main/openapi.yaml) was
used to generate a REST client using [nswag](https://github.com/RicoSuter/NSwag).

## Updating the REST Client

To generate or update the Flipt REST client **manually**, follow these steps:

_The **Rest client is generated automatically during build time** using the committed `openapi.yaml` file and is saved
in the `/obj/` folder_

### 1. Download the OpenAPI Specification

First, download the latest `openapi.yaml` file from the Flipt GitHub repository. This can be done manually or by using a
command like `curl` in the `/src/OpenFeature.Contrib.Providers.Flipt/`:

```
curl https://raw.githubusercontent.com/flipt-io/flipt/refs/heads/main/openapi.yaml -o openapi.yaml
```

### 2. Generate the Client Code

With the `openapi.yml` file in your working directory, run the following `nswag` command to generate the REST client
code. Make sure to correct the command as shown below:

```
nswag openapi2csclient /className:FliptRestClient /namespace:Flipt.Rest /input:"openapi.yaml" /output:"./Flipt.Rest.Client.cs" /GenerateExceptionClasses:true /OperationGenerationMode:SingleClientFromPathSegments /JsonLibrary:SystemTextJson /GenerateOptionalParameters:true /GenerateDefaultValues:true /GenerateResponseClasses:true /GenerateClientInterfaces:true /GenerateClientClasses:true /GenerateDtoTypes:true /ExceptionClass:FliptRestException /GenerateNativeRecords:true /UseBaseUrl:false /GenerateBaseUrlProperty:false 
```

#### Notes

- Ensure the `nswag` CLI tool is correctly installed and accessible from your terminal or command prompt.
- The command provided generates a C# client for interacting with the Flipt API, leveraging the System.Text.Json library
  for JSON serialization/deserialization.
- The generated client will include features such as exception classes, optional parameters, default values, response
  classes, client interfaces, DTO types, and native records, according to the specified options.
- This process assumes you're working in a directory that contains the `openapi.yml` file and will generate the
  `Flipt.Rest.Client.cs` file in the same directory.

## Know issues and limitations

-In `BuildClient()` method
from https://github.com/open-feature/dotnet-sdk-contrib/blob/204144f6df0dacf46e6d52d34dd6b5a223a853f4/src/OpenFeature.Contrib.Providers.Flipt/ClientWrapper/FliptClientWrapper.cs#L41-L47
a new `HttpClient` is created. In the future, it would be better to allow passing of `HttpConnectionFactory` to avoid
problems regarding socket starvation

