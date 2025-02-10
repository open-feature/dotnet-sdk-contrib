using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenFeature;
using OpenFeature.Model;
using SchematicHQ.Client;

namespace OpenFeature.Contrib.Providers.Schematic
{
    public class SchematicFeatureProvider : FeatureProvider
    {
        private readonly Schematic _schematic;
        private readonly ISchematicLogger _logger;

        public SchematicFeatureProvider(string apiKey, ClientOptions? options = null)
        {
            options ??= new ClientOptions();
            _logger = options.Logger ?? new ConsoleLogger();
            _schematic = new Schematic(apiKey, options);
        }

        public override Metadata GetMetadata() => new Metadata("schematic-provider");

        public override async Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(
            string flagKey,
            bool defaultValue,
            EvaluationContext? context = null,
            CancellationToken cancellationToken = default)
        {
            _logger.Debug("evaluating boolean flag: {0}", flagKey);
            var company = context?.GetValue("company") as Dictionary<string, string>;
            var user = context?.GetValue("user") as Dictionary<string, string>;

            try
            {
                bool value = await _schematic.CheckFlag(flagKey, company, user);
                _logger.Debug("evaluated flag: {0} => {1}", flagKey, value);
                return new ResolutionDetails<bool>(value, value ? "on" : "off", "schematic evaluation");
            }
            catch (Exception ex)
            {
                _logger.Error("error evaluating flag {0}: {1}. using default {2}", flagKey, ex.Message, defaultValue);
                return new ResolutionDetails<bool>(defaultValue, defaultValue ? "on" : "off", "error", "provider_error");
            }
        }

        public override Task<ResolutionDetails<string>> ResolveStringValueAsync(
            string flagKey,
            string defaultValue,
            EvaluationContext? context = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ResolutionDetails<string>(defaultValue, defaultValue, "unsupported type"));
        }

        public override Task<ResolutionDetails<int>> ResolveIntegerValueAsync(
            string flagKey,
            int defaultValue,
            EvaluationContext? context = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ResolutionDetails<int>(defaultValue, defaultValue.ToString(), "unsupported type"));
        }

        public override Task<ResolutionDetails<double>> ResolveDoubleValueAsync(
            string flagKey,
            double defaultValue,
            EvaluationContext? context = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ResolutionDetails<double>(defaultValue, defaultValue.ToString(), "unsupported type"));
        }

        public override Task<ResolutionDetails<object>> ResolveStructureValueAsync(
            string flagKey,
            object defaultValue,
            EvaluationContext? context = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ResolutionDetails<object>(defaultValue, defaultValue?.ToString() ?? string.Empty, "unsupported type"));
        }

        public override Task InitializeAsync(ProviderConfiguration? configuration, CancellationToken cancellationToken = default)
        {
            _logger.Debug("initializing schematic provider");
            return Task.CompletedTask;
        }

        public async Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            _logger.Debug("shutting down schematic provider");
            await _schematic.Shutdown();
        }

        public Task TrackEventAsync(string eventName, EvaluationContext? context = null, CancellationToken cancellationToken = default)
        {
            _logger.Debug("tracking event: {0}", eventName);
            var company = context?.GetValue("company") as Dictionary<string, string>;
            var user = context?.GetValue("user") as Dictionary<string, string>;
            var traits = context?.GetValue("traits") as Dictionary<string, object>;
            _schematic.Track(eventName, company, user, traits);
            return Task.CompletedTask;
        }
    }
}
