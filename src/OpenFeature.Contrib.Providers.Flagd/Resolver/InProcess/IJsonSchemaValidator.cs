using System.Threading;
using System.Threading.Tasks;

namespace OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess;

internal interface IJsonSchemaValidator
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    void Validate(string configuration);
}
