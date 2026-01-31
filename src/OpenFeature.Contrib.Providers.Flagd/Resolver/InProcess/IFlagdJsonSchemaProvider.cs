using System.Threading;
using System.Threading.Tasks;

namespace OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess;

#nullable enable

internal interface IFlagdJsonSchemaProvider
{
    Task<string> ReadSchemaAsync(FlagdSchema flagdSchema, CancellationToken cancellationToken = default);
}
