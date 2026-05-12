using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenFeature.Constant;
using OpenFeature.Error;
using OpenFeature.Model;

namespace OpenFeature.Providers.Flagd.Resolver.InProcess;

internal class FileBasedResolver : Resolver
{
    private readonly string _filePath;
    private readonly JsonEvaluator _evaluator;
    private readonly IJsonSchemaValidator _jsonSchemaValidator;
    private readonly TimeSpan _fileWatcherWaitForFileReadyInterval;
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private readonly ILogger _logger;
    private readonly ReaderWriterLockSlim _evaluatorLock = new ReaderWriterLockSlim();
    private readonly bool _useHashFileChangeDetection;
    private readonly TimeSpan? _fileChangePollingInterval;
    private IDisposable _fileWatcher;
    private Timer _debounceTimer;
    private Task _fileExistTask;

    internal static readonly TimeSpan DefaultWaitForFileReadyInterval = TimeSpan.FromMinutes(5);
    internal static readonly TimeSpan WaitForFileReadyPollingInterval = TimeSpan.FromSeconds(10);
    internal static readonly TimeSpan FileSystemWatcherDebounceInterval = TimeSpan.FromMilliseconds(500);

    public event EventHandler<FlagdProviderEvent> ProviderEvent;

    internal FileBasedResolver(ILogger logger, string filePath,
        IJsonSchemaValidator jsonSchemaValidator,
        string sourceSelector = "",
        bool useHashFileChangeDetection = false,
        TimeSpan? waitForFileReadyInterval = null,
        TimeSpan? fileChangePollingInterval = null)
    {
        if (!IsFilePathValid(filePath))
            throw new ArgumentException($"The provided file path '{filePath}' is not valid.", nameof(filePath));

        _logger = logger;
        _filePath = Path.GetFullPath(filePath);
        _jsonSchemaValidator = jsonSchemaValidator ?? throw new ArgumentNullException(nameof(jsonSchemaValidator));
        _fileWatcherWaitForFileReadyInterval = waitForFileReadyInterval ?? DefaultWaitForFileReadyInterval;
        _useHashFileChangeDetection = useHashFileChangeDetection;
        _fileChangePollingInterval = fileChangePollingInterval;
        _evaluator = new JsonEvaluator(sourceSelector, jsonSchemaValidator);
    }

