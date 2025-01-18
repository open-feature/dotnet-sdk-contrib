using Xunit;
using Moq;
using System;
using OpenFeature.Model;
using Amazon.AppConfigData;
using Amazon.AppConfigData.Model;
using System.Text;
using OpenFeature.Contrib.Providers.AwsAppConfig;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Caching.Memory;
public class AppConfigRetrievalApiTests
{
    private readonly Mock<IAmazonAppConfigData> _appConfigClientMock;
    private readonly IMemoryCache _memoryCache;
    private readonly AppConfigRetrievalApi _retrievalApi;
    private readonly string _jsonContent;

    public AppConfigRetrievalApiTests()
    {
        _appConfigClientMock = new Mock<IAmazonAppConfigData>();        
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _retrievalApi = new AppConfigRetrievalApi(_appConfigClientMock.Object, _memoryCache);
        _jsonContent = System.IO.File.ReadAllText("test-data.json");
    }

    [Fact]
    public async Task GetConfiguration_WhenSuccessful_ReturnsConfiguration()
    {
        // Arrange
        var profile = new FeatureFlagProfile{
            ApplicationIdentifier = "testApp",
            EnvironmentIdentifier = "testEnv",
            ConfigurationProfileIdentifier = "testConfig"
        };
        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(_jsonContent));

        var response = new GetLatestConfigurationResponse
        {
            Configuration = memoryStream,
            NextPollConfigurationToken = "nextToken"
        };

