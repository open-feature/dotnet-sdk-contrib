using System.Threading.Channels;
using System.Threading.Tasks;

namespace OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess.Storage;

internal interface Storage
{
    Task Init();
    Task Shutdown();
    Channel<StorageEvent> EventChannel();
}
