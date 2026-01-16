using System.Threading;
using System.Threading.Tasks;

namespace OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess;

#nullable enable

internal interface IFlagdJsonSchemaProvider
{
    Task<string> ReadTargetingSchemaAsync(CancellationToken cancellationToken = default);

    Task<string> ReadFlagSchemaAsync(CancellationToken cancellationToken = default);
}