        _appConfigClientMock
            .Setup(x => x.StartConfigurationSessionAsync(
                It.IsAny<StartConfigurationSessionRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StartConfigurationSessionResponse{InitialConfigurationToken="initialToken"});

        _appConfigClientMock
            .Setup(x => x.GetLatestConfigurationAsync(
                It.IsAny<GetLatestConfigurationRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        
        

        // Act
        var result = await _retrievalApi.GetLatestConfigurationAsync(profile);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_jsonContent, await new StreamReader(result.Configuration).ReadToEndAsync());        
    }

    [Fact]
    public async Task GetConfiguration_WhenSuccessful_SetCorrectNextPollToken()
    {
        // Arrange
        var profile = new FeatureFlagProfile{
            ApplicationIdentifier = "testApp",
            EnvironmentIdentifier = "testEnv",
            ConfigurationProfileIdentifier = "testConfig"
        };
        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(_jsonContent));

        var response = new GetLatestConfigurationResponse
        {
            Configuration = memoryStream,
            NextPollConfigurationToken = "nextToken"
        };

        _appConfigClientMock
            .Setup(x => x.StartConfigurationSessionAsync(
                It.IsAny<StartConfigurationSessionRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StartConfigurationSessionResponse{InitialConfigurationToken="initialToken"});

        _appConfigClientMock
            .Setup(x => x.GetLatestConfigurationAsync(
                It.IsAny<GetLatestConfigurationRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        
        

        // Act
        var result = await _retrievalApi.GetLatestConfigurationAsync(profile);

        // Assert        
        Assert.Equal("nextToken", result.NextPollConfigurationToken);        
        // Verify that correct sessionToken is set for Next polling.
        Assert.Equal(result.NextPollConfigurationToken, _memoryCache.Get<string>($"session_token_{profile}"));
    }

    [Fact]
    public async Task GetConfiguration_WhenSuccessful_CalledWithCorrectInitialToken()
    {
        // Arrange
        var profile = new FeatureFlagProfile{
            ApplicationIdentifier = "testApp",
            EnvironmentIdentifier = "testEnv",
            ConfigurationProfileIdentifier = "testConfig"
        };
        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(_jsonContent));

        var response = new GetLatestConfigurationResponse
        {
            Configuration = memoryStream,
            NextPollConfigurationToken = "nextToken"
        };

        _appConfigClientMock
            .Setup(x => x.StartConfigurationSessionAsync(
                It.IsAny<StartConfigurationSessionRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StartConfigurationSessionResponse{InitialConfigurationToken="initialToken"});

        _appConfigClientMock
            .Setup(x => x.GetLatestConfigurationAsync(
                It.IsAny<GetLatestConfigurationRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        
        

        // Act
        var result = await _retrievalApi.GetLatestConfigurationAsync(profile);

        // Assert        
        _appConfigClientMock.Verify(x => x.GetLatestConfigurationAsync(
            It.Is<GetLatestConfigurationRequest>(r => r.ConfigurationToken == "initialToken"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(null, "env", "config")]
    [InlineData("app", null, "config")]
    [InlineData("app", "env", null)]    
    public async Task GetConfiguration_WithNullParameters_ThrowsArgumentNullException(
        string application,
        string environment,
        string configuration)
    {
        // Arrange
        var profile = new FeatureFlagProfile
        {
            ApplicationIdentifier = application,
            EnvironmentIdentifier = environment,
            ConfigurationProfileIdentifier = configuration
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _retrievalApi.GetLatestConfigurationAsync(profile));
    }

    [Fact]
    public async Task GetConfiguration_WhenServiceThrows_PropagatesException()
    {
        // Arrange
         var profile = new FeatureFlagProfile{
            ApplicationIdentifier = "testApp",
            EnvironmentIdentifier = "testEnv",
            ConfigurationProfileIdentifier = "testConfig"
        };

        _appConfigClientMock
            .Setup(x => x.StartConfigurationSessionAsync(
                It.IsAny<StartConfigurationSessionRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StartConfigurationSessionResponse{InitialConfigurationToken="initialToken"});

        _appConfigClientMock
            .Setup(x => x.GetLatestConfigurationAsync(
                It.IsAny<GetLatestConfigurationRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonAppConfigDataException("Test exception"));

        // Act & Assert
        await Assert.ThrowsAsync<AmazonAppConfigDataException>(() =>
            _retrievalApi.GetLatestConfigurationAsync(profile));
    }

    [Fact]
    public async Task GetConfiguration_VerifiesCorrectParametersPassedToClient()
    {
        // Arrange
         var profile = new FeatureFlagProfile{
            ApplicationIdentifier = "testApp",
            EnvironmentIdentifier = "testEnv",
            ConfigurationProfileIdentifier = "testConfig"
        };

        var response = new GetLatestConfigurationResponse
        {
            Configuration = new MemoryStream(),
            NextPollConfigurationToken = "nextToken"
        };

        _appConfigClientMock
            .Setup(x => x.StartConfigurationSessionAsync(
                It.IsAny<StartConfigurationSessionRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StartConfigurationSessionResponse{InitialConfigurationToken="initialToken"});

        _appConfigClientMock
            .Setup(x => x.GetLatestConfigurationAsync(
                It.IsAny<GetLatestConfigurationRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        await _retrievalApi.GetLatestConfigurationAsync(profile);

        // Assert
        _appConfigClientMock.Verify(x => x.GetLatestConfigurationAsync(
            It.Is<GetLatestConfigurationRequest>(r =>
                r.ConfigurationToken == "initialToken"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetConfiguration_WhenCalledSecondTime_UsesNextPollConfigToken()
    {
        // Arrange        
        var profile = new FeatureFlagProfile{
            ApplicationIdentifier = "testApp",
            EnvironmentIdentifier = "testEnv",
            ConfigurationProfileIdentifier = "testConfig"
        };
        var response = new GetLatestConfigurationResponse
        {
            Configuration = new MemoryStream(),
            NextPollConfigurationToken = "nextToken"
        };

        _appConfigClientMock
            .Setup(x => x.StartConfigurationSessionAsync(
                It.IsAny<StartConfigurationSessionRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StartConfigurationSessionResponse{InitialConfigurationToken="initialToken"});

        _appConfigClientMock
            .Setup(x => x.GetLatestConfigurationAsync(
                It.IsAny<GetLatestConfigurationRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        await _retrievalApi.GetLatestConfigurationAsync(profile);

        await _retrievalApi.GetLatestConfigurationAsync(profile);

        // Assert
        _appConfigClientMock.Verify(x => x.GetLatestConfigurationAsync(
            It.Is<GetLatestConfigurationRequest>(r => r.ConfigurationToken == "initialToken"),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _appConfigClientMock.Verify(x => x.GetLatestConfigurationAsync(
            It.Is<GetLatestConfigurationRequest>(r => r.ConfigurationToken == "nextToken"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
