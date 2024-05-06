# FeatureManagement .NET Provider
> **Note**  
This requires a new feature of the FeatureManagement system, Variants. This feature is still in preview and has not been fully released.

The FeatureManagement Provider allows you to use the FeatureManagement system as an OpenFeature Provider.

# .NET SDK Usage

## Install dependencies

### .NET Cli

```shell
dotnet add package OpenFeature.Contrib.Provider.FeatureManagement --version 0.0.1-preview
```

### Package Manager

```shell
NuGet\Install-Package OpenFeature.Contrib.Provider.FeatureManagement -Version 0.0.1-preview
```

### Package Reference

```xml
<PackageReference Include="OpenFeature.Contrib.Provider.FeatureManagement" Version="0.0.1-preview" />
```

### Paket CLI
```shell
paket add OpenFeature.Contrib.Provider.FeatureManagement --version 0.0.1-preview
```

### Cake

```shell
// Install OpenFeature.Contrib.Provider.FeatureManagement as a Cake Addin
#addin nuget:?package=OpenFeature.Contrib.Provider.FeatureManagement&version=0.0.1-preview&prerelease

// Install OpenFeature.Contrib.Provider.FeatureManagement as a Cake Tool
#tool nuget:?package=OpenFeature.Contrib.Provider.FeatureManagement&version=0.0.1-preview&prerelease
```

## Using the FeatureManagement Provider with the OpenFeature SDK

FeatureManagement is built on top of .NETs Configuration system, so you must provide the loaded Configuration.  
Since Configuration is passed in any valid Configuration source can be used.  
For simplicity, we'll stick with a json file for all examples.  

```csharp
namespace OpenFeatureTestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("featureFlags.json");

            IConfiguration configuration = configurationBuilder.Build();

            var featureManagementProvider = new FeatureManagementProvider(configuration);
            OpenFeature.Api.Instance.SetProvider(featureManagementProvider);

            var client = OpenFeature.Api.Instance.GetClient();

            var val = await client.GetBooleanValue("myBoolFlag", false, null);

            System.Console.WriteLine(val.ToString());
        }
    }
}
```

A simple example configuration would look like this.

```json
{
  "FeatureManagement": {
    "myBoolFlag": {
      "Allocation": {
        "DefaultWhenEnabled": "FlagEnabled",
        "DefaultWhenDisabled": "FlagDisabled"
      },
      "Variants": [
        {
          "Name": "FlagEnabled",
          "ConfigurationValue": true
        },
        {
          "Name": "FlagDisabled",
          "ConfigurationValue": false
        }
      ],
      "EnabledFor": [
        {
          "Name": "AlwaysOn"
        }
      ]
    }
  }
}
```

