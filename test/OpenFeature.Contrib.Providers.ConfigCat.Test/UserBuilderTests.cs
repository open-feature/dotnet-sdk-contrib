using OpenFeature.Model;
using Xunit;

namespace OpenFeature.Contrib.ConfigCat.Test;

public class UserBuilderTests
{
    [Fact]
    public void UserBuilder_Should_Use_Fallback_Value_When_ID_Is_Not_Specified()
    {
        // Arrange
        var context = EvaluationContext.Builder()
            .Set("ID2", "test1")
            .Set("IdentifierKey", "test2")
            .Build();

        // Act
        var user = context.BuildUser();

        // Assert
        Assert.Equal("<n/a>", user.Identifier);
        Assert.Equal("test1", user.Custom["ID2"]);
        Assert.Equal("test2", user.Custom["IdentifierKey"]);
        Assert.False(user.Custom.ContainsKey("Identifier"));
    }

    [Fact]
    public void UserBuilder_Should_Map_Identifiers_With_TargetingKey_Taking_Precendence()
    {
        // Arrange
        var context = EvaluationContext.Builder()
            .Set("id", "test2")
            .Set("Identifier", "test3")
            .SetTargetingKey("test")
            .Build();

        // Act
        var user = context.BuildUser();

        // Assert
        Assert.Equal("test", user.Identifier);
        Assert.Equal("test", user.Custom["targetingKey"]);
        Assert.Equal("test2", user.Custom["id"]);
        Assert.False(user.Custom.ContainsKey("Identifier"));
    }

    [Theory]
    [InlineData("targetingKey")]
    [InlineData("id")]
    [InlineData("ID")]
    [InlineData("identifier")]
    [InlineData("Identifier")]
    public void UserBuilder_Should_Map_Identifiers(string key)
    {
        // Arrange
        var context = EvaluationContext.Builder().Set(key, "test").Build();

        // Act
        var user = context.BuildUser();

        // Assert
        Assert.Equal("test", user.Identifier);

        if (key == nameof(user.Identifier))
        {
            Assert.False(user.Custom.ContainsKey(key));
        }
        else
        {
            Assert.Equal("test", user.Custom[key]);
        }
    }

    [Theory]
    [InlineData("email")]
    [InlineData("Email")]
    [InlineData("EMAIL")]
    public void UserBuilder_Should_Map_Email(string key)
    {
        // Arrange
        var context = EvaluationContext.Builder().Set(key, "email@email.com").Build();

        // Act
        var user = context.BuildUser();

        // Assert
        Assert.Equal("email@email.com", user.Email);

        if (key == nameof(user.Email))
        {
            Assert.False(user.Custom.ContainsKey(key));
        }
        else
        {
            Assert.Equal("email@email.com", user.Custom[key]);
        }
    }

    [Theory]
    [InlineData("country")]
    [InlineData("Country")]
    [InlineData("COUNTRY")]
    public void UserBuilder_Should_Map_Country(string key)
    {
        // Arrange
        var context = EvaluationContext.Builder().Set(key, "US").Build();

        // Act
        var user = context.BuildUser();

        // Assert
        Assert.Equal("US", user.Country);

        if (key == nameof(user.Country))
        {
            Assert.False(user.Custom.ContainsKey(key));
        }
        else
        {
            Assert.Equal("US", user.Custom[key]);
        }
    }

    [Fact]
    public void UserBuilder_Should_Map_Custom()
    {
        // Arrange
        var context = EvaluationContext.Builder()
            .Set("custom", "str")
            .Set("bool", true)
            .Set("num", 3.14)
            .Build();

        // Act
        var user = context.BuildUser();

        // Assert
        Assert.Equal("str", user.Custom["custom"]);
        Assert.Equal(true, user.Custom["bool"]);
        Assert.Equal(3.14, user.Custom["num"]);
    }
}
