using Xunit;
using System;
using Flagsmith;
using System.Net.Http;
using NSubstitute;
using System.Threading.Tasks;
using OpenFeature.Constant;
using OpenFeature.Model;
using System.Linq;
using OpenFeature.Error;
using System.Collections.Generic;

namespace OpenFeature.Contrib.Providers.Flagsmith.Test
{
    public class UnitTestFlagsmithProvider
    {
        private static FlagsmithConfiguration GetDefaultFlagsmithConfiguration() => new()
        {
            ApiUrl = "https://edge.api.flagsmith.com/api/v1/",
            EnvironmentKey = "some-key",
            EnableClientSideEvaluation = false,
            EnvironmentRefreshIntervalSeconds = 60,
            EnableAnalytics = false,
            Retries = 1
        };

        private static FlagsmithProviderConfiguration GetDefaultFlagsmithProviderConfigurationConfiguration() => new();

        [Fact]
        public void CreateFlagmithProvider_WithValidCredentials_CreatesProviderInstanceSuccessfully()
        {
            // Arrange
            var config = GetDefaultFlagsmithConfiguration();
            var providerConfig = GetDefaultFlagsmithProviderConfigurationConfiguration();

            // Act
            var flagsmithProvider = new FlagsmithProvider(providerConfig, config);


            // Assert
            Assert.NotNull(flagsmithProvider._flagsmithClient);
        }

        [Fact]
        public void CreateFlagmithProvider_WithValidCredentialsAndCustomHttpClient_CreatesProviderInstanceSuccessfully()
        {
            // Arrange
            var config = GetDefaultFlagsmithConfiguration();
            var providerConfig = GetDefaultFlagsmithProviderConfigurationConfiguration();
            using var httpClient = new HttpClient();
            // Act
            var flagsmithProvider = new FlagsmithProvider(providerConfig, config, httpClient);


            // Assert
            Assert.NotNull(flagsmithProvider._flagsmithClient);
        }

        [Fact]
        public async Task GetValue_ForEnabledFeatureWithEvaluationContext_ReturnCorrectValue()
        {
            // Arrange
            var flagsmithClient = Substitute.For<IFlagsmithClient>();
            var flags = Substitute.For<IFlags>();
            var date = DateTime.Now;
            flags.GetFeatureValue("example-feature").Returns("true");
            flags.IsFeatureEnabled("example-feature").Returns(true);
            flagsmithClient.GetIdentityFlags("233", Arg.Is<List<ITrait>>(x => x.Count == 6 && x.Any(c => c.GetTraitKey() == "key1"))).Returns(flags);

            var providerConfig = GetDefaultFlagsmithProviderConfigurationConfiguration();
            var flagsmithProvider = new FlagsmithProvider(providerConfig, flagsmithClient);

            var contextBuilder = EvaluationContext.Builder()
                .Set("key1", "value")
                .Set("key2", 1)
                .Set("key3", true)
                .Set("key4", date)
                .Set("key5", Structure.Empty)
                .Set("key6", 1.0)
                .Set(FlagsmithProviderConfiguration.DefaultTargetingKey, "233");
            // Act
            var result = await flagsmithProvider.ResolveBooleanValue("example-feature", false, contextBuilder.Build());

            // Assert
            Assert.True(result.Value);
            Assert.Equal("example-feature", result.FlagKey);
            Assert.Null(result.Reason);
            Assert.Equal(ErrorType.None, result.ErrorType);
        }


        [Theory]
        [InlineData(true, true, "true", true, null, true)]
        [InlineData(false, true, "true", true, null, true)]
        [InlineData(true, false, "true", true, null, true)]
        [InlineData(false, false, "true", true, null, true)]
        [InlineData(true, true, "false", true, null, false)]
        [InlineData(false, true, "false", true, null, false)]
        [InlineData(true, false, "false", true, null, true)]
        [InlineData(false, false, "false", true, null, true)]

        [InlineData(true, true, "true", false, "DISABLED", true)]
        [InlineData(false, true, "true", false, "DISABLED", false)]
        [InlineData(true, false, "true", false, null, false)]
        [InlineData(false, false, "true", false, null, false)]
        [InlineData(true, true, "false", false, "DISABLED", true)]
        [InlineData(false, true, "false", false, "DISABLED", false)]
        [InlineData(true, false, "false", false, null, false)]
        [InlineData(false, false, "false", false, null, false)]
        public async Task GetBooleanValue_ForEnabledFeatureWithValidFormatAndSettedConfigValue_ReturnExpectedResult(
            bool defaultValue,
            bool enabledValueConfig,
            string settedValue,
            bool featureEnabled,
            string expectedReason,
            bool expectedResult)
        {
            // Arrange
            var flagsmithClient = Substitute.For<IFlagsmithClient>();
            var flags = Substitute.For<IFlags>();
            flags.GetFeatureValue("example-feature").Returns(settedValue);
            flags.IsFeatureEnabled("example-feature").Returns(featureEnabled);
            flagsmithClient.GetEnvironmentFlags().Returns(flags);
            var providerConfig = GetDefaultFlagsmithProviderConfigurationConfiguration();
            providerConfig.UsingBooleanConfigValue = enabledValueConfig;

            var flagsmithProvider = new FlagsmithProvider(providerConfig, flagsmithClient);

            // Act
            var result = await flagsmithProvider.ResolveBooleanValue("example-feature", defaultValue);

            // Assert
            Assert.Equal(expectedResult, result.Value);
            Assert.Equal("example-feature", result.FlagKey);
            Assert.Equal(expectedReason, result.Reason);
            Assert.Equal(ErrorType.None, result.ErrorType);
        }

