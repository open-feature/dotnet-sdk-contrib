using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using OpenFeature.Providers.Flagd.Resolver.InProcess;
using Xunit;

namespace OpenFeature.Providers.Flagd.Test.Resolver.InProcess;

public class FileSystemHashWatcherTests : IDisposable
{
    private readonly string _tempDir;

    public FileSystemHashWatcherTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "flagd-hash-test-" + Guid.NewGuid().ToString("N"));
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

    [Fact]
    public void Constructor_WithNullFilePath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new FileSystemHashWatcher(null, NullLogger.Instance));
    }

    [Fact]
    public void Constructor_WithEmptyFilePath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new FileSystemHashWatcher("", NullLogger.Instance));
    }

    [Fact]
    public void Start_CalledTwice_ThrowsInvalidOperationException()
    {
        var filePath = Path.Combine(_tempDir, "flags.json");
        File.WriteAllText(filePath, "{}");

        var watcher = new FileSystemHashWatcher(filePath, NullLogger.Instance,
            fileChangePollingInterval: TimeSpan.FromMilliseconds(100));
        watcher.Start();

        Assert.Throws<InvalidOperationException>(() => watcher.Start());

        watcher.Dispose();
    }

    [Fact]
    public async Task FileChanged_WhenContentChanges_RaisesEvent()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "flags.json");
        File.WriteAllText(filePath, Utils.validFlagConfig);

        var watcher = new FileSystemHashWatcher(filePath, NullLogger.Instance,
            fileChangePollingInterval: TimeSpan.FromMilliseconds(200));

        FileChangedEventArgs receivedArgs = null;
        watcher.FileChanged += (sender, args) => receivedArgs = args;

        watcher.Start();

        // Give the watcher a moment to establish baseline hash
        await Task.Delay(500);

        // Act - modify the file
        var updatedContent = @"{
            ""flags"": {
                ""newFlag"": {
                    ""state"": ""ENABLED"",
                    ""variants"": { ""on"": true, ""off"": false },
                    ""defaultVariant"": ""on""
                }
            }
        }";
        File.WriteAllText(filePath, updatedContent);

        // Assert
        await Utils.AssertUntilAsync(async (ct) =>
        {
            Assert.NotNull(receivedArgs);
            Assert.Equal(filePath, receivedArgs.FilePath);
            await Task.CompletedTask;
        }, timeoutMillis: 5000);

        await watcher.DisposeAsync();
    }

    [Fact]
    public async Task FileChanged_WhenContentDoesNotChange_DoesNotRaiseEvent()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "flags.json");
        File.WriteAllText(filePath, Utils.validFlagConfig);

        var watcher = new FileSystemHashWatcher(filePath, NullLogger.Instance,
            fileChangePollingInterval: TimeSpan.FromMilliseconds(200));

        var eventCount = 0;
        watcher.FileChanged += (sender, args) => eventCount++;

        watcher.Start();

        // Wait for several polling cycles with no changes
        await Task.Delay(1000);

        // Assert - no events should have been raised
        Assert.Equal(0, eventCount);

        await watcher.DisposeAsync();
    }

    [Fact]
    public async Task Dispose_StopsWatching()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "flags.json");
        File.WriteAllText(filePath, Utils.validFlagConfig);

        var watcher = new FileSystemHashWatcher(filePath, NullLogger.Instance,
            fileChangePollingInterval: TimeSpan.FromMilliseconds(200));

        watcher.Start();
        await Task.Delay(500);

        // Act
        watcher.Dispose();

        // Modify the file after disposal - should not raise events
        var eventCount = 0;
        watcher.FileChanged += (sender, args) => eventCount++;

        File.WriteAllText(filePath, @"{ ""flags"": {} }");
        await Task.Delay(1000);

        // Assert
        Assert.Equal(0, eventCount);
    }

    [Fact]
    public async Task DisposeAsync_StopsWatching()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "flags.json");
        File.WriteAllText(filePath, Utils.validFlagConfig);

        var watcher = new FileSystemHashWatcher(filePath, NullLogger.Instance,
            fileChangePollingInterval: TimeSpan.FromMilliseconds(200));

        watcher.Start();
        await Task.Delay(500);

        // Act
        await watcher.DisposeAsync();

        // Modify the file after disposal - should not raise events
        var eventCount = 0;
        watcher.FileChanged += (sender, args) => eventCount++;

        File.WriteAllText(filePath, @"{ ""flags"": {} }");
        await Task.Delay(1000);

        // Assert
        Assert.Equal(0, eventCount);
    }
}
