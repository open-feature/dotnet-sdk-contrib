using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using OpenFeature;
using OpenFeature.Model;
using OpenFeature.Contrib.Providers.Schematic;

namespace OpenFeature.Contrib.Providers.Schematic.Tests
{
    public class SchematicProviderTests
    {
        private SchematicFeatureProvider CreateProvider(ClientOptions options = null)
        {
            options ??= new ClientOptions { Offline = true };
            return new SchematicFeatureProvider("dummy-api-key", options);
        }

        [Fact]
        public void get_metadata_returns_correct_name()
        {
            var provider = CreateProvider();
            var metadata = provider.GetMetadata();
            Assert.NotNull(metadata);
            Assert.Equal("schematic-provider", metadata.Name);
        }

        [Fact]
        public async Task resolve_boolean_returns_correct_value_from_flag_defaults()
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
            var result = await provider.ResolveBooleanValueAsync("test_flag", false);
            Assert.True(result.Value);
            Assert.Equal("on", result.Variant);
            Assert.Equal("schematic evaluation", result.Reason);
        }

        [Fact]
        public async Task resolve_boolean_returns_default_when_flag_not_set()
        {
            // when no flag default is set, schematic returns false.
            var options = new ClientOptions { Offline = true };
            var provider = CreateProvider(options);
            var result = await provider.ResolveBooleanValueAsync("nonexistent_flag", true);
            Assert.False(result.Value);
            Assert.Equal("off", result.Variant);
            Assert.Equal("schematic evaluation", result.Reason);
        }

        [Fact]
        public async Task resolve_string_returns_unsupported_type()
        {
            var provider = CreateProvider();
            var result = await provider.ResolveStringValueAsync("string_flag", "default");
            Assert.Equal("default", result.Value);
            Assert.Equal("unsupported type", result.Reason);
        }

        [Fact]
        public async Task resolve_integer_returns_unsupported_type()
        {
            var provider = CreateProvider();
            var result = await provider.ResolveIntegerValueAsync("int_flag", 42);
            Assert.Equal(42, result.Value);
            Assert.Equal("unsupported type", result.Reason);
        }

        [Fact]
        public async Task resolve_double_returns_unsupported_type()
        {
            var provider = CreateProvider();
            var result = await provider.ResolveDoubleValueAsync("double_flag", 3.14);
            Assert.Equal(3.14, result.Value);
            Assert.Equal("unsupported type", result.Reason);
        }

        [Fact]
        public async Task resolve_structure_returns_unsupported_type()
        {
            var defaultObj = new Dictionary<string, string> { { "key", "value" } };
            var provider = CreateProvider();
            var result = await provider.ResolveStructureValueAsync("object_flag", defaultObj);
            Assert.Equal(defaultObj, result.Value);
            Assert.Equal("unsupported type", result.Reason);
        }

        [Fact]
        public async Task track_event_completes_without_error()
        {
            var provider = CreateProvider();
            // build an evaluation context with sample data
            var context = EvaluationContext.Builder()
                .Set("company", new Dictionary<string, string> { { "id", "your-company-id" } })
                .Set("user", new Dictionary<string, string> { { "id", "your-user-id" } })
                .Build();

            // tracking should complete without throwing
            await provider.TrackEventAsync("test_event", context);
        }

        [Fact]
        public async Task shutdown_completes_without_error()
        {
            var provider = CreateProvider();
            await provider.ShutdownAsync();
        }
    }
}
