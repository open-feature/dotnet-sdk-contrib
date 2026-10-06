# flagd .NET Core

The in-process [flagd](https://flagd.dev) flag evaluation engine for .NET.

This package contains only the evaluation logic: parsing a flagd flag configuration and resolving flags against it,
including targeting rules and the custom flagd operators (`fractional`, `sem_ver`, `starts_with`, `ends_with`) and
shared `$evaluators`. It performs no I/O. Fetching, syncing and watching flag configurations is left to the
concrete provider.

If you want to evaluate flags from a flagd service or a file, use
[OpenFeature.Providers.Flagd](https://www.nuget.org/packages/OpenFeature.Providers.Flagd) instead. Use this package when
you are building your own flagd-compatible provider and want to reuse the evaluation engine.

## Requirements

- open-feature/dotnet-sdk v2.9.0 or later, below v3.0.0

## Install

```shell
dotnet add package OpenFeature.Providers.Flagd.Core
```

## Usage

```csharp
using OpenFeature.Model;
using OpenFeature.Providers.Flagd.Core;

var core = new FlagdCore();

// Load a flagd flag configuration. Returns the keys of the flags that were added, removed or changed.
IReadOnlyList<string> changedKeys = core.SetConfigurations(flagConfigurationJson);

// Flag-set metadata, e.g. a "version" supplied by the flag source
var metadata = core.GetFlagSetMetadata();

// All currently loaded flag keys
IReadOnlyList<string> allKeys = core.GetFlagKeys();

// Resolve flags synchronously
var context = EvaluationContext.Builder().SetTargetingKey("user-1").Set("email", "me@example.com").Build();
ResolutionDetails<bool> details = core.ResolveBoolean("my-flag", false, context);
```

`ResolveBoolean`, `ResolveString`, `ResolveInteger`, `ResolveDouble` and `ResolveStructure` return
`ResolutionDetails<T>` and throw the OpenFeature `FeatureProviderException` subtypes (for example flag not found or type
mismatch) that a provider is expected to surface to the SDK.

### Thread safety

A `FlagdCore` instance is safe for concurrent use. Flags can be resolved from any number of threads while another thread
calls `SetConfigurations`; each resolution sees either the previous or the new configuration in full, never a mix of both.

### Invalid configurations

`SetConfigurations` throws a `ParseErrorException` when the configuration cannot be parsed. The previously loaded flags
and metadata are left untouched, so a failed update never leaves the engine empty.

### Schema validation

Flag configurations can additionally be checked against the flagd [flags](https://flagd.dev/schema/v0/flags.json) and
[targeting](https://flagd.dev/schema/v0/targeting.json) JSON schemas. Validation only logs warnings, it never rejects a
configuration. It is off until the schemas are loaded, which is asynchronous:

```csharp
var core = new FlagdCore(new FlagdCoreOptions { Logger = logger });
await core.InitializeAsync();
```

`Logger` is the only option. `FlagdCore` itself performs no I/O, so constructing and using it
without calling `InitializeAsync` never touches the network or the file system.
