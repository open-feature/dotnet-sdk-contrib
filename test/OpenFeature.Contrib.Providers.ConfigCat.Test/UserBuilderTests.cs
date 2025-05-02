using OpenFeature.Model;
using Xunit;

namespace OpenFeature.Contrib.ConfigCat.Test;

public class UserBuilderTests
{
    [Theory]
    [InlineData("id", "test")]
    [InlineData("identifier", "test")]
    public void UserBuilder_Should_Map_Identifiers(string key, string value)
    {
        // Arrange
        var context = EvaluationContext.Builder().Set(key, value).Build();

        // Act
        var user = context.BuildUser();

        // Assert
        Assert.Equal(value, user.Identifier);
    }

    [Fact]
    public void UserBuilder_Should_Map_Email()
    {
        // Arrange
        var context = EvaluationContext.Builder().Set("email", "email@email.com").Build();

        // Act
        var user = context.BuildUser();

        // Assert
        Assert.Equal("email@email.com", user.Email);
    }

    [Fact]
    public void UserBuilder_Should_Map_Country()
    {
        // Arrange
        var context = EvaluationContext.Builder().Set("country", "US").Build();

        // Act
        var user = context.BuildUser();

        // Assert
        Assert.Equal("US", user.Country);
    }

    [Fact]
    public void UserBuilder_Should_Map_Custom()
    {
        // Arrange
        var context = EvaluationContext.Builder().Set("custom", "custom").Build();

        // Act
        var user = context.BuildUser();

        // Assert
        Assert.Equal("custom", user.Custom["custom"]);
    }
}
