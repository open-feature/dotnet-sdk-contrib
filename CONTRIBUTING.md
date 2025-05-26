# Contributing

## System Requirements

Dotnet 8+ is recommended.

## Adding a project

1. Create a new library project under `src/`: `dotnet new classlib -o src/OpenFeature.Contrib.MyComponent`
2. Create a new test project under `test/`: `dotnet new xunit -o test/OpenFeature.Contrib.MyComponent.Test`
3. Add the library project to the solution: `dotnet sln DotnetSdkContrib.slnx add src/OpenFeature.Contrib.MyComponent/OpenFeature.Contrib.MyComponent.csproj`
4. Add the test project to the solution: `dotnet sln DotnetSdkContrib.slnx add test/OpenFeature.Contrib.MyComponent.Test/OpenFeature.Contrib.MyComponent.Test.csproj`
5. Add the desired properties to your library's `.csproj` file (see example below).
6. Remove all content besides the root element from your test project's `.csproj` file (all settings will be inherited).
7. Add the new library project to `release-please-config.json`.
8. Add a `version.txt` file to the root of your library with a version matching that in your new `.csproj` file, e.g. `0.0.1`.
9. If you care to release a pre `1.0.0` version, add the same version above to `.release-please-manifest.json`. Failing to do this will release a `1.0.0` initial release.

Sample `.csproj` file:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <PackageId>OpenFeature.Contrib.MyComponent</PackageId>
    <VersionNumber>0.0.1</VersionNumber> <!--x-release-please-version -->
    <VersionPrefix>$(VersionNumber)</VersionPrefix>
    <AssemblyVersion>$(VersionNumber)</AssemblyVersion>
    <FileVersion>$(VersionNumber)</FileVersion>
    <Description>A very valuable OpenFeature contribution!</Description>
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

This repo uses _Release Please_ to release packages. Release Please sets up a running PR that tracks all changes for the library components, and maintains the versions according to [conventional commits](https://www.conventionalcommits.org/en/v1.0.0/), generated when [PRs are merged](https://github.com/amannn/action-semantic-pull-request). When Release Please's running PR is merged, any changed artifacts are published.

## Dependencies

Keep dependencies to a minimum.
Dependencies used only for building and testing should have a `<PrivateAssets>all</PrivateAssets>` element to prevent them from being exposed to consumers.

## Consuming pre-release packages

1. Acquire a [GitHub personal access token (PAT)](https://docs.github.com/github/authenticating-to-github/creating-a-personal-access-token) scoped for `read:packages` and verify the permissions:

    ```console
    $ gh auth login --scopes read:packages

    ? What account do you want to log into? GitHub.com
    ? What is your preferred protocol for Git operations? HTTPS
    ? How would you like to authenticate GitHub CLI? Login with a web browser

    ! First copy your one-time code: ****-****
    Press Enter to open github.com in your browser...

    ✓ Authentication complete.
    - gh config set -h github.com git_protocol https
    ✓ Configured git protocol
    ✓ Logged in as ********
    ```

    ```console
    $ gh auth status

    github.com
      ✓ Logged in to github.com as ******** (~/.config/gh/hosts.yml)
      ✓ Git operations for github.com configured to use https protocol.
      ✓ Token: gho_************************************
      ✓ Token scopes: gist, read:org, read:packages, repo, workflow
    ```

2. Run the following command to configure your local environment to consume packages from GitHub Packages:

    ```console
    $ dotnet nuget update source github-open-feature --username $(gh api user --jq .email) --password $(gh auth token) --store-password-in-clear-text

    Package source "github-open-feature" was successfully updated.
    ```