        [Fact]
        public async Task GetBooleanValue_ForEnabledFeatureWithWrongFormatValue_ThrowsTypeMismatch()
        {
            // Arrange
            var flagsmithClient = Substitute.For<IFlagsmithClient>();
            var flags = Substitute.For<IFlags>();
            flags.GetFeatureValue("example-feature").Returns("hreni");
            flags.IsFeatureEnabled("example-feature").Returns(true);
            flagsmithClient.GetEnvironmentFlags().Returns(flags);

            var providerConfig = GetDefaultFlagsmithProviderConfigurationConfiguration();
            providerConfig.UsingBooleanConfigValue = true;

            var flagsmithProvider = new FlagsmithProvider(providerConfig, flagsmithClient);

            // Act and Assert
            await Assert.ThrowsAsync<TypeMismatchException>(() => flagsmithProvider.ResolveBooleanValue("example-feature", true));
        }


        [Fact]
        public async Task GetDoubleValue_ForEnabledFeatureWithValidFormat_ReturnCorrectValue()
        {
            // Arrange
            var flagsmithClient = Substitute.For<IFlagsmithClient>();
            var flags = Substitute.For<IFlags>();
            flags.GetFeatureValue("example-feature").Returns("32.334");
            flags.IsFeatureEnabled("example-feature").Returns(true);
            flagsmithClient.GetEnvironmentFlags().Returns(flags);

            var providerConfig = GetDefaultFlagsmithProviderConfigurationConfiguration();

            var flagsmithProvider = new FlagsmithProvider(providerConfig, flagsmithClient);

            // Act
            var result = await flagsmithProvider.ResolveDoubleValue("example-feature", 32.22);

            // Assert
            Assert.Equal(32.334, result.Value);
            Assert.Equal("example-feature", result.FlagKey);
            Assert.Null(result.Reason);
            Assert.Equal(ErrorType.None, result.ErrorType);
        }


        [Fact]
        public async Task GetDoubleValue_ForDisabledFeatureWithValidFormat_ReturnDefaultValue()
        {
            // Arrange
            var flagsmithClient = Substitute.For<IFlagsmithClient>();
            var flags = Substitute.For<IFlags>();
            flags.GetFeatureValue("example-feature").Returns("4112");
            flags.IsFeatureEnabled("example-feature").Returns(false);
            flagsmithClient.GetEnvironmentFlags().Returns(flags);
            var providerConfig = GetDefaultFlagsmithProviderConfigurationConfiguration();

            var flagsmithProvider = new FlagsmithProvider(providerConfig, flagsmithClient);

            // Act
            var result = await flagsmithProvider.ResolveDoubleValue("example-feature", -32.22);

            // Assert
            Assert.Equal(-32.22, result.Value);
            Assert.Equal("example-feature", result.FlagKey);
            Assert.Equal(Reason.Disabled, result.Reason);
            Assert.Equal(ErrorType.None, result.ErrorType);
        }

        [Fact]
        public async Task GetDoubleValue_ForEnabledFeatureWithWrongFormatValue_ThrowsTypeMismatch()
        {
            // Arrange
            var flagsmithClient = Substitute.For<IFlagsmithClient>();
            var flags = Substitute.For<IFlags>();
            flags.GetFeatureValue("example-feature").Returns("hreni");
            flags.IsFeatureEnabled("example-feature").Returns(true);
            flagsmithClient.GetEnvironmentFlags().Returns(flags);
            var providerConfig = GetDefaultFlagsmithProviderConfigurationConfiguration();

            var flagsmithProvider = new FlagsmithProvider(providerConfig, flagsmithClient);

            // Act and Assert
            await Assert.ThrowsAsync<TypeMismatchException>(() => flagsmithProvider.ResolveDoubleValue("example-feature", 2222.22133));
        }



