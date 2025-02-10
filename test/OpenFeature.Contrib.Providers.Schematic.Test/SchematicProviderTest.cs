using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using OpenFeature;
using OpenFeature.Model;
using SchematicHQ.Client;

namespace OpenFeature.Contrib.Providers.Schematic.Tests
{
    public class SchematicProviderTests
    {
        private SchematicProvider CreateProvider(ClientOptions options = null)
        {
            if (options == null)
            {
                options = new ClientOptions { Offline = true };
            }
            return new SchematicProvider("dummy-api-key", options);
        }

        [Fact]
        public void GetMetadata_Returns_Correct_Name()
        {
            var provider = CreateProvider();
            var metadata = provider.GetMetadata();
            Assert.NotNull(metadata);
            Assert.Equal("schematic-provider", metadata.Name);
        }

        [Fact]
        public async Task ResolveBoolean_Returns_Correct_Value_From_FlagDefaults()
        {
            var options = new ClientOptions
            {
                Offline = true,
                FlagDefaults = new Dictionary<string, bool>
                {
                    { "test_flag", true }
                }
            };

            var provider = CreateProvider(options);
            var result = await provider.ResolveBooleanValueAsync(
                "test_flag",
                false,
                EvaluationContext.Empty,
                CancellationToken.None);
            Assert.True(result.Value);
            Assert.Equal("on", result.Variant);
            Assert.Equal("schematic evaluation", result.Reason);
        }

        [Fact]
        public async Task ResolveBoolean_Returns_Default_When_Flag_Not_Set()
        {
            var options = new ClientOptions { Offline = true };
            var provider = CreateProvider(options);
            var result = await provider.ResolveBooleanValueAsync(
                "nonexistent_flag",
                true,
                EvaluationContext.Empty,
                CancellationToken.None);
            Assert.False(result.Value);
            Assert.Equal("off", result.Variant);
            Assert.Equal("schematic evaluation", result.Reason);
        }

        [Fact]
        public async Task ResolveString_Returns_Unsupported_Type()
        {
            var provider = CreateProvider();
            var result = await provider.ResolveStringValueAsync(
                "string_flag",
                "default",
                EvaluationContext.Empty,
                CancellationToken.None);
            Assert.Equal("default", result.Value);
            Assert.Equal("unsupported type", result.Reason);
        }

        [Fact]
        public async Task ResolveInteger_Returns_Unsupported_Type()
        {
            var provider = CreateProvider();
            var result = await provider.ResolveIntegerValueAsync(
                "int_flag",
                42,
                EvaluationContext.Empty,
                CancellationToken.None);
            Assert.Equal(42, result.Value);
            Assert.Equal("unsupported type", result.Reason);
        }

        [Fact]
        public async Task ResolveDouble_Returns_Unsupported_Type()
        {
            var provider = CreateProvider();
            var result = await provider.ResolveDoubleValueAsync(
                "double_flag",
                3.14,
                EvaluationContext.Empty,
                CancellationToken.None);
            Assert.Equal(3.14, result.Value);
            Assert.Equal("unsupported type", result.Reason);
        }

        [Fact]
        public async Task ResolveStructure_Returns_Unsupported_Type()
        {
            var structure = Structure.Builder()
                .Set("key", new Value("value"))
                .Build();
            var defaultValue = new Value(structure);

            var provider = CreateProvider();
            var result = await provider.ResolveStructureValueAsync(
                "object_flag",
                defaultValue,
                EvaluationContext.Empty,
                CancellationToken.None);
            Assert.Equal(defaultValue, result.Value);
            Assert.Equal("unsupported type", result.Reason);
        }

        [Fact]
        public async Task TrackEvent_Completes_Without_Error()
        {
            var provider = CreateProvider();

            var companyStructure = Structure.Builder()
                .Set("name", new Value("test_company"))
                .Build();

            var userStructure = Structure.Builder()
                .Set("id", new Value("test_user"))
                .Build();

            var traitsStructure = Structure.Builder()
                .Set("score", new Value(100))
                .Build();

            var context = EvaluationContext.Builder()
                .Set("company", new Value(companyStructure))
                .Set("user", new Value(userStructure))
                .Set("traits", new Value(traitsStructure))
                .Build();

            await provider.TrackEventAsync("test_event", context, CancellationToken.None);
        }

        [Fact]
        public async Task Shutdown_Completes_Without_Error()
        {
            var provider = CreateProvider();
            await provider.ShutdownAsync(CancellationToken.None);
        }
    }
}
