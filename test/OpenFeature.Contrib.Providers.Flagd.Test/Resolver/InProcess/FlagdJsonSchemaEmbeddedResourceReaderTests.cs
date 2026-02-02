using System.Text.Json;
using System.Threading.Tasks;
using OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.Test.Resolver.InProcess;

public class FlagdJsonSchemaEmbeddedResourceReaderTests
{
    [Fact]
    public async Task ReadSchemaAsync_WithTargetingSchema_ReturnsJson()
    {
        var reader = new FlagdJsonSchemaEmbeddedResourceReader();

        var schema = await reader.ReadSchemaAsync(FlagdSchema.Targeting);

        Assert.False(string.IsNullOrWhiteSpace(schema));

        JsonDocument.Parse(schema); // Will throw if not valid JSON
    }

    [Fact]
    public async Task ReadSchemaAsync_WithFlagSchema_ReturnsJson()
    {
        var reader = new FlagdJsonSchemaEmbeddedResourceReader();

        var schema = await reader.ReadSchemaAsync(FlagdSchema.Flags);

        Assert.False(string.IsNullOrWhiteSpace(schema));

        JsonDocument.Parse(schema); // Will throw if not valid JSON
    }
}