        [Fact]
        public async Task GetStringValue_ForEnabledFeatureWithValidFormat_ReturnCorrectValue()
        {
            // Arrange
            var flagsmithClient = Substitute.For<IFlagsmithClient>();
            var flags = Substitute.For<IFlags>();
            flags.GetFeatureValue("example-feature").Returns("example");
            flags.IsFeatureEnabled("example-feature").Returns(true);
            flagsmithClient.GetEnvironmentFlags().Returns(flags);
            var providerConfig = GetDefaultFlagsmithProviderConfigurationConfiguration();

            var flagsmithProvider = new FlagsmithProvider(providerConfig, flagsmithClient);

            // Act
            var result = await flagsmithProvider.ResolveStringValue("example-feature", "example");

            // Assert
            Assert.Equal("example", result.Value);
            Assert.Equal("example-feature", result.FlagKey);
            Assert.Null(result.Reason);
            Assert.Equal(ErrorType.None, result.ErrorType);
        }


        [Fact]
        public async Task GetStringValue_ForDisabledFeatureWithValidFormat_ReturnDefaultValue()
        {
            // Arrange
            var flagsmithClient = Substitute.For<IFlagsmithClient>();
            var flags = Substitute.For<IFlags>();
            flags.GetFeatureValue("example-feature").Returns("4112");
            flags.IsFeatureEnabled("example-feature").Returns(false);
            flagsmithClient.GetEnvironmentFlags().Returns(flags);
            var providerConfig = GetDefaultFlagsmithProviderConfigurationConfiguration();

            var flagsmithProvider = new FlagsmithProvider(providerConfig, flagsmithClient);

            // Act
            var result = await flagsmithProvider.ResolveStringValue("example-feature", "3333a");

            // Assert
            Assert.Equal("3333a", result.Value);
            Assert.Equal("example-feature", result.FlagKey);
            Assert.Equal(Reason.Disabled, result.Reason);
            Assert.Equal(ErrorType.None, result.ErrorType);
        }


        [Fact]
        public async Task GetIntValue_ForEnabledFeatureWithValidFormat_ReturnCorrectValue()
        {
            // Arrange
            var flagsmithClient = Substitute.For<IFlagsmithClient>();
            var flags = Substitute.For<IFlags>();
            flags.GetFeatureValue("example-feature").Returns("232");
            flags.IsFeatureEnabled("example-feature").Returns(true);
            flagsmithClient.GetEnvironmentFlags().Returns(flags);
            var providerConfig = GetDefaultFlagsmithProviderConfigurationConfiguration();

            var flagsmithProvider = new FlagsmithProvider(providerConfig, flagsmithClient);

            // Act
            var result = await flagsmithProvider.ResolveIntegerValue("example-feature", 32);

            // Assert
            Assert.Equal(232, result.Value);
            Assert.Equal("example-feature", result.FlagKey);
            Assert.Null(result.Reason);
            Assert.Equal(ErrorType.None, result.ErrorType);
        }

        [Fact]
        public async Task GetIntValue_ForDisabledFeatureWithValidFormat_ReturnDefaultValue()
        {
            // Arrange
            var flagsmithClient = Substitute.For<IFlagsmithClient>();
            var flags = Substitute.For<IFlags>();
            flags.GetFeatureValue("example-feature").Returns("4112");
            flags.IsFeatureEnabled("example-feature").Returns(false);
            flagsmithClient.GetEnvironmentFlags().Returns(flags);
            var providerConfig = GetDefaultFlagsmithProviderConfigurationConfiguration();

            var flagsmithProvider = new FlagsmithProvider(providerConfig, flagsmithClient);

            // Act
            var result = await flagsmithProvider.ResolveIntegerValue("example-feature", -32);

            // Assert
            Assert.Equal(-32, result.Value);
            Assert.Equal("example-feature", result.FlagKey);
            Assert.Equal(Reason.Disabled, result.Reason);
            Assert.Equal(ErrorType.None, result.ErrorType);
        }

        [Fact]
        public async Task GetIntValue_ForEnabledFeatureWithWrongFormatValue_ThrowsTypeMismatch()
        {
            // Arrange
            var flagsmithClient = Substitute.For<IFlagsmithClient>();
            var flags = Substitute.For<IFlags>();
            flags.GetFeatureValue("example-feature").Returns("hreni");
            flags.IsFeatureEnabled("example-feature").Returns(true);
            flagsmithClient.GetEnvironmentFlags().Returns(flags);
            var providerConfig = GetDefaultFlagsmithProviderConfigurationConfiguration();

            var flagsmithProvider = new FlagsmithProvider(providerConfig, flagsmithClient);

            // Act and Assert
            await Assert.ThrowsAsync<TypeMismatchException>(() => flagsmithProvider.ResolveIntegerValue("example-feature", 2222));
        }