    public async Task Init()
    {
        _logger?.LogInformation("{Resolver} for '{FilePath}' is initializing", nameof(FileBasedResolver), _filePath);

        await _jsonSchemaValidator.InitializeAsync(_cts.Token).ConfigureAwait(false);

        await (_fileExistTask = WaitForFileExists()).ConfigureAwait(false);

        try
        {
            var flagKeys = LoadFlags();

            CreateFileWatcher();

            _logger?.LogInformation("{Resolver} for '{FilePath}' was initialized successfully", nameof(FileBasedResolver), _filePath);

            var flagdEvent = new FlagdProviderEvent(
                ProviderEventTypes.ProviderReady,
                flagKeys,
                Structure.Empty);

            RaiseEvent(flagdEvent);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "{Resolver} failed loading flags from '{FilePath}'", nameof(FileBasedResolver), _filePath);

            var flagdEvent = new FlagdProviderEvent(
                ProviderEventTypes.ProviderError,
                null,
                Structure.Empty);

            RaiseEvent(flagdEvent);

            throw;
        }
    }

    public async Task Shutdown()
    {
        _cts.Cancel();

        if (_fileExistTask != null)
        {
            try
            {
                await _fileExistTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            catch (TimeoutException) { }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Unexpected error during file exists task teardown for file '{FilePath}'", _filePath);
            }
        }

        if (_fileWatcher != null)
        {
            if (_fileWatcher is IAsyncDisposable asyncDisposableWatcher)
            {
                try
                {
                    await asyncDisposableWatcher.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Unexpected error during file watcher async disposal for file '{FilePath}'", _filePath);
                }
            }
            else
            {
                _fileWatcher.Dispose();
            }
            _fileWatcher = null;
        }

        _debounceTimer?.Dispose();
        _evaluatorLock.Dispose();
        _cts.Dispose();
    }

    public Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(string flagKey, bool defaultValue, EvaluationContext context = null)
    {
        _evaluatorLock.EnterReadLock();
        try
        {
            return Task.FromResult(_evaluator.ResolveBooleanValueAsync(flagKey, defaultValue, context));
        }
        finally
        {
            _evaluatorLock.ExitReadLock();
        }
    }

    public Task<ResolutionDetails<string>> ResolveStringValueAsync(string flagKey, string defaultValue, EvaluationContext context = null)
    {
        _evaluatorLock.EnterReadLock();
        try
        {
            return Task.FromResult(_evaluator.ResolveStringValueAsync(flagKey, defaultValue, context));
        }
        finally
        {
            _evaluatorLock.ExitReadLock();
        }
    }

    public Task<ResolutionDetails<int>> ResolveIntegerValueAsync(string flagKey, int defaultValue, EvaluationContext context = null)
    {
        _evaluatorLock.EnterReadLock();
        try
        {
            return Task.FromResult(_evaluator.ResolveIntegerValueAsync(flagKey, defaultValue, context));
        }
        finally
        {
            _evaluatorLock.ExitReadLock();
        }
    }

    public Task<ResolutionDetails<double>> ResolveDoubleValueAsync(string flagKey, double defaultValue, EvaluationContext context = null)
    {
        _evaluatorLock.EnterReadLock();
        try
        {
            return Task.FromResult(_evaluator.ResolveDoubleValueAsync(flagKey, defaultValue, context));
        }
        finally
        {
            _evaluatorLock.ExitReadLock();
        }
    }

    public Task<ResolutionDetails<Value>> ResolveStructureValueAsync(string flagKey, Value defaultValue, EvaluationContext context = null)
    {
        _evaluatorLock.EnterReadLock();
        try
        {
            return Task.FromResult(_evaluator.ResolveStructureValueAsync(flagKey, defaultValue, context));
        }
        finally
        {
            _evaluatorLock.ExitReadLock();
        }
    }

    private static bool IsFilePathValid(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return false;

        try
        {
            _ = Path.GetFullPath(filePath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void RaiseEvent(FlagdProviderEvent flagdEvent)
    {
        var eventHandler = ProviderEvent;
        if (eventHandler == null) return;

        try
        {
            eventHandler.Invoke(this, flagdEvent);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error while raising provider event {EventType}, the handler delegate threw an exception", flagdEvent.EventType);
        }
    }

    private Task WaitForFileExists()
    {
        return Task.Run(async () =>
        {
            var deadline = DateTime.UtcNow.Add(_fileWatcherWaitForFileReadyInterval);

            while (true)
            {
                _cts.Token.ThrowIfCancellationRequested();

                try
                {
                    if (File.Exists(_filePath))
                    {
                        _logger?.LogInformation("File '{FilePath}' exists. Loading flag file", _filePath);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error checking existence of file '{FilePath}'", _filePath);
                }

                if (DateTime.UtcNow >= deadline)
                    throw new TimeoutException($"Flag file '{_filePath}' was not found within defined interval: {_fileWatcherWaitForFileReadyInterval}");

                await Task.Delay(WaitForFileReadyPollingInterval, _cts.Token).ConfigureAwait(false);
            }
        }, _cts.Token);
    }

    private List<string> LoadFlags()
    {
        string flagJson;

        _logger?.LogInformation("Loading flags from file '{FilePath}'", _filePath);

        using (var fs = new FileStream(
                   _filePath,
                   FileMode.Open,
                   FileAccess.Read,
                   FileShare.ReadWrite | FileShare.Delete))
        {
            using (var reader = new StreamReader(fs, Encoding.UTF8))
            {
                flagJson = reader.ReadToEnd();
            }
        }

        if (string.IsNullOrWhiteSpace(flagJson))
        {
            var errorMessage = $"Flag file '{_filePath}' is empty. No flags were loaded.";
            _logger?.LogWarning(errorMessage);
            throw new ParseErrorException(errorMessage);
        }

        _evaluatorLock.EnterWriteLock();

        try
        {
            _evaluator.Sync(FlagConfigurationUpdateType.ALL, flagJson);
            _logger?.LogInformation("Flags were loaded successfully from file '{FilePath}'", _filePath);
            return new List<string>(_evaluator.Flags.Keys);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading flags from file '{FilePath}'", _filePath);
            throw;
        }
        finally
        {
            _evaluatorLock.ExitWriteLock();
        }
    }

    private void CreateFileWatcher()
    {
        if (_useHashFileChangeDetection)
        {
            _logger?.LogInformation("File watcher for '{FilePath}' is using content hashing for change detection", _filePath);

            var fileWatcher = new FileSystemHashWatcher(_filePath, _logger, _fileChangePollingInterval);

            fileWatcher.FileChanged += (sender, e) =>
            {
                try
                {
                    _logger?.LogInformation("File '{FilePath}' content change detected at {LastModified}.", _filePath, e.LastModified);

                    var flagKeys = LoadFlags();

                    var flagdEvent = new FlagdProviderEvent(
                        ProviderEventTypes.ProviderConfigurationChanged,
                        flagKeys,
                        Structure.Empty);

                    RaiseEvent(flagdEvent);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error reloading flags from file '{FilePath}'", _filePath);

                    var flagdEvent = new FlagdProviderEvent(
                        ProviderEventTypes.ProviderError,
                        null,
                        Structure.Empty);

                    RaiseEvent(flagdEvent);
                }
            };

            fileWatcher.Start();

            _fileWatcher = fileWatcher;
        }
        else
        {
            _logger?.LogInformation("File watcher for '{FilePath}' is using file system events for change detection", _filePath);

            var directory = Path.GetDirectoryName(_filePath);
            var fileName = Path.GetFileName(_filePath);
            var fileWatcher = new FileSystemWatcher(directory, fileName);

            fileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName;
            fileWatcher.IncludeSubdirectories = false;

            _debounceTimer = new Timer(_ =>
            {
                try
                {
                    _logger?.LogInformation("Debounced file change reload for '{FilePath}'.", _filePath);

                    var flagKeys = LoadFlags();

                    var flagdEvent = new FlagdProviderEvent(
                        ProviderEventTypes.ProviderConfigurationChanged,
                        flagKeys,
                        Structure.Empty);

                    RaiseEvent(flagdEvent);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error reloading flags from file '{FilePath}'", _filePath);

                    var flagdEvent = new FlagdProviderEvent(
                        ProviderEventTypes.ProviderError,
                        null,
                        Structure.Empty);

                    RaiseEvent(flagdEvent);
                }
            }, null, Timeout.Infinite, Timeout.Infinite);

            FileSystemEventHandler fileChangeHandler = (object sender, FileSystemEventArgs e) =>
            {
                _logger?.LogInformation("File '{FilePath}' file change detected type={ChangeType}, debouncing reload.", _filePath, e.ChangeType);
                _debounceTimer.Change(FileSystemWatcherDebounceInterval, Timeout.InfiniteTimeSpan);
            };

            fileWatcher.Changed += fileChangeHandler;
            fileWatcher.Created += fileChangeHandler;

            fileWatcher.Renamed += (object sender, RenamedEventArgs e) =>
            {
                fileChangeHandler.Invoke(sender, e);
            };

            fileWatcher.Error += (object sender, ErrorEventArgs e) =>
            {
                _logger?.LogError(e.GetException(), "File watcher for '{FilePath}' encountered an error", _filePath);
            };

            fileWatcher.EnableRaisingEvents = true;

            _fileWatcher = fileWatcher;
        }
    }
}
