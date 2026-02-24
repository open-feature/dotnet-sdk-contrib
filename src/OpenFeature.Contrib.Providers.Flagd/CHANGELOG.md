# Changelog

## [0.3.6](https://github.com/open-feature/dotnet-sdk-contrib/compare/OpenFeature.Contrib.Providers.Flagd-v0.3.5...OpenFeature.Contrib.Providers.Flagd-v0.3.6) (2026-02-24)


### 🐛 Bug Fixes

* **flagd:** Default to correct Port when only Resolver is set ([#585](https://github.com/open-feature/dotnet-sdk-contrib/issues/585)) ([8a1d97c](https://github.com/open-feature/dotnet-sdk-contrib/commit/8a1d97c817bca8575efd6f4a13dbc2d79c446b73))

## [0.3.5](https://github.com/open-feature/dotnet-sdk-contrib/compare/OpenFeature.Contrib.Providers.Flagd-v0.3.4...OpenFeature.Contrib.Providers.Flagd-v0.3.5) (2026-02-03)


### 🐛 Bug Fixes

* Fix Flagd InProcess Evaluator when no defaultVariant is specified ([#513](https://github.com/open-feature/dotnet-sdk-contrib/issues/513)) ([5fd7cce](https://github.com/open-feature/dotnet-sdk-contrib/commit/5fd7cce549a7c1f9aaf68d473225ec692fd497ed))
* flagd not throwing parse error when target key is not a primitive ([#540](https://github.com/open-feature/dotnet-sdk-contrib/issues/540)) ([5d54780](https://github.com/open-feature/dotnet-sdk-contrib/commit/5d5478096018dc946e4ae9bcc408ae0163515a04))
* **flagd:** Flag metadata not being mapped when using RpcResolver ([#575](https://github.com/open-feature/dotnet-sdk-contrib/issues/575)) ([cc24731](https://github.com/open-feature/dotnet-sdk-contrib/commit/cc24731fa2521ffc48757f8ba13e439b2307b2ae))
* **flagd:** Return parse error when Rpc Resolver receives a data loss exception ([#554](https://github.com/open-feature/dotnet-sdk-contrib/issues/554)) ([43e9d9c](https://github.com/open-feature/dotnet-sdk-contrib/commit/43e9d9c26c0eb894b913659355c4f53aaad1e31d))
* **flagd:** Throw general error when targeting variant not found ([#553](https://github.com/open-feature/dotnet-sdk-contrib/issues/553)) ([6b30828](https://github.com/open-feature/dotnet-sdk-contrib/commit/6b30828fa75184e32fa424b150f4e6da611ac647))
* Handle error state in Init() with TaskCompletionSource ([#571](https://github.com/open-feature/dotnet-sdk-contrib/issues/571)) ([fbcfac8](https://github.com/open-feature/dotnet-sdk-contrib/commit/fbcfac85692b266a41c9ef0baca82029938ed6b6))


### ✨ New Features

* **flagd:** Add code to sync context from Flagd for the InProcess resolver ([#515](https://github.com/open-feature/dotnet-sdk-contrib/issues/515)) ([a9c4bb4](https://github.com/open-feature/dotnet-sdk-contrib/commit/a9c4bb46bc7bc690b1c22cdb65e2c8e65a772d0b))
* Update Target Framework to include net10.0 ([#548](https://github.com/open-feature/dotnet-sdk-contrib/issues/548)) ([5f6ec66](https://github.com/open-feature/dotnet-sdk-contrib/commit/5f6ec66e599245193e0f3202e9ea89da72c7df29))


### 🔧 Refactoring

* Decouple event publishing from Flagd Resolver when state changes ([#531](https://github.com/open-feature/dotnet-sdk-contrib/issues/531)) ([c48e25a](https://github.com/open-feature/dotnet-sdk-contrib/commit/c48e25afad6213ae1f8f904eeff77e36514743c5))
* **flagd:** Remove HTTP call when validating JSON schema ([#547](https://github.com/open-feature/dotnet-sdk-contrib/issues/547)) ([8ed7f04](https://github.com/open-feature/dotnet-sdk-contrib/commit/8ed7f04ad72de9acc60e345f4ab45d5a2ed4a4bb))

## [0.3.4](https://github.com/open-feature/dotnet-sdk-contrib/compare/OpenFeature.Contrib.Providers.Flagd-v0.3.3...OpenFeature.Contrib.Providers.Flagd-v0.3.4) (2025-10-17)


### 🐛 Bug Fixes

* Update OpenFeature dependencies and remove unused lifecycle method ([#475](https://github.com/open-feature/dotnet-sdk-contrib/issues/475)) ([1b3af8d](https://github.com/open-feature/dotnet-sdk-contrib/commit/1b3af8da0a78aa3cee181bc44bd8d2a80515cacd))

## [0.3.3](https://github.com/open-feature/dotnet-sdk-contrib/compare/OpenFeature.Contrib.Providers.Flagd-v0.3.2...OpenFeature.Contrib.Providers.Flagd-v0.3.3) (2025-07-04)


### ✨ New Features

* Add Dependency Injection extensions for Adding Flagd Provider ([#410](https://github.com/open-feature/dotnet-sdk-contrib/issues/410)) ([0f59127](https://github.com/open-feature/dotnet-sdk-contrib/commit/0f59127adadcd8f5675c21b4fc285fcf4368b263))
* add JSON Schema validation for Flagd provider when in-process mode is used ([#373](https://github.com/open-feature/dotnet-sdk-contrib/issues/373)) ([5037f60](https://github.com/open-feature/dotnet-sdk-contrib/commit/5037f60c4a2bf14b79fb1420e9340c66da6671b4))

## [0.3.2](https://github.com/open-feature/dotnet-sdk-contrib/compare/OpenFeature.Contrib.Providers.Flagd-v0.3.1...OpenFeature.Contrib.Providers.Flagd-v0.3.2) (2025-04-23)


### 🧹 Chore

* update sdk version in readme ([90750b6](https://github.com/open-feature/dotnet-sdk-contrib/commit/90750b66218dadb09355f53ef0f796fd7185632e))

## [0.3.1](https://github.com/open-feature/dotnet-sdk-contrib/compare/OpenFeature.Contrib.Providers.Flagd-v0.3.0...OpenFeature.Contrib.Providers.Flagd-v0.3.1) (2025-04-22)


### 🐛 Bug Fixes

* migrate to System.Text.Json and JsonLogic ([#347](https://github.com/open-feature/dotnet-sdk-contrib/issues/347)) ([ef98686](https://github.com/open-feature/dotnet-sdk-contrib/commit/ef9868688f0804e26a1b69b6ea25be5f105c26b5))


### ✨ New Features

* Update in-process resolver to support flag metadata [#305](https://github.com/open-feature/dotnet-sdk-contrib/issues/305) ([#309](https://github.com/open-feature/dotnet-sdk-contrib/issues/309)) ([e603c08](https://github.com/open-feature/dotnet-sdk-contrib/commit/e603c08df7c19f360b2d8896caef3e3a5bcdcefd))


### 🧹 Chore

* **deps:** update dependency google.protobuf to 3.28.2 ([#272](https://github.com/open-feature/dotnet-sdk-contrib/issues/272)) ([1c45c1a](https://github.com/open-feature/dotnet-sdk-contrib/commit/1c45c1a3578ddc814483ac83549c2be5579d403c))
* **deps:** update dependency google.protobuf to 3.30.2 ([#335](https://github.com/open-feature/dotnet-sdk-contrib/issues/335)) ([3f63d35](https://github.com/open-feature/dotnet-sdk-contrib/commit/3f63d35540979dfb42e1f9d80ba5d2bba0b4a509))
* **deps:** update dependency grpc.net.client to 2.66.0 ([#282](https://github.com/open-feature/dotnet-sdk-contrib/issues/282)) ([04803d7](https://github.com/open-feature/dotnet-sdk-contrib/commit/04803d7cfcf739ea17c11dc576444ae75ba85192))
* **deps:** update dependency grpc.net.client to 2.70.0 ([#336](https://github.com/open-feature/dotnet-sdk-contrib/issues/336)) ([cd4cd4f](https://github.com/open-feature/dotnet-sdk-contrib/commit/cd4cd4f29bedebcca0a11085307bed72e6e7b794))
* **deps:** update dependency grpc.tools to 2.66.0 ([#271](https://github.com/open-feature/dotnet-sdk-contrib/issues/271)) ([161fb63](https://github.com/open-feature/dotnet-sdk-contrib/commit/161fb638f22eecae2d4caa84c6c595878c8c48c9))
* **deps:** update dependency grpc.tools to 2.71.0 ([#286](https://github.com/open-feature/dotnet-sdk-contrib/issues/286)) ([84acae2](https://github.com/open-feature/dotnet-sdk-contrib/commit/84acae2663677cf60c7e9691fb22fd250af6fd64))
* **deps:** update dependency semver to v3 ([#351](https://github.com/open-feature/dotnet-sdk-contrib/issues/351)) ([9f47608](https://github.com/open-feature/dotnet-sdk-contrib/commit/9f4760807f6d5ddf416a8ec7bd931f698f4f30b2))
* **deps:** update ghcr.io/open-feature/flagd-testbed docker tag to v0.5.21 ([#291](https://github.com/open-feature/dotnet-sdk-contrib/issues/291)) ([29553b2](https://github.com/open-feature/dotnet-sdk-contrib/commit/29553b252344057dc4eba7379b95acb085e9caa1))
* **deps:** update ghcr.io/open-feature/flagd-testbed-unstable docker tag to v0.5.21 ([#323](https://github.com/open-feature/dotnet-sdk-contrib/issues/323)) ([faa44cc](https://github.com/open-feature/dotnet-sdk-contrib/commit/faa44cc6db5b014069f3dd72b1bf34e3e5ada1df))
* **deps:** update ghcr.io/open-feature/flagd-testbed-unstable docker tag to v1 ([#339](https://github.com/open-feature/dotnet-sdk-contrib/issues/339)) ([308cc42](https://github.com/open-feature/dotnet-sdk-contrib/commit/308cc42afce6196ff4c4ffc89350454a44f1d1e0))
* **deps:** update ghcr.io/open-feature/sync-testbed-unstable docker tag to v0.5.13 ([#333](https://github.com/open-feature/dotnet-sdk-contrib/issues/333)) ([6cbf656](https://github.com/open-feature/dotnet-sdk-contrib/commit/6cbf6563e7f5b2f4c4b8e0b557f978cfe12f79c9))
* **deps:** update src/openfeature.contrib.providers.flagd/schemas digest to 9b0ee43 ([#332](https://github.com/open-feature/dotnet-sdk-contrib/issues/332)) ([1f7214d](https://github.com/open-feature/dotnet-sdk-contrib/commit/1f7214d28f04e504fdf8f3dac7fa14ff613fa677))
* **deps:** update src/openfeature.contrib.providers.flagd/schemas digest to c707f56 ([#343](https://github.com/open-feature/dotnet-sdk-contrib/issues/343)) ([5d142fd](https://github.com/open-feature/dotnet-sdk-contrib/commit/5d142fd798da9b668d8b45f5f6310a03b1424c36))
* Use TestContainers instead of github services / docker for e2e tests ([#345](https://github.com/open-feature/dotnet-sdk-contrib/issues/345)) ([1173f4f](https://github.com/open-feature/dotnet-sdk-contrib/commit/1173f4f1c0a06f191d4aa6b0353ac54f81889ec6))

## [0.3.0](https://github.com/open-feature/dotnet-sdk-contrib/compare/OpenFeature.Contrib.Providers.Flagd-v0.2.3...OpenFeature.Contrib.Providers.Flagd-v0.3.0) (2024-08-22)


### ⚠ BREAKING CHANGES

* use (and require) OpenFeature SDK v2 ([#262](https://github.com/open-feature/dotnet-sdk-contrib/issues/262))

### ✨ New Features

* use (and require) OpenFeature SDK v2 ([#262](https://github.com/open-feature/dotnet-sdk-contrib/issues/262)) ([f845134](https://github.com/open-feature/dotnet-sdk-contrib/commit/f84513438586457087ac47fd40629912f2ec473a))

## [0.2.3](https://github.com/open-feature/dotnet-sdk-contrib/compare/OpenFeature.Contrib.Providers.Flagd-v0.2.2...OpenFeature.Contrib.Providers.Flagd-v0.2.3) (2024-08-21)


### 🐛 Bug Fixes

* version expression ([cad2cd1](https://github.com/open-feature/dotnet-sdk-contrib/commit/cad2cd166d0c25753b37189f044c3a585cda0fad))


### 🧹 Chore

* **deps:** update dependency grpc.net.client to v2.65.0 ([#242](https://github.com/open-feature/dotnet-sdk-contrib/issues/242)) ([d20431c](https://github.com/open-feature/dotnet-sdk-contrib/commit/d20431cc9793edad3e08517a5c3a64a0103a48f3))

## [0.2.2](https://github.com/open-feature/dotnet-sdk-contrib/compare/OpenFeature.Contrib.Providers.Flagd-v0.2.1...OpenFeature.Contrib.Providers.Flagd-v0.2.2) (2024-08-20)


### 🧹 Chore

* **deps:** update dependency google.protobuf to v3.27.2 ([#220](https://github.com/open-feature/dotnet-sdk-contrib/issues/220)) ([316e159](https://github.com/open-feature/dotnet-sdk-contrib/commit/316e159a417e5b4268482551fec8e7eb34436648))
* **deps:** update dependency google.protobuf to v3.27.3 ([#244](https://github.com/open-feature/dotnet-sdk-contrib/issues/244)) ([204fdda](https://github.com/open-feature/dotnet-sdk-contrib/commit/204fddad9f2b5f1601e1eaf913654e190020d15e))
* **deps:** update dependency grpc.net.client to v2.64.0 ([#239](https://github.com/open-feature/dotnet-sdk-contrib/issues/239)) ([3d3ed02](https://github.com/open-feature/dotnet-sdk-contrib/commit/3d3ed02e3bdf979336e0dad8e3d5ed6886345a2d))
* **deps:** update dependency grpc.tools to v2.65.0 ([#234](https://github.com/open-feature/dotnet-sdk-contrib/issues/234)) ([b5ea7bf](https://github.com/open-feature/dotnet-sdk-contrib/commit/b5ea7bf6be6b37a2461d0d3192445901df2e1cdc))
* **deps:** update dotnet monorepo ([#229](https://github.com/open-feature/dotnet-sdk-contrib/issues/229)) ([0ee1e5b](https://github.com/open-feature/dotnet-sdk-contrib/commit/0ee1e5b4e3a002e2f56c78ee5d5665576190cbba))
* update OpenFeature version compatiblity ([#249](https://github.com/open-feature/dotnet-sdk-contrib/issues/249)) ([232e948](https://github.com/open-feature/dotnet-sdk-contrib/commit/232e948a0916ca10612f85343e2eecebca107090))

## [0.2.1](https://github.com/open-feature/dotnet-sdk-contrib/compare/OpenFeature.Contrib.Providers.Flagd-v0.2.0...OpenFeature.Contrib.Providers.Flagd-v0.2.1) (2024-07-16)


### 🐛 Bug Fixes

* remove Microsoft.Extensions.Logging from flagd provider ([#233](https://github.com/open-feature/dotnet-sdk-contrib/issues/233)) ([7385735](https://github.com/open-feature/dotnet-sdk-contrib/commit/7385735aee328e60be323fba037291bf4fd3d1c9))

## [0.2.0](https://github.com/open-feature/dotnet-sdk-contrib/compare/OpenFeature.Contrib.Providers.Flagd-v0.1.9...OpenFeature.Contrib.Providers.Flagd-v0.2.0) (2024-07-04)


### ⚠ BREAKING CHANGES

* in-process var name/value (FLAGD_RESOLVER="in-process") ([#206](https://github.com/open-feature/dotnet-sdk-contrib/issues/206))

### 🐛 Bug Fixes

* in-process var name/value (FLAGD_RESOLVER="in-process") ([#206](https://github.com/open-feature/dotnet-sdk-contrib/issues/206)) ([1e580d7](https://github.com/open-feature/dotnet-sdk-contrib/commit/1e580d75a06f3d9f4683578e692247dbfc8aa7ea))


### ✨ New Features

* relative weights in fractional, fix injected props ([#208](https://github.com/open-feature/dotnet-sdk-contrib/issues/208)) ([7cccc8d](https://github.com/open-feature/dotnet-sdk-contrib/commit/7cccc8df0de6d9607e045fa62f070f35f20d6a0a))


### 🧹 Chore

* additional unit tests for flagd provider ([#203](https://github.com/open-feature/dotnet-sdk-contrib/issues/203)) ([38a59f0](https://github.com/open-feature/dotnet-sdk-contrib/commit/38a59f01b4c740ddcfb69b68c8b79fb169e06ad4))
* **deps:** update dependency google.protobuf to v3.27.1 ([#75](https://github.com/open-feature/dotnet-sdk-contrib/issues/75)) ([0db8692](https://github.com/open-feature/dotnet-sdk-contrib/commit/0db86920eddaf16fa4685843e5e6b6308893d012))
* **deps:** update dependency grpc.net.client to v2.63.0 ([#209](https://github.com/open-feature/dotnet-sdk-contrib/issues/209)) ([ce14c23](https://github.com/open-feature/dotnet-sdk-contrib/commit/ce14c2389c3602aa211d877b4122b7a7d03835b9))
* **deps:** update dependency grpc.tools to v2.63.0 ([#193](https://github.com/open-feature/dotnet-sdk-contrib/issues/193)) ([75e4eb7](https://github.com/open-feature/dotnet-sdk-contrib/commit/75e4eb7c379e6680545fb6dc638ec0345114877b))
* **deps:** update dependency grpc.tools to v2.64.0 ([#207](https://github.com/open-feature/dotnet-sdk-contrib/issues/207)) ([eafdc3c](https://github.com/open-feature/dotnet-sdk-contrib/commit/eafdc3c206f010a9d363dc4cd70b2308d6b5fab1))

## [0.1.9](https://github.com/open-feature/dotnet-sdk-contrib/compare/OpenFeature.Contrib.Providers.Flagd-v0.1.8...OpenFeature.Contrib.Providers.Flagd-v0.1.9) (2024-04-30)


### 🐛 Bug Fixes

* provider status incorrect ([#187](https://github.com/open-feature/dotnet-sdk-contrib/issues/187)) ([6108d45](https://github.com/open-feature/dotnet-sdk-contrib/commit/6108d452d6c8a5c70c18b45ea9dd2e13612370ec))
* various in-process fixes, e2e tests ([#189](https://github.com/open-feature/dotnet-sdk-contrib/issues/189)) ([f2d096f](https://github.com/open-feature/dotnet-sdk-contrib/commit/f2d096fb4c1140a64a6d95bd17fd2efaf2320cda))


### ✨ New Features

* add custom JsonLogic evaluators ([#159](https://github.com/open-feature/dotnet-sdk-contrib/issues/159)) ([18aa151](https://github.com/open-feature/dotnet-sdk-contrib/commit/18aa15161975aeb5d334e79d9a57af5c0d2ee14a))
* add custom jsonLogic string evaluators ([#158](https://github.com/open-feature/dotnet-sdk-contrib/issues/158)) ([728286c](https://github.com/open-feature/dotnet-sdk-contrib/commit/728286c3ad8677cb92ef378ae714cb1b5f2cfea4))
* in-process provider ([#149](https://github.com/open-feature/dotnet-sdk-contrib/issues/149)) ([f7371dc](https://github.com/open-feature/dotnet-sdk-contrib/commit/f7371dc91a3b8a9a6036429aee31d1098aed958f))


### 🧹 Chore

* **deps:** update dependency grpc.tools to v2.62.0 ([#151](https://github.com/open-feature/dotnet-sdk-contrib/issues/151)) ([97b124f](https://github.com/open-feature/dotnet-sdk-contrib/commit/97b124fe3c047b8a1d1f08ca0d3619addddd94af))


### 📚 Documentation

* fix typo in flagd readme ([8738e31](https://github.com/open-feature/dotnet-sdk-contrib/commit/8738e3169da13774d734964c3ea621b35a031d77))

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
