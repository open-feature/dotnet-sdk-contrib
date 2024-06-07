using AutoFixture.Xunit2;
using OpenFeature.Error;
using OpenFeature.Model;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace OpenFeature.Contrib.Providers.Statsig.Test
{
    public class EvaluationContextExtensionsTests
    {
        [Theory]
        [AutoData]
        public void AsStatsigUser_ShouldMapUserIdSuccessfully(string userId)
        {
            // Arrange
            var evaluationContext = EvaluationContext.Builder().SetTargetingKey(userId).Build();

            // Act
            var statsigUser = evaluationContext.AsStatsigUser();

            // Assert
            Assert.NotNull(statsigUser);
            Assert.Equal(userId, statsigUser.UserID);
        }

        [Theory]
        [AutoData]
        public void AsStatsigUser_ShouldMapString(string key, string value)
        {
            // Arrange
            var evaluationContext = EvaluationContext.Builder().Set(key, value).Build();

            // Act
            var statsigUser = evaluationContext.AsStatsigUser();

            // Assert
            Assert.NotNull(statsigUser);
            Assert.True(statsigUser.CustomProperties.TryGetValue(key, out var mappedValue));
            Assert.Equal(value, mappedValue as string);
        }


        [Theory]
        [AutoData]
        public void AsStatsigUser_ShouldMapStatsigProperties(string appVersion, string country, string email, string ipAddress, string locale, string userAgent)
        {
            // Arrange
            var evaluationContext = EvaluationContext.Builder()
                .Set(EvaluationContextExtensions.CONTEXT_APP_VERSION, appVersion)
                .Set(EvaluationContextExtensions.CONTEXT_COUNTRY, country)
                .Set(EvaluationContextExtensions.CONTEXT_EMAIL, email)
                .Set(EvaluationContextExtensions.CONTEXT_IP, ipAddress)
                .Set(EvaluationContextExtensions.CONTEXT_LOCALE, locale)
                .Set(EvaluationContextExtensions.CONTEXT_USER_AGENT, userAgent).Build();

            // Act
            var statsigUser = evaluationContext.AsStatsigUser();

            // Assert
            Assert.NotNull(statsigUser);
            Assert.Equal(statsigUser.AppVersion, appVersion);
            Assert.Equal(statsigUser.Country, country);
            Assert.Equal(statsigUser.Email, email);
            Assert.Equal(statsigUser.IPAddress, ipAddress);
            Assert.Equal(statsigUser.Locale, locale);
            Assert.Equal(statsigUser.UserAgent, userAgent);
        }

        [Theory]
        [AutoData]
        public void AsStatsigUser_ShouldMapPrivateData(string key, string value)
        {
            var privateProperties = new Dictionary<string, Value>() { { key, new Value(value) } };

            // Arrange
            var evaluationContext = EvaluationContext.Builder().Set(EvaluationContextExtensions.CONTEXT_PRIVATE_ATTRIBUTES, new Structure(privateProperties)).Build();

            // Act
            var statsigUser = evaluationContext.AsStatsigUser();

            // Assert
            Assert.NotNull(statsigUser);
            Assert.True(statsigUser.PrivateAttributes.TryGetValue(key, out var mappedValue));
            Assert.Equal(value, mappedValue);
        }

        [Theory]
        [AutoData]
        public void AsStatsigUser_ShouldMapCustomIds(Dictionary<string, string> customIdKeyValues)
        {

            // Arrange
            var customIdStructure = new Structure(customIdKeyValues.ToDictionary(kvp => kvp.Key, kvp => new Value(kvp.Value)));
            var evaluationContext = EvaluationContext.Builder().Set(EvaluationContextExtensions.CONTEXT_CUSTOM_IDS, customIdStructure).Build();

            // Act
            var statsigUser = evaluationContext.AsStatsigUser();

            // Assert
            Assert.NotNull(statsigUser);
            var result = statsigUser.CustomIDs.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            Assert.Equal(customIdKeyValues, result);
        }

        [Theory]
        [AutoData]
        public void AsStatsigUser_ShouldFailOnNonStringCustomId(Dictionary<string, int> customIdKeyValues)
        {
            // Arrange
            var customIdStructure = new Structure(customIdKeyValues.ToDictionary(kvp => kvp.Key, kvp => new Value(kvp.Value)));
            var evaluationContext = EvaluationContext.Builder().Set(EvaluationContextExtensions.CONTEXT_CUSTOM_IDS, customIdStructure).Build();

            // Act and Assert
            Assert.Throws<FeatureProviderException>(evaluationContext.AsStatsigUser);
        }

        [Fact]
        public void AsStatsigUser_ShouldHandleNullEvaluationContext()
        {
            // Arrange
            EvaluationContext evaluationContext = null;

            // Act
            var statsigUser = evaluationContext.AsStatsigUser();

            // Assert
            Assert.Null(statsigUser);
        }

        [Fact]
        public void AsStatsigUser_ShouldHandleEmptyUserID()
        {
            // Arrange
            var evaluationContext = EvaluationContext.Builder().SetTargetingKey("").Build();

            // Act
            var statsigUser = evaluationContext.AsStatsigUser();

            // Assert
            Assert.NotNull(statsigUser);
            Assert.Equal("", statsigUser.UserID);
        }
    }
}