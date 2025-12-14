using System.Threading.Tasks;
using OpenFeature.Model;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.Test;

public class SyncMetadataHookTests
{
    [Fact]
    public async Task BeforeAsync_ReturnsCorrectContext()
    {
        // Arrange
        var evaluationContext = EvaluationContext.Builder()
            .Set("key1", "value1")
            .Set("key2", 1.0)
            .Build();

        var hook = new SyncMetadataHook(() => evaluationContext);
        var clientMetadata = new ClientMetadata("test-client", "1.0.0");
        var providerMetadata = new Metadata("test-provider");
        var innerContext = EvaluationContext.Empty;
        var hookContext = new HookContext<bool>("key", false, Constant.FlagValueType.Boolean, clientMetadata, providerMetadata, innerContext);

        // Act
        var actualContext = await hook.BeforeAsync(hookContext);

        // Assert
        var expected = evaluationContext.AsDictionary();
        foreach (var kvp in actualContext.AsDictionary())
        {
            Assert.True(expected.ContainsKey(kvp.Key));
            Assert.Equal(kvp.Value, expected[kvp.Key]);
        }
    }

    [Fact]
    public async Task BeforeAsync_ReturnsNullContext()
    {
        // Arrange
        var hook = new SyncMetadataHook(() => null);
        var clientMetadata = new ClientMetadata("test-client", "1.0.0");
        var providerMetadata = new Metadata("test-provider");
        var innerContext = EvaluationContext.Empty;
        var hookContext = new HookContext<bool>("key", false, Constant.FlagValueType.Boolean, clientMetadata, providerMetadata, innerContext);

        // Act
        var actualContext = await hook.BeforeAsync(hookContext);

        // Assert
        Assert.Null(actualContext);
    }
}
