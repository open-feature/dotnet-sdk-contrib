using System.Text.Json;
using System.Threading.Tasks;
using OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.Test.Resolver.InProcess;

public class FlagdJsonSchemaEmbeddedResourceReaderTests
{
    [Fact]
    public async Task ReadTargetingSchemaAsyncReturnsJson()
    {
        var reader = new FlagdJsonSchemaEmbeddedResourceReader();

        var schema = await reader.ReadTargetingSchemaAsync();

        Assert.False(string.IsNullOrWhiteSpace(schema));

        JsonDocument.Parse(schema); // Will throw if not valid JSON
    }

    [Fact]
    public async Task ReadFlagSchemaAsyncReturnsJson()
    {
        var reader = new FlagdJsonSchemaEmbeddedResourceReader();

        var schema = await reader.ReadFlagSchemaAsync();

        Assert.False(string.IsNullOrWhiteSpace(schema));

        JsonDocument.Parse(schema); // Will throw if not valid JSON
    }
}
