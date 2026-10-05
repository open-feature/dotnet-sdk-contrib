using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace OpenFeature.Providers.Flagd.Core;

internal interface IJsonSchemaValidator
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    void Validate(string configuration);
}
