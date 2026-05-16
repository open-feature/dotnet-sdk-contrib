using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Murmur;

namespace OpenFeature.Providers.Flagd.Resolver.InProcess;

internal class FileSystemHashWatcher : IAsyncDisposable, IDisposable
{
    private readonly string _filePath;
    private readonly TimeSpan _pollingInterval;
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private readonly ILogger _logger;
    private readonly HashAlgorithm _murmur128;
    private Task _watcherTask;

    private byte[] _lastFileHash = Array.Empty<byte>();
    private DateTime _lastModified = DateTime.MinValue;
    private int _lastSize;

    internal static readonly TimeSpan DefaultFileChangePollingInterval = TimeSpan.FromMinutes(1);

    public event EventHandler<FileChangedEventArgs> FileChanged;

    public FileSystemHashWatcher(string filePath, ILogger logger, TimeSpan? fileChangePollingInterval = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        _filePath = filePath;
        _pollingInterval = fileChangePollingInterval ?? DefaultFileChangePollingInterval;
        _murmur128 = MurmurHash.Create128(managed: true);
        _logger = logger;
    }

    public void Start()
    {
        if (_watcherTask != null)
            throw new InvalidOperationException($"{nameof(FileSystemHashWatcher)} is already running");

        _logger?.LogInformation("Starting {WatcherName} for file '{FilePath}'", nameof(FileSystemHashWatcher), _filePath);

        // Establish the baseline hash synchronously so that any file changes
        // occurring after Start() returns are reliably detected.
        if (File.Exists(_filePath))
        {
            var (hash, size) = GetFileContentHash();
            _lastFileHash = hash;
            _lastSize = size;
            _lastModified = File.GetLastWriteTimeUtc(_filePath);
            _logger?.LogInformation("Initial file read for file '{FilePath}', Hash={LastFileHash}, Size={LastFileSize}, Modified={LastModified}.",
                _filePath, BitConverter.ToString(hash).Replace("-", ""), _lastSize, _lastModified);
        }

        _watcherTask = Task.Run(async () => await WatchFileAsync(_cts.Token).ConfigureAwait(false), _cts.Token);
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();

        if (_watcherTask != null)
        {
            try
            {
                await _watcherTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Unexpected error awaiting watcher task during disposal for file '{FilePath}'", _filePath);
            }
        }

        _cts.Dispose();
        _murmur128.Dispose();
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Forces the revalidation of cached attributes and data.
    ///
    /// Two techniques are combined:
    ///
    /// 1. Directory.GetFiles triggers READDIRPLUS, refreshing inode attributes.
    /// 2. Reading FileInfo.Length forces a GETATTR that can invalidate cached
    ///    data pages when the server reports a different mtime/size.
    /// </summary>
    private void RefreshFileSystemCache()
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (directory != null)
            {
                Directory.GetFiles(directory, Path.GetFileName(_filePath));
            }

            new FileInfo(_filePath).Refresh();
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "File system cache refresh attempt failed for file '{FilePath}'", _filePath);
        }
    }

    private (byte[] Hash, int Size) GetFileContentHash()
    {
        RefreshFileSystemCache();

        using (var fs = new FileStream(
                   _filePath,
                   FileMode.Open,
                   FileAccess.Read,
                   FileShare.ReadWrite | FileShare.Delete,
                   bufferSize: 4096,
                   FileOptions.SequentialScan))
        {
            var hash = _murmur128.ComputeHash(fs);
            return (hash, (int)fs.Length);
        }
    }

    private async Task WatchFileAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var (currentHash, currentSize) = GetFileContentHash();
                    var currentModified = File.GetLastWriteTimeUtc(_filePath);

                    if (_lastFileHash.Length == 0)
                    {
                        _lastFileHash = currentHash;
                        _lastSize = currentSize;
                        _lastModified = currentModified;
                        _logger?.LogInformation("Initial file read for file '{FilePath}', Hash={LastFileHash}, Size={LastFileSize}, Modified={LastModified}.",
                            _filePath, BitConverter.ToString(currentHash).Replace("-", ""), _lastSize, _lastModified);
                    }
                    else if (currentModified != _lastModified ||
                             currentSize != _lastSize ||
                             !currentHash.AsSpan().SequenceEqual(_lastFileHash))
                    {
                        _lastFileHash = currentHash;
                        _lastSize = currentSize;
                        _lastModified = currentModified;
                        _logger?.LogInformation("File content change detected for file '{FilePath}', Hash={LastFileHash}, Size={LastFileSize}, Modified={LastModified}.",
                            _filePath, BitConverter.ToString(currentHash).Replace("-", ""), _lastSize, _lastModified);

                        RaiseEvent(new FileChangedEventArgs(_filePath, currentModified));
                    }
                }
                else
                {
                    _logger?.LogWarning("Watched file '{FilePath}' does not exist", _filePath);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error polling file '{FilePath}'", _filePath);
            }

            try
            {
                await Task.Delay(_pollingInterval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void RaiseEvent(FileChangedEventArgs e)
    {
        var eventHandler = FileChanged;
        if (eventHandler == null) return;

        try
        {
            eventHandler.Invoke(this, e);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error in FileChanged event handler for file '{FilePath}'", _filePath);
        }
    }
}

/// <summary>
/// Event args for file change notifications.
/// </summary>
internal class FileChangedEventArgs : EventArgs
{
    /// <summary>
    /// Path of the changed file.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// UTC timestamp of the last modification.
    /// </summary>
    public DateTime LastModified { get; }

    public FileChangedEventArgs(string filePath, DateTime lastModified)
    {
        FilePath = filePath;
        LastModified = lastModified;
    }
}
