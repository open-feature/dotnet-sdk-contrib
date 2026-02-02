using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess;

internal sealed class FlagdJsonSchemaEmbeddedResourceReader : IFlagdJsonSchemaProvider
{
    const string TargetingJsonResourceName = "OpenFeature.Contrib.Providers.Flagd.Resources.targeting.json";
    const string FlagJsonResourceName = "OpenFeature.Contrib.Providers.Flagd.Resources.flags.json";

    public Task<string> ReadSchemaAsync(FlagdSchema flagdSchema, CancellationToken cancellationToken = default)
    {
        return flagdSchema switch
        {
            FlagdSchema.Targeting => this.ReadAsStringAsync(TargetingJsonResourceName, cancellationToken),
            FlagdSchema.Flags => this.ReadAsStringAsync(FlagJsonResourceName, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(flagdSchema), flagdSchema, null)
        };
    }

    private async Task<string> ReadAsStringAsync(string resourceName, CancellationToken cancellationToken = default)
    {
        var assembly = typeof(FlagdJsonSchemaEmbeddedResourceReader).Assembly!;

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Embedded resource not found: '{resourceName}'.");
        }

        using var streamReader = new StreamReader(stream);

#if NET8_0_OR_GREATER
        return await streamReader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
#else
        return await streamReader.ReadToEndAsync().ConfigureAwait(false);
#endif
    }
}
