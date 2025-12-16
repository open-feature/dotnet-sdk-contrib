using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OpenFeature.Providers.Ofrep.Configuration;
using Xunit;

namespace OpenFeature.Providers.Ofrep.Test.Configuration;

[Collection("EnvironmentVariableTests")]
public class OfrepOptionsEnvironmentTest : IDisposable
{
    public OfrepOptionsEnvironmentTest() => CleanEnvVars();

    public void Dispose() => CleanEnvVars();

    private static void CleanEnvVars()
    {
        Environment.SetEnvironmentVariable(OfrepOptions.EnvVarEndpoint, null);
        Environment.SetEnvironmentVariable(OfrepOptions.EnvVarHeaders, null);
        Environment.SetEnvironmentVariable(OfrepOptions.EnvVarTimeout, null);
    }

    #region FromEnvironment Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-valid-uri")]
    public void FromEnvironment_ShouldThrowArgumentException_WhenEndpointIsInvalid(string? endpoint)
    {
        // Arrange
        if (endpoint != null)
        {
            Environment.SetEnvironmentVariable(OfrepOptions.EnvVarEndpoint, endpoint);
        }

        // Act & Assert
        Assert.Throws<ArgumentException>(() => OfrepOptions.FromEnvironment());
    }

    [Fact]
    public void FromEnvironment_ShouldCreateOptionsWithAllValues()
    {
        // Arrange
        Environment.SetEnvironmentVariable(OfrepOptions.EnvVarEndpoint, "http://localhost:8080");
        Environment.SetEnvironmentVariable(OfrepOptions.EnvVarTimeout, "5000");
        Environment.SetEnvironmentVariable(OfrepOptions.EnvVarHeaders, "Authorization=Bearer token,Content-Type=application/json");

        // Act
        var options = OfrepOptions.FromEnvironment();

        // Assert
        Assert.Equal("http://localhost:8080", options.BaseUrl);
        Assert.Equal(TimeSpan.FromMilliseconds(5000), options.Timeout);
        Assert.Equal(2, options.Headers.Count);
        Assert.Equal("Bearer token", options.Headers["Authorization"]);
    }

    [Theory]
    [InlineData("not-a-number")]
    [InlineData("-100")]
    [InlineData("0")]
    public void FromEnvironment_ShouldUseDefaultTimeout_WhenTimeoutIsInvalid(string timeout)
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        Environment.SetEnvironmentVariable(OfrepOptions.EnvVarEndpoint, "http://localhost:8080");
        Environment.SetEnvironmentVariable(OfrepOptions.EnvVarTimeout, timeout);

        // Act
        var options = OfrepOptions.FromEnvironment(logger);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(10), options.Timeout);
    }

    #endregion

    #region ParseHeaders Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseHeaders_ShouldReturnEmptyDictionary_WhenInputIsNullOrEmpty(string? input)
    {
        // Act
        var headers = OfrepOptions.ParseHeaders(input);

        // Assert
        Assert.Empty(headers);
    }

    [Fact]
    public void ParseHeaders_ShouldParseMultipleHeadersWithUrlEncoding()
    {
        // Tests: multiple headers, URL-encoded equals (%3D), multiple equals in value
        // Note: URL-encoded comma (%2C) is decoded before splitting, so commas in values are not supported
        var headers = OfrepOptions.ParseHeaders("Key1=value1,Key2=val%3Due2,Key3=base64==");

        Assert.Equal(3, headers.Count);
        Assert.Equal("value1", headers["Key1"]);
        Assert.Equal("val=ue2", headers["Key2"]);       // URL-encoded equals in value
        Assert.Equal("base64==", headers["Key3"]);      // multiple equals in value (no encoding needed)
    }

    [Fact]
    public void ParseHeaders_ShouldHandleEdgeCases()
    {
        // Tests: whitespace trimming, empty values, duplicate keys (last wins), malformed entries skipped
        var headers = OfrepOptions.ParseHeaders(" Key1 = Value1 ,EmptyValue=,Key1=Override,MalformedEntry");

        Assert.Equal(2, headers.Count);
        Assert.Equal("Override", headers["Key1"]);      // duplicate key, last value wins
        Assert.Equal("", headers["EmptyValue"]);        // empty value allowed
        Assert.False(headers.ContainsKey("MalformedEntry")); // malformed entries skipped
    }

    #endregion

    #region FromConfiguration Tests

    [Fact]
    public void FromConfiguration_ShouldThrowArgumentException_WhenNoConfigurationAvailable()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => OfrepOptions.FromConfiguration(null));
        Assert.Contains(OfrepOptions.EnvVarEndpoint, ex.Message);
    }

    [Fact]
    public void FromConfiguration_ShouldPreferConfigurationOverEnvVar()
    {
        // Arrange
        Environment.SetEnvironmentVariable(OfrepOptions.EnvVarEndpoint, "http://env-endpoint:9090");
        var configData = new Dictionary<string, string?>
        {
            { OfrepOptions.EnvVarEndpoint, "http://config-endpoint:8080" },
            { OfrepOptions.EnvVarTimeout, "3000" },
            { OfrepOptions.EnvVarHeaders, "Auth=Bearer%3Dtoken" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        var options = OfrepOptions.FromConfiguration(configuration);

        // Assert
        Assert.Equal("http://config-endpoint:8080", options.BaseUrl);  // config over env var
        Assert.Equal(TimeSpan.FromMilliseconds(3000), options.Timeout);
        Assert.Equal("Bearer=token", options.Headers["Auth"]);          // URL-encoded equals parsed
    }

    [Fact]
    public void FromConfiguration_ShouldFallbackToEnvVar_WhenConfigurationEmpty()
    {
        // Arrange
        Environment.SetEnvironmentVariable(OfrepOptions.EnvVarEndpoint, "http://env-endpoint:9090");
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var options = OfrepOptions.FromConfiguration(configuration);

        // Assert
        Assert.Equal("http://env-endpoint:9090", options.BaseUrl);
    }

    #endregion

    #region Environment Variable Constants Tests

    [Fact]
    public void EnvVarConstants_ShouldHaveCorrectValues()
    {
        Assert.Equal("OFREP_ENDPOINT", OfrepOptions.EnvVarEndpoint);
        Assert.Equal("OFREP_HEADERS", OfrepOptions.EnvVarHeaders);
        Assert.Equal("OFREP_TIMEOUT_MS", OfrepOptions.EnvVarTimeout);
    }

    #endregion
}