        [Fact]
        public async Task GetStructureValue_ForEnabledFeatureWithValidFormat_ReturnCorrectValue()
        {
            // Arrange
            var flagsmithClient = Substitute.For<IFlagsmithClient>();
            var flags = Substitute.For<IFlags>();

            #pragma warning disable format
            var expectedValue =
                """
                {
                    "glossary": {
                    "title": "example glossary",
                    "GlossDiv": {
                      "title": "S",
                      "GlossList": {
                        "GlossEntry": {
                          "ID": "SGML",
                          "SortAs": "SGML",
                          "GlossTerm": "Standard Generalized Markup Language",
                          "Acronym": "SGML",
                          "Abbrev": "ISO 8879:1986",
                          "GlossDef": {
                            "para": "A meta-markup language, used to create markup languages such as DocBook.",
                            "GlossSeeAlso": [
                              "GML",
                              "XML"
                            ]
                          },
                          "GlossSee": "markup"
                        }
                      }
                    }
                  }
                }
                """;
            #pragma warning restore format

            flags.GetFeatureValue("example-feature").Returns(expectedValue);
            flags.IsFeatureEnabled("example-feature").Returns(true);
            flagsmithClient.GetEnvironmentFlags().Returns(flags);
            var providerConfig = GetDefaultFlagsmithProviderConfigurationConfiguration();

            var flagsmithProvider = new FlagsmithProvider(providerConfig, flagsmithClient);

            // Act
            var defaultObject = new Value(Structure.Empty);

            var result = await flagsmithProvider.ResolveStructureValue("example-feature", defaultObject);

            // Assert
            var glossary = result.Value.AsStructure.GetValue("glossary");
            Assert.True(glossary.IsStructure);
            Assert.Equal("example glossary", glossary.AsStructure.GetValue("title").AsString);
            var glossDiv = glossary.AsStructure.GetValue("GlossDiv");
            Assert.True(glossDiv.IsStructure);
            var glossList = glossDiv.AsStructure.GetValue("GlossList");
            Assert.True(glossList.IsStructure);
            var glossEntry = glossList.AsStructure.GetValue("GlossEntry");
            Assert.True(glossEntry.IsStructure);
            Assert.Equal("SGML", glossEntry.AsStructure.GetValue("SortAs").AsString);
            var glossDef = glossEntry.AsStructure.GetValue("GlossDef");
            Assert.True(glossDef.IsStructure);
            var glossSeeAlso = glossDef.AsStructure.GetValue("GlossSeeAlso");
            Assert.True(glossSeeAlso.IsList);
            Assert.Equal(2, glossSeeAlso.AsList.Count);
            Assert.Equal("GML", glossSeeAlso.AsList.First().AsString);
            Assert.Equal("XML", glossSeeAlso.AsList.Last().AsString);

            Assert.Equal("example-feature", result.FlagKey);
            Assert.Null(result.Reason);
            Assert.Equal(ErrorType.None, result.ErrorType);
        }

        [Fact]
        public async Task GetStructureValue_ForDisabledFeatureWithValidFormat_ReturnDefaultValue()
        {
            // Arrange
            var flagsmithClient = Substitute.For<IFlagsmithClient>();
            var flags = Substitute.For<IFlags>();
            flags.GetFeatureValue("example-feature").Returns("4112");
            flags.IsFeatureEnabled("example-feature").Returns(false);
            flagsmithClient.GetEnvironmentFlags().Returns(flags);

            var defaultObject = new Value("default");
            var providerConfig = GetDefaultFlagsmithProviderConfigurationConfiguration();
            var flagsmithProvider = new FlagsmithProvider(providerConfig, flagsmithClient);

            // Act
            var result = await flagsmithProvider.ResolveStructureValue("example-feature", defaultObject);

            // Assert
            Assert.Equal(defaultObject, result.Value);
            Assert.Equal("example-feature", result.FlagKey);
            Assert.Equal(Reason.Disabled, result.Reason);
            Assert.Equal(ErrorType.None, result.ErrorType);
        }

        [Fact]
        public async Task GetStructureValue_ForEnabledFeatureWithWrongFormatValue_ThrowsTypeMismatch()
        {
            // Arrange
            var flagsmithClient = Substitute.For<IFlagsmithClient>();
            var flags = Substitute.For<IFlags>();
            flags.GetFeatureValue("example-feature").Returns("hreni");
            flags.IsFeatureEnabled("example-feature").Returns(true);
            flagsmithClient.GetEnvironmentFlags().Returns(flags);

            var defaultObject = new Value("default");
            var providerConfig = GetDefaultFlagsmithProviderConfigurationConfiguration();
            var flagsmithProvider = new FlagsmithProvider(providerConfig, flagsmithClient);

            // Act and Assert
            await Assert.ThrowsAsync<TypeMismatchException>(() => flagsmithProvider.ResolveStructureValue("example-feature", defaultObject));
        }
    }
}
