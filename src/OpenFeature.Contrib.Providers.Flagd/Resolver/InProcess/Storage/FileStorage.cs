using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess.Storage;

internal class FileStorage: Storage
{
    private readonly Channel<StorageEvent> _eventChannel = Channel.CreateBounded<StorageEvent>(1);
    private readonly string _path;
    private readonly FileSystemWatcher _fileSystemWatcher;

    internal FileStorage(FlagdConfig config)
    {
        _path = config.OfflineFlagSourceFullPath;
        _fileSystemWatcher = new FileSystemWatcher(Path.GetDirectoryName(_path), Path.GetFileName(_path))
        {
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.LastWrite,
        };
        _fileSystemWatcher.Changed += (_, _) => this.HandleFileChanged();
    }

    public Task Init()
    {
        return Task.Run(() =>
        {
            var latch = new CountdownEvent(1);
            new Thread(() =>
            {
                var file = File.ReadAllText(_path);

                if (!latch.IsSet)
                {
                    latch.Signal();
                }
                this._eventChannel.Writer.TryWrite(new StorageEvent(StorageEvent.Type.READY, file));
            })
            {
                IsBackground = true
            }.Start();
            latch.Wait();
        });
    }

    public Task Shutdown()
    {
        _fileSystemWatcher.Dispose();
        return Task.CompletedTask;
    }

    public Channel<StorageEvent> EventChannel()
    {
        return this._eventChannel;
    }

    private void HandleFileChanged()
    {
        var file = File.ReadAllText(_path);
        this._eventChannel.Writer.TryWrite(new StorageEvent(StorageEvent.Type.CHANGED, file));
    }
}
