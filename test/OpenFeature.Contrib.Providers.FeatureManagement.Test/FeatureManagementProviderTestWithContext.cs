using Microsoft.Extensions.Configuration;
using OpenFeature.Model;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace OpenFeature.Contrib.Providers.FeatureManagement.Test
{
    public class FeatureManagementProviderTestWithContext
    {
        private EvaluationContext BuildEvaluationContext(string userId, string group)
        {
            Value userIdValue = new Value(userId);

            IList<Value> groups = new List<Value> { new Value(group) };
            Value groupsValue = new Value(groups);

            return EvaluationContext.Builder()
                .Set("UserId", userIdValue)
                .Set("Groups", groupsValue)
                .Build();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task MissingFlagKey_ShouldReturnDefault(bool defaultValue)
        {
            // Arrange
            var evaluationContext = BuildEvaluationContext("test.user@openfeature.dev", "test.group");

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.targeting.json")
                .Build();

            var provider = new FeatureManagementProvider(configuration);

            // Act
            var result = await provider.ResolveBooleanValue("MissingFlagKey", defaultValue, evaluationContext);

            // Assert
            Assert.Equal(defaultValue, result.Value);
        }

        [Theory]
        [InlineData("Flag_Boolean_TargetingUserId", "test.user@openfeature.dev", "test.group", true)]
        [InlineData("Flag_Boolean_TargetingUserId", "missing.user@openfeature.dev", "missing.group", false)]
        [InlineData("Flag_Boolean_TargetingGroup", "test.user@openfeature.dev", "test.group", true)]
        [InlineData("Flag_Boolean_TargetingGroup", "missing.user@openfeature.dev", "missing.group", false)]
        public async Task BooleanValue_ShouldReturnExpected(string key, string userId, string group, bool expected)
        {
            // Arrange
            var evaluationContext = BuildEvaluationContext(userId, group);
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.targeting.json")
                .Build();

            var provider = new FeatureManagementProvider(configuration);

            // Act
            // Invert the expected value to ensure that the value is being read from the configuration
            var result = await provider.ResolveBooleanValue(key, !expected, evaluationContext);

            // Assert
            Assert.Equal(expected, result.Value);
        }

        [Theory]
        [InlineData("Flag_Double_TargetingUserId", "test.user@openfeature.dev", "test.group", 1.0)]
        [InlineData("Flag_Double_TargetingUserId", "missing.user@openfeature.dev", "missing.group", -1.0)]
        [InlineData("Flag_Double_TargetingGroup", "test.user@openfeature.dev", "test.group", 1.0)]
        [InlineData("Flag_Double_TargetingGroup", "missing.user@openfeature.dev", "missing.group", -1.0)]
        public async Task DoubleValue_ShouldReturnExpected(string key, string userId, string group, double expected)
        {
            // Arrange
            var evaluationContext = BuildEvaluationContext(userId, group);
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.targeting.json")
                .Build();
            var provider = new FeatureManagementProvider(configuration);

            // Act
            // Using 0.0 for the default to verify the value is being read from the configuration
            var result = await provider.ResolveDoubleValue(key, 0.0f, evaluationContext);

            // Assert
            Assert.Equal(expected, result.Value);
        }

        [Theory]
        [InlineData("Flag_Integer_TargetingUserId", "test.user@openfeature.dev", "test.group", 1.0)]
        [InlineData("Flag_Integer_TargetingUserId", "missing.user@openfeature.dev", "missing.group", -1.0)]
        [InlineData("Flag_Integer_TargetingGroup", "test.user@openfeature.dev", "test.group", 1.0)]
        [InlineData("Flag_Integer_TargetingGroup", "missing.user@openfeature.dev", "missing.group", -1.0)]
        public async Task IntegerValue_ShouldReturnExpected(string key, string userId, string group, int expected)
        {
            // Arrange
            var evaluationContext = BuildEvaluationContext(userId, group);
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.targeting.json")
                .Build();
            var provider = new FeatureManagementProvider(configuration);

            // Act
            // Using 0 for the default to verify the value is being read from the configuration
            var result = await provider.ResolveIntegerValue(key, 0, evaluationContext);

            // Assert
            Assert.Equal(expected, result.Value);
        }

        [Theory]
        [InlineData("Flag_String_TargetingUserId", "test.user@openfeature.dev", "test.group", "FlagEnabled")]
        [InlineData("Flag_String_TargetingUserId", "missing.user@openfeature.dev", "missing.group", "FlagDisabled")]
        [InlineData("Flag_String_TargetingGroup", "test.user@openfeature.dev", "test.group", "FlagEnabled")]
        [InlineData("Flag_String_TargetingGroup", "missing.user@openfeature.dev", "missing.group", "FlagDisabled")]
        public async Task StringValue_ShouldReturnExpected(string key, string userId, string group, string expected)
        {
            // Arrange
            var evaluationContext = BuildEvaluationContext(userId, group);
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.targeting.json")
                .Build();
            var provider = new FeatureManagementProvider(configuration);

            // Act
            // Using 0 for the default to verify the value is being read from the configuration
            var result = await provider.ResolveStringValue(key, "DefaultValue", evaluationContext);

            // Assert
            Assert.Equal(expected, result.Value);
        }

        [Theory]
        [InlineData("Flag_Structure_TargetingUserId", "test.user@openfeature.dev", "test.group")]
        [InlineData("Flag_Structure_TargetingGroup", "test.user@openfeature.dev", "test.group")]
        public async Task StructureValue_ShouldReturnExpected(string key, string userId, string group)
        {
            // Arrange
            var evaluationContext = BuildEvaluationContext(userId, group);
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.targeting.json")
                .Build();
            var provider = new FeatureManagementProvider(configuration);

            // Act
            // Using 0 for the default to verify the value is being read from the configuration
            var result = await provider.ResolveStructureValue(key, null, evaluationContext);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.True(result.Value.IsStructure);
            Assert.Equal(2, result.Value.AsStructure.Count);
        }
    }
}
