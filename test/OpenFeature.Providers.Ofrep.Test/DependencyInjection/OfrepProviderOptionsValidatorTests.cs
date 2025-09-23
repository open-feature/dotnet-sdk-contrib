using Microsoft.Extensions.Options;
using OpenFeature.Providers.Ofrep.DependencyInjection;
using Xunit;

namespace OpenFeature.Providers.Ofrep.Test.DependencyInjection;

public class OfrepProviderOptionsValidatorTests
{
    private readonly OfrepProviderOptionsValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Validate_WithNullOrWhitespaceBaseUrl_ReturnsFailure(string? baseUrl)
    {
        // Arrange
        var options = new OfrepProviderOptions { BaseUrl = baseUrl! };

        // Act
        var result = this._validator.Validate("test", options);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains("Ofrep BaseUrl is required", result.FailureMessage);
    }

    [Theory]
    [InlineData("invalid-url")]
    [InlineData("not-a-url")]
    [InlineData("://example.com")]     // malformed
    [InlineData("http://")]            // incomplete
    [InlineData("relative/path")]      // relative URI
    public void Validate_WithInvalidBaseUrl_ReturnsFailure(string baseUrl)
    {
        // Arrange
        var options = new OfrepProviderOptions { BaseUrl = baseUrl };

        // Act
        var result = this._validator.Validate("test", options);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains("Ofrep BaseUrl must be a valid absolute URI", result.FailureMessage);
    }

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("file:///path/to/file")]
    [InlineData("ws://example.com")]
    [InlineData("mailto:test@example.com")]
    [InlineData("ldap://example.com")]
    public void Validate_WithNonHttpScheme_ReturnsFailure(string baseUrl)
    {
        // Arrange
        var options = new OfrepProviderOptions { BaseUrl = baseUrl };

        // Act
        var result = this._validator.Validate("test", options);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains("Ofrep BaseUrl must use HTTP or HTTPS scheme", result.FailureMessage);
    }

    [Theory]
    [InlineData("http://localhost:8080")]
    [InlineData("https://api.example.com")]
    [InlineData("https://api.example.com/")]
    [InlineData("https://api.example.com/ofrep")]
    [InlineData("http://127.0.0.1:3000")]
    [InlineData("https://subdomain.example.com:443/path")]
    [InlineData("http://example.com")]
    [InlineData("https://example.com:8443/api/v1")]
    public void Validate_WithValidHttpBaseUrl_ReturnsSuccess(string baseUrl)
    {
        // Arrange
        var options = new OfrepProviderOptions { BaseUrl = baseUrl };

        // Act
        var result = this._validator.Validate("test", options);

        // Assert
        Assert.False(result.Failed);
        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void Validate_WithNullOptionsName_StillValidates()
    {
        // Arrange
        var options = new OfrepProviderOptions { BaseUrl = "https://api.example.com" };

        // Act
        var result = this._validator.Validate(null, options);

        // Assert
        Assert.False(result.Failed);
        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void Validate_WithValidOptionsAndAllProperties_ReturnsSuccess()
    {
        // Arrange
        var options = new OfrepProviderOptions
        {
            BaseUrl = "https://api.example.com",
            Timeout = TimeSpan.FromSeconds(30),
            Headers = new Dictionary<string, string> { { "Authorization", "Bearer token" } },
            HttpClientName = "MyClient"
        };

        // Act
        var result = this._validator.Validate("test", options);

        // Assert
        Assert.False(result.Failed);
        Assert.Equal(ValidateOptionsResult.Success, result);
    }
}
