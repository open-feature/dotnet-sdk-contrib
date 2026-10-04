using System.Threading;
using System.Threading.Tasks;

namespace OpenFeature.Providers.Flagd.Core;

#nullable enable

internal interface IFlagdJsonSchemaProvider
{
    Task<string> ReadSchemaAsync(FlagdSchema flagdSchema, CancellationToken cancellationToken = default);
}
