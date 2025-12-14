using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OpenFeature.Providers.Ofrep.Configuration;
using Xunit;

namespace OpenFeature.Providers.Ofrep.Test.Configuration;

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
    public void ParseHeaders_ShouldParseMultipleHeadersWithEscapeSequences()
    {
        // Tests: multiple headers, escaped comma, escaped equals, escaped backslash, multiple equals in value
        var headers = OfrepOptions.ParseHeaders(@"Key1=val\,ue1,Key2=val\=ue2,Key3=C:\\path,Key4=base64==");

        Assert.Equal(4, headers.Count);
        Assert.Equal("val,ue1", headers["Key1"]);       // escaped comma
        Assert.Equal("val=ue2", headers["Key2"]);       // escaped equals
        Assert.Equal(@"C:\path", headers["Key3"]);      // escaped backslash
        Assert.Equal("base64==", headers["Key4"]);      // multiple equals in value
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
            { OfrepOptions.EnvVarHeaders, @"Auth=Bearer\=token" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        var options = OfrepOptions.FromConfiguration(configuration);

        // Assert
        Assert.Equal("http://config-endpoint:8080", options.BaseUrl);  // config over env var
        Assert.Equal(TimeSpan.FromMilliseconds(3000), options.Timeout);
        Assert.Equal("Bearer=token", options.Headers["Auth"]);          // escape sequence parsed
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
