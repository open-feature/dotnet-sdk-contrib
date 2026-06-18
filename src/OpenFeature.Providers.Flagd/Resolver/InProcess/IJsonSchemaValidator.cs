using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace OpenFeature.Providers.Flagd.Resolver.InProcess;

internal interface IJsonSchemaValidator
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    void Validate(string configuration);
}
