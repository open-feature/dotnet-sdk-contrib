using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement.FeatureFilters;
using OpenFeature.Model;
using Xunit;

namespace OpenFeature.Contrib.Providers.FeatureManagement.Test;

public class FeatureManagementProviderTestWithContext
{
    private EvaluationContext BuildEvaluationContext(string userId, string group)
    {
        Value userIdValue = new Value(userId);

        IList<Value> groups = new List<Value> { new Value(group) };
        Value groupsValue = new Value(groups);

        return EvaluationContext.Builder()
            .Set(nameof(TargetingContext.UserId), userIdValue)
            .Set(nameof(TargetingContext.Groups), groupsValue)
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
        var result = await provider.ResolveBooleanValueAsync("MissingFlagKey", defaultValue, evaluationContext);

        // Assert
        Assert.Equal(defaultValue, result.Value);
    }

    [Theory]
    [MemberData(nameof(TestData.BooleanWithContext), MemberType = typeof(TestData))]
    public async Task BooleanValue_ShouldReturnExpected(string key, string userId, string group, bool defaultValue, bool expectedValue)
    {
        // Arrange
        var evaluationContext = BuildEvaluationContext(userId, group);
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.targeting.json")
            .Build();

        var provider = new FeatureManagementProvider(configuration);

        // Act
        var result = await provider.ResolveBooleanValueAsync(key, defaultValue, evaluationContext);

        // Assert
        Assert.Equal(expectedValue, result.Value);
    }

    [Theory]
    [MemberData(nameof(TestData.DoubleWithContext), MemberType = typeof(TestData))]
    public async Task DoubleValue_ShouldReturnExpected(string key, string userId, string group, double defaultValue, double expectedValue)
    {
        // Arrange
        var evaluationContext = BuildEvaluationContext(userId, group);
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.targeting.json")
            .Build();
        var provider = new FeatureManagementProvider(configuration);

        // Act
        // Using 0.0 for the default to verify the value is being read from the configuration
        var result = await provider.ResolveDoubleValueAsync(key, defaultValue, evaluationContext);

        // Assert
        Assert.Equal(expectedValue, result.Value);
    }

    [Theory]
    [MemberData(nameof(TestData.IntegerWithContext), MemberType = typeof(TestData))]
    public async Task IntegerValue_ShouldReturnExpected(string key, string userId, string group, int defaultValue, int expectedValue)
    {
        // Arrange
        var evaluationContext = BuildEvaluationContext(userId, group);
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.targeting.json")
            .Build();
        var provider = new FeatureManagementProvider(configuration);

        // Act
        // Using 0 for the default to verify the value is being read from the configuration
        var result = await provider.ResolveIntegerValueAsync(key, defaultValue, evaluationContext);

        // Assert
        Assert.Equal(expectedValue, result.Value);
    }

    [Theory]
    [MemberData(nameof(TestData.StringWithContext), MemberType = typeof(TestData))]
    public async Task StringValue_ShouldReturnExpected(string key, string userId, string group, string defaultValue, string expectedValue)
    {
        // Arrange
        var evaluationContext = BuildEvaluationContext(userId, group);
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.targeting.json")
            .Build();
        var provider = new FeatureManagementProvider(configuration);

        // Act
        // Using 0 for the default to verify the value is being read from the configuration
        var result = await provider.ResolveStringValueAsync(key, defaultValue, evaluationContext);

        // Assert
        Assert.Equal(expectedValue, result.Value);
    }

    [Theory]
    [MemberData(nameof(TestData.StructureWithContext), MemberType = typeof(TestData))]
    public async Task StructureValue_ShouldReturnExpected(string key, string userId, string group)
    {
        // Arrange
        var evaluationContext = BuildEvaluationContext(userId, group);
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.targeting.json")
            .Build();
        var provider = new FeatureManagementProvider(configuration);

        // Act
        var result = await provider.ResolveStructureValueAsync(key, null, evaluationContext);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.IsStructure);
        Assert.Equal(2, result.Value.AsStructure.Count);
    }

    [Theory]
    [InlineData("MissingFlagKey", "missing.user@openfeature.dev", "missing.group")]
    public async Task StructureValue_ShouldReturnNull(string key, string userId, string group)
    {
        // Arrange
        var evaluationContext = BuildEvaluationContext(userId, group);
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.targeting.json")
            .Build();
        var provider = new FeatureManagementProvider(configuration);

        // Act
        var result = await provider.ResolveStructureValueAsync(key, null, evaluationContext);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Value);
    }
}
