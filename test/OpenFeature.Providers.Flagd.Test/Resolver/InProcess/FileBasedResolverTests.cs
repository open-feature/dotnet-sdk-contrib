using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OpenFeature.Constant;
using OpenFeature.Error;
using OpenFeature.Model;
using OpenFeature.Providers.Flagd.Resolver.InProcess;
using Xunit;

namespace OpenFeature.Providers.Flagd.Test.Resolver.InProcess;

public class FileBasedResolverTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ILogger _logger = NullLogger.Instance;

    public FileBasedResolverTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "flagd-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    private string CreateTempFlagFile(string content)
    {
        var filePath = Path.Combine(_tempDir, "flags.json");
        File.WriteAllText(filePath, content);
        return filePath;
    }

    [Fact]
    public void Constructor_WithNullFilePath_ThrowsArgumentException()
    {
        var mockValidator = Substitute.For<IJsonSchemaValidator>();

        Assert.Throws<ArgumentException>(() =>
            new FileBasedResolver(_logger, null, mockValidator));
    }

    [Fact]
    public void Constructor_WithEmptyFilePath_ThrowsArgumentException()
    {
        var mockValidator = Substitute.For<IJsonSchemaValidator>();

        Assert.Throws<ArgumentException>(() =>
            new FileBasedResolver(_logger, "", mockValidator));
    }

    [Fact]
    public void Constructor_WithWhitespaceFilePath_ThrowsArgumentException()
    {
        var mockValidator = Substitute.For<IJsonSchemaValidator>();

        Assert.Throws<ArgumentException>(() =>
            new FileBasedResolver(_logger, "   ", mockValidator));
    }

    [Fact]
    public void Constructor_WithNullJsonSchemaValidator_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new FileBasedResolver(_logger, "/some/path.json", null));
    }

    [Fact]
    public async Task Init_WithValidFlagFile_RaisesProviderReadyEvent()
    {
        // Arrange
        var filePath = CreateTempFlagFile(Utils.validFlagConfig);
        var mockValidator = Substitute.For<IJsonSchemaValidator>();
        var resolver = new FileBasedResolver(_logger, filePath, mockValidator,
            waitForFileReadyInterval: TimeSpan.FromSeconds(5));

        FlagdProviderEvent receivedEvent = null;
        resolver.ProviderEvent += (sender, evt) => receivedEvent = evt;

        // Act
        await resolver.Init();

        // Assert
        Assert.NotNull(receivedEvent);
        Assert.Equal(ProviderEventTypes.ProviderReady, receivedEvent.EventType);
        Assert.Contains("validFlag", receivedEvent.FlagsChanged);

        await resolver.Shutdown();
    }

    [Fact]
    public async Task Init_WithMissingFile_ThrowsTimeoutException()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "nonexistent.json");
        var mockValidator = Substitute.For<IJsonSchemaValidator>();
        var resolver = new FileBasedResolver(_logger, filePath, mockValidator,
            waitForFileReadyInterval: TimeSpan.FromSeconds(1));

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(() => resolver.Init());

        await resolver.Shutdown();
    }

    [Fact]
    public async Task Init_WithEmptyFile_ThrowsParseErrorException()
    {
        // Arrange
        var filePath = CreateTempFlagFile("");
        var mockValidator = Substitute.For<IJsonSchemaValidator>();
        var resolver = new FileBasedResolver(_logger, filePath, mockValidator,
            waitForFileReadyInterval: TimeSpan.FromSeconds(5));

        // Act & Assert
        await Assert.ThrowsAsync<ParseErrorException>(() => resolver.Init());

        await resolver.Shutdown();
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_WithValidFlag_ReturnsCorrectValue()
    {
        // Arrange
        var filePath = CreateTempFlagFile(Utils.validFlagConfig);
        var mockValidator = Substitute.For<IJsonSchemaValidator>();
        var resolver = new FileBasedResolver(_logger, filePath, mockValidator,
            waitForFileReadyInterval: TimeSpan.FromSeconds(5));
        await resolver.Init();

        // Act
        var result = await resolver.ResolveBooleanValueAsync("validFlag", false);

        // Assert
        Assert.True(result.Value);

        await resolver.Shutdown();
    }

    [Fact]
    public async Task ResolveStringValueAsync_WithValidFlag_ReturnsCorrectValue()
    {
        // Arrange
        var filePath = CreateTempFlagFile(Utils.flags);
        var mockValidator = Substitute.For<IJsonSchemaValidator>();
        var resolver = new FileBasedResolver(_logger, filePath, mockValidator,
            waitForFileReadyInterval: TimeSpan.FromSeconds(5));
        await resolver.Init();

        // Act
        var result = await resolver.ResolveStringValueAsync("staticStringFlag", "default");

        // Assert
        Assert.Equal("#CC0000", result.Value);

        await resolver.Shutdown();
    }

    [Fact]
    public async Task ResolveIntegerValueAsync_WithValidFlag_ReturnsCorrectValue()
    {
        // Arrange
        var filePath = CreateTempFlagFile(Utils.flags);
        var mockValidator = Substitute.For<IJsonSchemaValidator>();
        var resolver = new FileBasedResolver(_logger, filePath, mockValidator,
            waitForFileReadyInterval: TimeSpan.FromSeconds(5));
        await resolver.Init();

        // Act
        var result = await resolver.ResolveIntegerValueAsync("staticIntFlag", 0);

        // Assert
        Assert.Equal(1, result.Value);

        await resolver.Shutdown();
    }

    [Fact]
    public async Task ResolveDoubleValueAsync_WithValidFlag_ReturnsCorrectValue()
    {
        // Arrange
        var filePath = CreateTempFlagFile(Utils.flags);
        var mockValidator = Substitute.For<IJsonSchemaValidator>();
        var resolver = new FileBasedResolver(_logger, filePath, mockValidator,
            waitForFileReadyInterval: TimeSpan.FromSeconds(5));
        await resolver.Init();

        // Act
        var result = await resolver.ResolveDoubleValueAsync("staticFloatFlag", 0.0);

        // Assert
        Assert.Equal(1.0, result.Value);

        await resolver.Shutdown();
    }

    [Fact]
    public async Task ResolveStructureValueAsync_WithValidFlag_ReturnsCorrectValue()
    {
        // Arrange
        var filePath = CreateTempFlagFile(Utils.flags);
        var mockValidator = Substitute.For<IJsonSchemaValidator>();
        var resolver = new FileBasedResolver(_logger, filePath, mockValidator,
            waitForFileReadyInterval: TimeSpan.FromSeconds(5));
        await resolver.Init();

        // Act
        var result = await resolver.ResolveStructureValueAsync("staticObjectFlag", null);

        // Assert
        Assert.NotNull(result.Value);
        Assert.Equal(123, result.Value.AsStructure["abc"].AsInteger);

        await resolver.Shutdown();
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_WithUnknownFlag_ThrowsFeatureProviderException()
    {
        // Arrange
        var filePath = CreateTempFlagFile(Utils.validFlagConfig);
        var mockValidator = Substitute.For<IJsonSchemaValidator>();
        var resolver = new FileBasedResolver(_logger, filePath, mockValidator,
            waitForFileReadyInterval: TimeSpan.FromSeconds(5));
        await resolver.Init();

        // Act & Assert
        await Assert.ThrowsAsync<FeatureProviderException>(
            () => resolver.ResolveBooleanValueAsync("unknownFlag", false));

        await resolver.Shutdown();
    }

    [Fact]
    public async Task FileChange_WithFSWatcher_RaisesProviderConfigurationChangedEvent()
    {
        // Arrange
        var filePath = CreateTempFlagFile(Utils.validFlagConfig);
        var mockValidator = Substitute.For<IJsonSchemaValidator>();
        var resolver = new FileBasedResolver(_logger, filePath, mockValidator,
            useHashFileChangeDetection: false,
            waitForFileReadyInterval: TimeSpan.FromSeconds(5));

        FlagdProviderEvent configChangedEvent = null;
        resolver.ProviderEvent += (sender, evt) =>
        {
            if (evt.EventType == ProviderEventTypes.ProviderConfigurationChanged)
                configChangedEvent = evt;
        };

        await resolver.Init();

        // Act - modify the file
        var updatedConfig = @"{
            ""flags"": {
                ""newFlag"": {
                    ""state"": ""ENABLED"",
                    ""variants"": { ""on"": true, ""off"": false },
                    ""defaultVariant"": ""on""
                }
            }
        }";
        File.WriteAllText(filePath, updatedConfig);

        // Assert - wait for debounced reload
        await Utils.AssertUntilAsync(async (ct) =>
        {
            Assert.NotNull(configChangedEvent);
            Assert.Equal(ProviderEventTypes.ProviderConfigurationChanged, configChangedEvent.EventType);
            Assert.Contains("newFlag", configChangedEvent.FlagsChanged);
            await Task.CompletedTask;
        }, timeoutMillis: 5000);

        await resolver.Shutdown();
    }

    [Fact]
    public async Task FileChange_WithHashWatcher_RaisesProviderConfigurationChangedEvent()
    {
        // Arrange
        var filePath = CreateTempFlagFile(Utils.validFlagConfig);
        var mockValidator = Substitute.For<IJsonSchemaValidator>();
        var resolver = new FileBasedResolver(_logger, filePath, mockValidator,
            useHashFileChangeDetection: true,
            waitForFileReadyInterval: TimeSpan.FromSeconds(5),
            fileChangePollingInterval: TimeSpan.FromMilliseconds(200));

        FlagdProviderEvent configChangedEvent = null;
        resolver.ProviderEvent += (sender, evt) =>
        {
            if (evt.EventType == ProviderEventTypes.ProviderConfigurationChanged)
                configChangedEvent = evt;
        };

        await resolver.Init();

        // Act - modify the file
        var updatedConfig = @"{
            ""flags"": {
                ""hashDetectedFlag"": {
                    ""state"": ""ENABLED"",
                    ""variants"": { ""on"": true, ""off"": false },
                    ""defaultVariant"": ""on""
                }
            }
        }";
        File.WriteAllText(filePath, updatedConfig);

        // Assert - wait for hash-based change detection
        await Utils.AssertUntilAsync(async (ct) =>
        {
            Assert.NotNull(configChangedEvent);
            Assert.Equal(ProviderEventTypes.ProviderConfigurationChanged, configChangedEvent.EventType);
            Assert.Contains("hashDetectedFlag", configChangedEvent.FlagsChanged);
            await Task.CompletedTask;
        }, timeoutMillis: 5000);

        await resolver.Shutdown();
    }

    [Fact]
    public async Task Shutdown_DisposesCleanly()
    {
        // Arrange
        var filePath = CreateTempFlagFile(Utils.validFlagConfig);
        var mockValidator = Substitute.For<IJsonSchemaValidator>();
        var resolver = new FileBasedResolver(_logger, filePath, mockValidator,
            waitForFileReadyInterval: TimeSpan.FromSeconds(5));
        await resolver.Init();

        // Act & Assert - should not throw
        await resolver.Shutdown();
    }

    [Fact]
    public async Task Shutdown_WithoutInit_DoesNotThrow()
    {
        // Arrange
        var filePath = CreateTempFlagFile(Utils.validFlagConfig);
        var mockValidator = Substitute.For<IJsonSchemaValidator>();
        var resolver = new FileBasedResolver(_logger, filePath, mockValidator,
            waitForFileReadyInterval: TimeSpan.FromSeconds(5));

        // Act & Assert - should not throw
        await resolver.Shutdown();
    }
}
