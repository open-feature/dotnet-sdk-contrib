# Changelog

## 1.0.0 (2025-05-08)


### ⚠ BREAKING CHANGES

* use (and require) OpenFeature SDK v2 ([#262](https://github.com/open-feature/dotnet-sdk-contrib/issues/262))
* in-process var name/value (FLAGD_RESOLVER="in-process") ([#206](https://github.com/open-feature/dotnet-sdk-contrib/issues/206))
* rename namespace, add OpenFeature dependency and readmes.
* rename namespace, add OpenFeature dep ([#18](https://github.com/open-feature/dotnet-sdk-contrib/issues/18))

### Features

* Add ConfigCat provider ([#119](https://github.com/open-feature/dotnet-sdk-contrib/issues/119)) ([20aeb3a](https://github.com/open-feature/dotnet-sdk-contrib/commit/20aeb3a471227571fdc47a46a6292e0b59c9b3a5))
* add custom JsonLogic evaluators ([#159](https://github.com/open-feature/dotnet-sdk-contrib/issues/159)) ([18aa151](https://github.com/open-feature/dotnet-sdk-contrib/commit/18aa15161975aeb5d334e79d9a57af5c0d2ee14a))
* add custom jsonLogic string evaluators ([#158](https://github.com/open-feature/dotnet-sdk-contrib/issues/158)) ([728286c](https://github.com/open-feature/dotnet-sdk-contrib/commit/728286c3ad8677cb92ef378ae714cb1b5f2cfea4))
* Add Flagsmith provider ([#89](https://github.com/open-feature/dotnet-sdk-contrib/issues/89)) ([b7ba62e](https://github.com/open-feature/dotnet-sdk-contrib/commit/b7ba62e4f88f23fba9daeaf487465834846ae532))
* Add Metrics Hook ([#114](https://github.com/open-feature/dotnet-sdk-contrib/issues/114)) ([5845e2b](https://github.com/open-feature/dotnet-sdk-contrib/commit/5845e2b0ae4b89a8a313051b42e6afdd856f1ea3))
* add scaffolding and publishing ([#13](https://github.com/open-feature/dotnet-sdk-contrib/issues/13)) ([d56e3fd](https://github.com/open-feature/dotnet-sdk-contrib/commit/d56e3fd09156dd548c4cb826512e319d28dfd961))
* Add support for basic flags in Feature Management ([#350](https://github.com/open-feature/dotnet-sdk-contrib/issues/350)) ([cfe5e57](https://github.com/open-feature/dotnet-sdk-contrib/commit/cfe5e5739edc0e812d3efcc01b740ecaad88d8a3))
* added OTel hook ([#44](https://github.com/open-feature/dotnet-sdk-contrib/issues/44)) ([cbe92b5](https://github.com/open-feature/dotnet-sdk-contrib/commit/cbe92b52a3f58279d57a054bed368b6003e03561))
* Adding a Provider implementation on top of the standard dotnet FeatureManagement system. ([#129](https://github.com/open-feature/dotnet-sdk-contrib/issues/129)) ([69bf2d6](https://github.com/open-feature/dotnet-sdk-contrib/commit/69bf2d67606affa334792e5a9c70da9e4a28748e))
* Environment Variable Provider ([#312](https://github.com/open-feature/dotnet-sdk-contrib/issues/312)) ([4908000](https://github.com/open-feature/dotnet-sdk-contrib/commit/4908000ed27a648ee7cf8823320ae7d7c8cd8c45))
* flagd provider basic functionality ([#31](https://github.com/open-feature/dotnet-sdk-contrib/issues/31)) ([5ed9336](https://github.com/open-feature/dotnet-sdk-contrib/commit/5ed9336132a12c058f46beef5c861233270e975e))
* GO Feature Flag dotnet provider ([#24](https://github.com/open-feature/dotnet-sdk-contrib/issues/24)) ([964cf32](https://github.com/open-feature/dotnet-sdk-contrib/commit/964cf3297d1b78954d5139750d26acbad9fcd895))
* **gofeatureflag:** Provider refactor ([#313](https://github.com/open-feature/dotnet-sdk-contrib/issues/313)) ([c30446e](https://github.com/open-feature/dotnet-sdk-contrib/commit/c30446eb51538b05378db7c4d56228f01ed1cb88))
* implement the flagd provider ([5ed9336](https://github.com/open-feature/dotnet-sdk-contrib/commit/5ed9336132a12c058f46beef5c861233270e975e))
* implemented LRU caching for flagd provider  ([#47](https://github.com/open-feature/dotnet-sdk-contrib/issues/47)) ([f4d2142](https://github.com/open-feature/dotnet-sdk-contrib/commit/f4d21426e9ec079d62ecca4e8d1936cb8ad299b7))
* in-process provider ([#149](https://github.com/open-feature/dotnet-sdk-contrib/issues/149)) ([f7371dc](https://github.com/open-feature/dotnet-sdk-contrib/commit/f7371dc91a3b8a9a6036429aee31d1098aed958f))
* introduce FlagdProvider constructor accepting FlagdConfig as parameter ([#57](https://github.com/open-feature/dotnet-sdk-contrib/issues/57)) ([2e4fda3](https://github.com/open-feature/dotnet-sdk-contrib/commit/2e4fda3abc6ffd3c43d5ea42dcddb855f5298322))
* Introduce flipt provider for dotnet  ([#293](https://github.com/open-feature/dotnet-sdk-contrib/issues/293)) ([4d59bc3](https://github.com/open-feature/dotnet-sdk-contrib/commit/4d59bc35bd4c65c9989e8c980668d85242240eec))
* map to Statsig CustomIDs ([#210](https://github.com/open-feature/dotnet-sdk-contrib/issues/210)) ([c745edc](https://github.com/open-feature/dotnet-sdk-contrib/commit/c745edc1a2d1141b2ef41b7b661fdd68b764c57d))
* relative weights in fractional, fix injected props ([#208](https://github.com/open-feature/dotnet-sdk-contrib/issues/208)) ([7cccc8d](https://github.com/open-feature/dotnet-sdk-contrib/commit/7cccc8df0de6d9607e045fa62f070f35f20d6a0a))
* rename namespace, add OpenFeature dep ([#18](https://github.com/open-feature/dotnet-sdk-contrib/issues/18)) ([3ca3172](https://github.com/open-feature/dotnet-sdk-contrib/commit/3ca31722b83053d4edf2038889c78efa717a7cff))
* rename namespace, add OpenFeature dependency and readmes. ([3ca3172](https://github.com/open-feature/dotnet-sdk-contrib/commit/3ca31722b83053d4edf2038889c78efa717a7cff))
* Statsing provider ([#163](https://github.com/open-feature/dotnet-sdk-contrib/issues/163)) ([98028e9](https://github.com/open-feature/dotnet-sdk-contrib/commit/98028e9c37bce6225a1feeef09917a4539065a23))
* Support apiKey for GO Feature Flag relay proxy v1.7.0 ([#59](https://github.com/open-feature/dotnet-sdk-contrib/issues/59)) ([74eb627](https://github.com/open-feature/dotnet-sdk-contrib/commit/74eb627c28cd9c7cafc37e2ac735f43a35eca12b))
* support TLS connection in flagd provider ([#48](https://github.com/open-feature/dotnet-sdk-contrib/issues/48)) ([49e775a](https://github.com/open-feature/dotnet-sdk-contrib/commit/49e775a425b043e5774fbae348cfa2c4af59f2cf))
* unix socket support for flagd provider ([#42](https://github.com/open-feature/dotnet-sdk-contrib/issues/42)) ([9679fe4](https://github.com/open-feature/dotnet-sdk-contrib/commit/9679fe40cb13b48fa2f34521ce6175d9b8a6874b))
* Update in-process resolver to support flag metadata [#305](https://github.com/open-feature/dotnet-sdk-contrib/issues/305) ([#309](https://github.com/open-feature/dotnet-sdk-contrib/issues/309)) ([e603c08](https://github.com/open-feature/dotnet-sdk-contrib/commit/e603c08df7c19f360b2d8896caef3e3a5bcdcefd))
* use (and require) OpenFeature SDK v2 ([#262](https://github.com/open-feature/dotnet-sdk-contrib/issues/262)) ([f845134](https://github.com/open-feature/dotnet-sdk-contrib/commit/f84513438586457087ac47fd40629912f2ec473a))


### Bug Fixes

* allow SDK versions 0.5+ ([#21](https://github.com/open-feature/dotnet-sdk-contrib/issues/21)) ([831c10c](https://github.com/open-feature/dotnet-sdk-contrib/commit/831c10c8357c522f208a81a3d83ad44c01b15389))
* do not send targeting key as separate trait in flagsmith ([#120](https://github.com/open-feature/dotnet-sdk-contrib/issues/120)) ([0725f8f](https://github.com/open-feature/dotnet-sdk-contrib/commit/0725f8f3c726c05a6ccd2580f04b896f0aff4810))
* Fix Statsig nuget package name ([#172](https://github.com/open-feature/dotnet-sdk-contrib/issues/172)) ([3d089f5](https://github.com/open-feature/dotnet-sdk-contrib/commit/3d089f5c48478d7151fcf5964aa545471a0afe5c))
* Flagsmith provider no key exception ([#98](https://github.com/open-feature/dotnet-sdk-contrib/issues/98)) ([da84a17](https://github.com/open-feature/dotnet-sdk-contrib/commit/da84a177b574ac5779f3d85af836e426f47020e7))
* flagsmith release metadata ([#105](https://github.com/open-feature/dotnet-sdk-contrib/issues/105)) ([bd07b99](https://github.com/open-feature/dotnet-sdk-contrib/commit/bd07b9936099374af47c2d52127635a9d2cb980c))
* flagsmith release metadata ([#107](https://github.com/open-feature/dotnet-sdk-contrib/issues/107)) ([25fb39b](https://github.com/open-feature/dotnet-sdk-contrib/commit/25fb39bf3202b1393d831dadecb8cd4c965f4fc1))
* **flagsmith/tests:** Fix ValueError in FlagsmithProvider tests ([#186](https://github.com/open-feature/dotnet-sdk-contrib/issues/186)) ([2a80936](https://github.com/open-feature/dotnet-sdk-contrib/commit/2a8093601ade4ee6295a9788b7b8a1c00d372685))
* force a republish ([#298](https://github.com/open-feature/dotnet-sdk-contrib/issues/298)) ([ad01db2](https://github.com/open-feature/dotnet-sdk-contrib/commit/ad01db2991a147d527637afac30827f73a4cc40e))
* GoFeatureFlagUser class was not serialized. ([#33](https://github.com/open-feature/dotnet-sdk-contrib/issues/33)) ([0f222b4](https://github.com/open-feature/dotnet-sdk-contrib/commit/0f222b4a46d16bd075a9bbc3a512e3d8bf79bee4))
* in-process var name/value (FLAGD_RESOLVER="in-process") ([#206](https://github.com/open-feature/dotnet-sdk-contrib/issues/206)) ([1e580d7](https://github.com/open-feature/dotnet-sdk-contrib/commit/1e580d75a06f3d9f4683578e692247dbfc8aa7ea))
* migrate to System.Text.Json and JsonLogic ([#347](https://github.com/open-feature/dotnet-sdk-contrib/issues/347)) ([ef98686](https://github.com/open-feature/dotnet-sdk-contrib/commit/ef9868688f0804e26a1b69b6ea25be5f105c26b5))
* NET462 requires TLS for GRPC to work ([#72](https://github.com/open-feature/dotnet-sdk-contrib/issues/72)) ([2322f43](https://github.com/open-feature/dotnet-sdk-contrib/commit/2322f4319b4b44b66c6965e736551538b4ced9a1))
* provider status incorrect ([#187](https://github.com/open-feature/dotnet-sdk-contrib/issues/187)) ([6108d45](https://github.com/open-feature/dotnet-sdk-contrib/commit/6108d452d6c8a5c70c18b45ea9dd2e13612370ec))
* refactor OTEL metrics tests to make them more stable ([#212](https://github.com/open-feature/dotnet-sdk-contrib/issues/212)) ([24818e7](https://github.com/open-feature/dotnet-sdk-contrib/commit/24818e79613ca4d85ed52ab849e594fe6d060084))
* remove Microsoft.Extensions.Logging from flagd provider ([#233](https://github.com/open-feature/dotnet-sdk-contrib/issues/233)) ([7385735](https://github.com/open-feature/dotnet-sdk-contrib/commit/7385735aee328e60be323fba037291bf4fd3d1c9))
* Return correct name from FlagdProvider ([#126](https://github.com/open-feature/dotnet-sdk-contrib/issues/126)) ([0b704e9](https://github.com/open-feature/dotnet-sdk-contrib/commit/0b704e9662ab63fa164235aefa2013f0a9101857))
* Revise ConfigCat provider ([#280](https://github.com/open-feature/dotnet-sdk-contrib/issues/280)) ([0b2d5f2](https://github.com/open-feature/dotnet-sdk-contrib/commit/0b2d5f29490ad16ee5efde55d31354e0322c6f86))
* Setup config correct when passing a Uri (fixes [#71](https://github.com/open-feature/dotnet-sdk-contrib/issues/71)) ([#83](https://github.com/open-feature/dotnet-sdk-contrib/issues/83)) ([e27d351](https://github.com/open-feature/dotnet-sdk-contrib/commit/e27d351f7e3392102e2c7f840a0ab30e13198613))
* some issues in the GO Feature Flag relay proxy ([#45](https://github.com/open-feature/dotnet-sdk-contrib/issues/45)) ([9901ecc](https://github.com/open-feature/dotnet-sdk-contrib/commit/9901ecc6566f8e97b222ce2080d329d2adf4401f))
* typo in renovate configuration ([#46](https://github.com/open-feature/dotnet-sdk-contrib/issues/46)) ([9782458](https://github.com/open-feature/dotnet-sdk-contrib/commit/9782458a9e0df3164cf445cbec6ca1f8c773a5f8))
* update docs ([#300](https://github.com/open-feature/dotnet-sdk-contrib/issues/300)) ([50fd738](https://github.com/open-feature/dotnet-sdk-contrib/commit/50fd738585567a39f6fd0b1db37b899cbae42ba5))
* update flagd provider docs, publishing ([#39](https://github.com/open-feature/dotnet-sdk-contrib/issues/39)) ([7abdf5e](https://github.com/open-feature/dotnet-sdk-contrib/commit/7abdf5e979fe03b41ecf83e05c41ceb626941510))
* update Flagsmith dependencies ([#102](https://github.com/open-feature/dotnet-sdk-contrib/issues/102)) ([1c3b6ed](https://github.com/open-feature/dotnet-sdk-contrib/commit/1c3b6ed1f23c137e3703d8bcd710e5d180a5565d))
* update readme ([1aaa387](https://github.com/open-feature/dotnet-sdk-contrib/commit/1aaa3877ae3db884d401226b2138f8e3903a56c2))
* Update usage of StatsigServerOptions ([#169](https://github.com/open-feature/dotnet-sdk-contrib/issues/169)) ([d12bbc7](https://github.com/open-feature/dotnet-sdk-contrib/commit/d12bbc735eda7c2931d7f8d6ad32ef4f2f1741ed))
* Use new Statsig Api to return default value when flag is not defined ([#177](https://github.com/open-feature/dotnet-sdk-contrib/issues/177)) ([5efc8a6](https://github.com/open-feature/dotnet-sdk-contrib/commit/5efc8a603d1ad9d8887d75e38f95d5168a2319fa))
* use the TargetingKey property in the Flagsmith provider ([#227](https://github.com/open-feature/dotnet-sdk-contrib/issues/227)) ([5e175c8](https://github.com/open-feature/dotnet-sdk-contrib/commit/5e175c8695b90bfb285eb2b4e2aeacae2e533ce4))
* various in-process fixes, e2e tests ([#189](https://github.com/open-feature/dotnet-sdk-contrib/issues/189)) ([f2d096f](https://github.com/open-feature/dotnet-sdk-contrib/commit/f2d096fb4c1140a64a6d95bd17fd2efaf2320cda))
* version expression ([cad2cd1](https://github.com/open-feature/dotnet-sdk-contrib/commit/cad2cd166d0c25753b37189f044c3a585cda0fad))
* warnings in xunit tests due to unused theory params ([#297](https://github.com/open-feature/dotnet-sdk-contrib/issues/297)) ([3095caf](https://github.com/open-feature/dotnet-sdk-contrib/commit/3095cafd90c1828b3a678b110d200be1480e4070))


### Performance Improvements

* Cleanup allocations + missing ConfigureAwait's ([#124](https://github.com/open-feature/dotnet-sdk-contrib/issues/124)) ([e3d0f06](https://github.com/open-feature/dotnet-sdk-contrib/commit/e3d0f06c5fc732c068eb5d135143fac3c2a6b01e))
