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

namespace OpenFeature.Contrib.Providers.Flagsmith.Test
{
    public class UnitTestFlagsmithProvider
    {
        private static FlagsmithConfiguration GetDefaultFlagsmithConfiguration() => new ()
        {
            ApiUrl = "https://edge.api.flagsmith.com/api/v1/",
            EnvironmentKey = string.Empty,
            EnableClientSideEvaluation = false,
            EnvironmentRefreshIntervalSeconds = 60,
            EnableAnalytics = false,
            Retries = 1
        };
    [Fact]
        public void CreateFlagmithProvider_WithValidCredetials_CreatesInstanceSuccessfully()
        {
            // Arrange
            var config = GetDefaultFlagsmithConfiguration();

            // Act
            var flagsmithProvider = new FlagsmithProvider(config);


            // Assert
            Assert.NotNull(flagsmithProvider._flagsmithClient);
        }

        [Fact]
        public void CreateFlagmithProvider_WithValidCredetialsAndCustomHttpClient_CreatesInstanceSuccessfully()
        {
            // Arrange
            var config = GetDefaultFlagsmithConfiguration();
            using var httpClient = new HttpClient();
            // Act
            var flagsmithProvider = new FlagsmithProvider(config, httpClient);


            // Assert
            Assert.NotNull(flagsmithProvider._flagsmithClient);
        }

        [Fact]
        public async Task GetBooleanValue_ForEnabledFeatureWithValidFormat_ReturnCorrectValue()
        {
            // Arrange
            var flagsmithClient = Substitute.For<IFlagsmithClient>();
            var flags = Substitute.For<IFlags>();
            flags.GetFeatureValue("example-feature").Returns("true");
            flags.IsFeatureEnabled("example-feature").Returns(true);
            flagsmithClient.GetEnvironmentFlags().Returns(flags);

            var flagsmithProvider = new FlagsmithProvider(flagsmithClient);

            // Act
            var result = await flagsmithProvider.ResolveBooleanValue("example-feature", false);

            // Assert
            Assert.True(result.Value);
            Assert.Equal("example-feature", result.FlagKey);
            Assert.Null(result.Reason);
            Assert.Equal(ErrorType.None, result.ErrorType);
        }

        [Fact]
        public async Task GetBooleanValue_ForDisabledFeatureWithValidFormat_ReturnDefaultValue()
        {
            // Arrange
            var flagsmithClient = Substitute.For<IFlagsmithClient>();
            var flags = Substitute.For<IFlags>();
            flags.GetFeatureValue("example-feature").Returns("false");
            flags.IsFeatureEnabled("example-feature").Returns(false);
            flagsmithClient.GetEnvironmentFlags().Returns(flags);

            var flagsmithProvider = new FlagsmithProvider(flagsmithClient);

            // Act
            var result = await flagsmithProvider.ResolveBooleanValue("example-feature", true);

            // Assert
            Assert.True(result.Value);
            Assert.Equal("example-feature", result.FlagKey);
            Assert.Equal(Reason.Disabled, result.Reason);
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

            var flagsmithProvider = new FlagsmithProvider(flagsmithClient);

            // Act and Assert
            await Assert.ThrowsAsync<TypeMismatchException>( () =>  flagsmithProvider.ResolveBooleanValue("example-feature", true));

        }


        [Fact]
        public async Task GetDoubleValue_ForEnabledFeatureWithValidFormat_ReturnCorrectValue()
        {
            // Arrange
            var flagsmithClient = Substitute.For<IFlagsmithClient>();
            var flags = Substitute.For<IFlags>();
            flags.GetFeatureValue("example-feature").Returns("32,334");
            flags.IsFeatureEnabled("example-feature").Returns(true);
            flagsmithClient.GetEnvironmentFlags().Returns(flags);

            var flagsmithProvider = new FlagsmithProvider(flagsmithClient);

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

            var flagsmithProvider = new FlagsmithProvider(flagsmithClient);

            // Act
            var result = await flagsmithProvider.ResolveDoubleValue("example-feature", -32.22);

            // Assert
            Assert.Equal(-32.22, result.Value);
            Assert.Equal("example-feature", result.FlagKey);
            Assert.Equal(Reason.Disabled, result.Reason);
            Assert.Equal(ErrorType.None, result.ErrorType);
        }

