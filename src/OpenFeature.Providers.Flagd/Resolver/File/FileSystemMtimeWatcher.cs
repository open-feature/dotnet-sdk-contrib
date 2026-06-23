using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OpenFeature.Providers.Flagd.Resolver.File;

/// <summary>
/// Default file watcher for the file resolver. Polls the watched file's last-write
/// timestamp and size at a fixed interval and raises <see cref="FileChanged"/> when
/// either changes.
///
/// Modification-time polling is the default because OS-level file system event APIs
/// (e.g. <see cref="FileSystemWatcher"/>) are unreliable in the environments this
/// resolver targets, such as Linux overlay/NFS mounts and bind-mounted ConfigMaps,
/// where events are frequently missed.
/// </summary>
internal class FileSystemMtimeWatcher : IFileWatcher, IAsyncDisposable
{
    private readonly string _filePath;
    private readonly TimeSpan _pollingInterval;
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private readonly ILogger _logger;
    private Task _watcherTask;
    private bool _disposed;

    private DateTime _lastModified = DateTime.MinValue;
    private long _lastSize = -1;

    internal static readonly TimeSpan DefaultPollingInterval = TimeSpan.FromSeconds(5);

    public event EventHandler<FileChangedEventArgs> FileChanged;

    public FileSystemMtimeWatcher(string filePath, ILogger logger, TimeSpan? pollingInterval = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        _filePath = filePath;
        _pollingInterval = pollingInterval ?? DefaultPollingInterval;
        _logger = logger;
    }

    public void Start()
    {
        if (_watcherTask != null)
            throw new InvalidOperationException($"{nameof(FileSystemMtimeWatcher)} is already running");

        _logger?.LogInformation("Starting {WatcherName} for file '{FilePath}'", nameof(FileSystemMtimeWatcher), _filePath);

        // Establish the baseline so that any file changes occurring after Start()
        // returns are reliably detected.
        if (System.IO.File.Exists(_filePath))
        {
            _lastModified = System.IO.File.GetLastWriteTimeUtc(_filePath);
            _lastSize = new FileInfo(_filePath).Length;
            _logger?.LogInformation("Initial file read for file '{FilePath}', Size={LastFileSize}, Modified={LastModified}.",
                _filePath, _lastSize, _lastModified);
        }

        _watcherTask = Task.Run(async () => await WatchFileAsync(_cts.Token).ConfigureAwait(false), _cts.Token);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;

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
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private async Task WatchFileAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (System.IO.File.Exists(_filePath))
                {
                    var currentModified = System.IO.File.GetLastWriteTimeUtc(_filePath);
                    var currentSize = new FileInfo(_filePath).Length;

                    if (_lastSize < 0)
                    {
                        _lastModified = currentModified;
                        _lastSize = currentSize;
                        _logger?.LogInformation("Initial file read for file '{FilePath}', Size={LastFileSize}, Modified={LastModified}.",
                            _filePath, _lastSize, _lastModified);
                    }
                    else if (currentSize != _lastSize || currentModified != _lastModified)
                    {
                        _lastModified = currentModified;
                        _lastSize = currentSize;
                        _logger?.LogInformation("File modification detected for file '{FilePath}', Size={LastFileSize}, Modified={LastModified}.",
                            _filePath, _lastSize, _lastModified);

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
