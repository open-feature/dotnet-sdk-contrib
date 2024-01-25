# Changelog

## [0.1.8](https://github.com/open-feature/dotnet-sdk-contrib/compare/OpenFeature.Contrib.Providers.Flagd-v0.1.7...OpenFeature.Contrib.Providers.Flagd-v0.1.8) (2024-01-24)


### 🐛 Bug Fixes

* Return correct name from FlagdProvider ([#126](https://github.com/open-feature/dotnet-sdk-contrib/issues/126)) ([0b704e9](https://github.com/open-feature/dotnet-sdk-contrib/commit/0b704e9662ab63fa164235aefa2013f0a9101857))


### 🧹 Chore

* Add support for GitHub Packages ([#134](https://github.com/open-feature/dotnet-sdk-contrib/issues/134)) ([0def0da](https://github.com/open-feature/dotnet-sdk-contrib/commit/0def0da173e2f327b7381eba043b6e99ae8f26fe))
* **deps:** update dependency grpc.net.client to v2.58.0 ([#92](https://github.com/open-feature/dotnet-sdk-contrib/issues/92)) ([350d5ef](https://github.com/open-feature/dotnet-sdk-contrib/commit/350d5efdfde51e1557e4f37b82c6baaccb05b2c9))
* **deps:** update dependency grpc.net.client to v2.59.0 ([#115](https://github.com/open-feature/dotnet-sdk-contrib/issues/115)) ([f33c5ff](https://github.com/open-feature/dotnet-sdk-contrib/commit/f33c5ff8ea9040ed61d7b36a2d4cf621a3a5c813))
* **deps:** update dependency grpc.tools to v2.59.0 ([#88](https://github.com/open-feature/dotnet-sdk-contrib/issues/88)) ([fa1dccc](https://github.com/open-feature/dotnet-sdk-contrib/commit/fa1dccc647da33b77a1509afe791b4fa83fab3e8))
* **deps:** update dependency grpc.tools to v2.60.0 ([#111](https://github.com/open-feature/dotnet-sdk-contrib/issues/111)) ([500cfe4](https://github.com/open-feature/dotnet-sdk-contrib/commit/500cfe49a4d12e4af199f9050cd89abeb06bcfe5))
* **deps:** update dotnet monorepo to v8 (major) ([#100](https://github.com/open-feature/dotnet-sdk-contrib/issues/100)) ([13d3223](https://github.com/open-feature/dotnet-sdk-contrib/commit/13d32231983e61ec9960cabfbf9a55fc5a6b32cb))


### 🚀 Performance

* Cleanup allocations + missing ConfigureAwait's ([#124](https://github.com/open-feature/dotnet-sdk-contrib/issues/124)) ([e3d0f06](https://github.com/open-feature/dotnet-sdk-contrib/commit/e3d0f06c5fc732c068eb5d135143fac3c2a6b01e))

## [0.1.7](https://github.com/open-feature/dotnet-sdk-contrib/compare/OpenFeature.Contrib.Providers.Flagd-v0.1.6...OpenFeature.Contrib.Providers.Flagd-v0.1.7) (2023-09-13)


### 🐛 Bug Fixes

* Setup config correct when passing a Uri (fixes [#71](https://github.com/open-feature/dotnet-sdk-contrib/issues/71)) ([#83](https://github.com/open-feature/dotnet-sdk-contrib/issues/83)) ([e27d351](https://github.com/open-feature/dotnet-sdk-contrib/commit/e27d351f7e3392102e2c7f840a0ab30e13198613))


### 🧹 Chore

* **deps:** update dependency grpc.net.client to v2.57.0 ([#78](https://github.com/open-feature/dotnet-sdk-contrib/issues/78)) ([b749549](https://github.com/open-feature/dotnet-sdk-contrib/commit/b74954944c87dd708a0256a44fd7df8db911a66c))
* **deps:** update dependency grpc.tools to v2.57.0 ([#77](https://github.com/open-feature/dotnet-sdk-contrib/issues/77)) ([9690abc](https://github.com/open-feature/dotnet-sdk-contrib/commit/9690abc3e3540cee3ec2a6c0cd29e81c8d4d39be))
* **deps:** update dependency grpc.tools to v2.58.0 ([#82](https://github.com/open-feature/dotnet-sdk-contrib/issues/82)) ([9b017ff](https://github.com/open-feature/dotnet-sdk-contrib/commit/9b017ff3a92499901c677e5cf9347ab387f91aaa))

## [0.1.6](https://github.com/open-feature/dotnet-sdk-contrib/compare/OpenFeature.Contrib.Providers.Flagd-v0.1.5...OpenFeature.Contrib.Providers.Flagd-v0.1.6) (2023-08-08)


### Bug Fixes

* NET462 requires TLS for GRPC to work ([#72](https://github.com/open-feature/dotnet-sdk-contrib/issues/72)) ([2322f43](https://github.com/open-feature/dotnet-sdk-contrib/commit/2322f4319b4b44b66c6965e736551538b4ced9a1))

## [0.1.5](https://github.com/open-feature/dotnet-sdk-contrib/compare/OpenFeature.Contrib.Providers.Flagd-v0.1.4...OpenFeature.Contrib.Providers.Flagd-v0.1.5) (2023-04-07)


### Features

* introduce FlagdProvider constructor accepting FlagdConfig as parameter ([#57](https://github.com/open-feature/dotnet-sdk-contrib/issues/57)) ([2e4fda3](https://github.com/open-feature/dotnet-sdk-contrib/commit/2e4fda3abc6ffd3c43d5ea42dcddb855f5298322))

## [0.1.4](https://github.com/open-feature/dotnet-sdk-contrib/compare/OpenFeature.Contrib.Providers.Flagd-v0.1.3...OpenFeature.Contrib.Providers.Flagd-v0.1.4) (2023-04-04)


### Features

* implemented LRU caching for flagd provider  ([#47](https://github.com/open-feature/dotnet-sdk-contrib/issues/47)) ([f4d2142](https://github.com/open-feature/dotnet-sdk-contrib/commit/f4d21426e9ec079d62ecca4e8d1936cb8ad299b7))
* support TLS connection in flagd provider ([#48](https://github.com/open-feature/dotnet-sdk-contrib/issues/48)) ([49e775a](https://github.com/open-feature/dotnet-sdk-contrib/commit/49e775a425b043e5774fbae348cfa2c4af59f2cf))

## [0.1.3](https://github.com/open-feature/dotnet-sdk-contrib/compare/OpenFeature.Contrib.Providers.Flagd-v0.1.2...OpenFeature.Contrib.Providers.Flagd-v0.1.3) (2023-03-22)


### Features

* unix socket support for flagd provider ([#42](https://github.com/open-feature/dotnet-sdk-contrib/issues/42)) ([9679fe4](https://github.com/open-feature/dotnet-sdk-contrib/commit/9679fe40cb13b48fa2f34521ce6175d9b8a6874b))

## [0.1.2](https://github.com/open-feature/dotnet-sdk-contrib/compare/OpenFeature.Contrib.Providers.Flagd-v0.1.1...OpenFeature.Contrib.Providers.Flagd-v0.1.2) (2023-02-13)


### Bug Fixes

* update flagd provider docs, publishing ([#39](https://github.com/open-feature/dotnet-sdk-contrib/issues/39)) ([7abdf5e](https://github.com/open-feature/dotnet-sdk-contrib/commit/7abdf5e979fe03b41ecf83e05c41ceb626941510))

## [0.1.1](https://github.com/open-feature/dotnet-sdk-contrib/compare/OpenFeature.Contrib.Providers.Flagd-v0.1.0...OpenFeature.Contrib.Providers.Flagd-v0.1.1) (2023-02-13)


### Features

* flagd provider basic functionality ([#31](https://github.com/open-feature/dotnet-sdk-contrib/issues/31)) ([5ed9336](https://github.com/open-feature/dotnet-sdk-contrib/commit/5ed9336132a12c058f46beef5c861233270e975e))
* implement the flagd provider ([5ed9336](https://github.com/open-feature/dotnet-sdk-contrib/commit/5ed9336132a12c058f46beef5c861233270e975e))

## [0.1.0](https://github.com/open-feature/dotnet-sdk-contrib/compare/OpenFeature.Contrib.Providers.Flagd-v0.0.2...OpenFeature.Contrib.Providers.Flagd-v0.1.0) (2022-10-16)


### ⚠ BREAKING CHANGES

* rename namespace, add OpenFeature dependency and readmes.
* rename namespace, add OpenFeature dep (#18)

### Features

* rename namespace, add OpenFeature dep ([#18](https://github.com/open-feature/dotnet-sdk-contrib/issues/18)) ([3ca3172](https://github.com/open-feature/dotnet-sdk-contrib/commit/3ca31722b83053d4edf2038889c78efa717a7cff))
* rename namespace, add OpenFeature dependency and readmes. ([3ca3172](https://github.com/open-feature/dotnet-sdk-contrib/commit/3ca31722b83053d4edf2038889c78efa717a7cff))