        [Fact]
        public async Task GetDoubleValue_ForEnabledFeatureWithWrongFormatValue_ReturnDefaultValue()
        {
            // Arrange
            var flagsmithClient = Substitute.For<IFlagsmithClient>();
            var flags = Substitute.For<IFlags>();
            flags.GetFeatureValue("example-feature").Returns("hreni");
            flags.IsFeatureEnabled("example-feature").Returns(true);
            flagsmithClient.GetEnvironmentFlags().Returns(flags);

            var flagsmithProvider = new FlagsmithProvider(flagsmithClient);

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

            var flagsmithProvider = new FlagsmithProvider(flagsmithClient);

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

            var flagsmithProvider = new FlagsmithProvider(flagsmithClient);

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

            var flagsmithProvider = new FlagsmithProvider(flagsmithClient);

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

            var flagsmithProvider = new FlagsmithProvider(flagsmithClient);

            // Act
            var result = await flagsmithProvider.ResolveIntegerValue("example-feature", -32);

            // Assert
            Assert.Equal(-32, result.Value);
            Assert.Equal("example-feature", result.FlagKey);
            Assert.Equal(Reason.Disabled, result.Reason);
            Assert.Equal(ErrorType.None, result.ErrorType);
        }

        [Fact]
        public async Task GetIntValue_ForEnabledFeatureWithWrongFormatValue_ReturnDefaultValue()
        {
            // Arrange
            var flagsmithClient = Substitute.For<IFlagsmithClient>();
            var flags = Substitute.For<IFlags>();
            flags.GetFeatureValue("example-feature").Returns("hreni");
            flags.IsFeatureEnabled("example-feature").Returns(true);
            flagsmithClient.GetEnvironmentFlags().Returns(flags);

            var flagsmithProvider = new FlagsmithProvider(flagsmithClient);

            // Act and Assert
            await Assert.ThrowsAsync<TypeMismatchException>(() => flagsmithProvider.ResolveIntegerValue("example-feature", 2222));
        }

        [Fact]
        public async Task GetStructureValue_ForEnabledFeatureWithValidFormat_ReturnCorrectValue()
        {
            // Arrange
            var flagsmithClient = Substitute.For<IFlagsmithClient>();
            var flags = Substitute.For<IFlags>();
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
            flags.GetFeatureValue("example-feature").Returns(expectedValue);
            flags.IsFeatureEnabled("example-feature").Returns(true);
            flagsmithClient.GetEnvironmentFlags().Returns(flags);

            var flagsmithProvider = new FlagsmithProvider(flagsmithClient);

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
            var flagsmithProvider = new FlagsmithProvider(flagsmithClient);

            // Act
            var result = await flagsmithProvider.ResolveStructureValue("example-feature", defaultObject);

            // Assert
            Assert.Equal(defaultObject, result.Value);
            Assert.Equal("example-feature", result.FlagKey);
            Assert.Equal(Reason.Disabled, result.Reason);
            Assert.Equal(ErrorType.None, result.ErrorType);
        }

        [Fact]
        public async Task GetStructureValue_ForEnabledFeatureWithWrongFormatValue_ReturnDefaultValue()
        {
            // Arrange
            var flagsmithClient = Substitute.For<IFlagsmithClient>();
            var flags = Substitute.For<IFlags>();
            flags.GetFeatureValue("example-feature").Returns("hreni");
            flags.IsFeatureEnabled("example-feature").Returns(true);
            flagsmithClient.GetEnvironmentFlags().Returns(flags);

            var defaultObject = new Value("default");
            var flagsmithProvider = new FlagsmithProvider(flagsmithClient);

            // Act and Assert
            await Assert.ThrowsAsync<TypeMismatchException>(() => flagsmithProvider.ResolveStructureValue("example-feature", defaultObject));
        }
    }

    public class ExampleConfig
    {
        public string ExampleText { get; set; }
        public int ExampleInt {  get; set; }
    }

}
