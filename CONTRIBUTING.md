# Contributing

## System Requirements

Dotnet 6+ is recommended.

## Compilation target(s)

As in the Dotnet-SDK, we target C# LangVersion 7.3. The `Common.props` configures this automatically.

## Adding a project

1. Create a new library project under `src/`: `dotnet new classlib -o src/OpenFeature.Contrib.MyComponent --langVersion 7.3`
2. Create a new test project under `test/`: `dotnet new xunit -o test/OpenFeature.Contrib.MyComponent.Test`
3. Add the library project to the solution: `dotnet sln DotnetSdkContrib.sln add src/OpenFeature.Contrib.MyComponent/OpenFeature.Contrib.MyComponent.csproj`
4. Add the test project to the solution: `dotnet sln DotnetSdkContrib.sln add test/OpenFeature.Contrib.MyComponent.Test/OpenFeature.Contrib.MyComponent.Test.csproj`
5. Add the desired properties to your library's `.csproj` file (see example below).
5. Remove all content besides the root element from your test project's `.csproj` file (all settings will be inherited).
6. Add the new library project to `release-please-config.json`.
7. Add a `version.txt` file to the root of your library with a version matching that in your new `.csproj` file, e.g. `0.0.1`.
8. If you care to release a pre `1.0.0` version, add the same version above to `.release-please-manifest.json`. Failing to do this will release a `1.0.0` initial release.

Sample `.csproj` file:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <PackageId>OpenFeature.MyComponent</PackageId>
    <VersionNumber>0.0.1</VersionNumber> <!--x-release-please-version -->
    <Version>$(VersionNumber)</Version>
    <AssemblyVersion>$(VersionNumber)</AssemblyVersion>
    <FileVersion>$(VersionNumber)</FileVersion>
    <Description>A very valuable OpenFeature contribution!</Description>
    <PackageProjectUrl>https://openfeature.dev</PackageProjectUrl>
    <RepositoryUrl>https://github.com/open-feature/dotnet-sdk-contrib</RepositoryUrl>
    <Authors>Me!</Authors>
  </PropertyGroup>

</Project>
```

## Documentation

Any published modules must have documentation in their root directory, explaining the basic purpose of the module as well as installation and usage instructions.
Instructions for how to develop a module should also be included (required system dependencies, instructions for testing locally, etc).

## Testing

Any published modules must have reasonable test coverage.
The instructions above will generate a test project for you.

Use `dotnet test` to test the entire project.

## Versioning and releasing

As described in the [README](./README.md), this project uses release-please, and semantic versioning.
Breaking changes should be identified by using a semantic PR title.

## Dependencies

Keep dependencies to a minimum.
Dependencies used only for building and testing should have a `<PrivateAssets>all</PrivateAssets>` element to prevent them from being exposed to consumers.
