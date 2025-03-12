using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenFeature;
using OpenFeature.Model;
using OpenFeature.Constant;
using SchematicHQ.Client;

namespace OpenFeature.Contrib.Providers.Schematic
{
    public class SchematicProvider : FeatureProvider
    {
        private readonly SchematicHQ.Client.Schematic _schematic;
        private readonly ISchematicLogger _logger;

        public SchematicProvider(string apiKey, ClientOptions options)
        {
            if (options == null)
            {
                options = new ClientOptions();
            }
            _logger = options.Logger ?? new ConsoleLogger();
            _schematic = new SchematicHQ.Client.Schematic(apiKey, options);
        }

        public override Metadata GetMetadata()
        {
            return new Metadata("schematic-provider");
        }

        public override async Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(
            string flagKey,
            bool defaultValue,
            EvaluationContext context,
            CancellationToken cancellationToken)
        {
            _logger.Debug("evaluating boolean flag: {0}", flagKey);

            Dictionary<string, string> company = null;
            Dictionary<string, string> user = null;

            if (context != null)
            {
                var companyValue = context.TryGetValue("company", out var companyVal) ? companyVal : null;
                if (companyValue != null)
                {
                    try
                    {
                        company = companyValue.ToObject<Dictionary<string, string>>();
                    }
                    catch (Exception)
                    {
                        _logger.Debug("error converting company to dictionary");
                    }
                }
                var userValue = context.TryGetValue("user", out var userVal) ? userVal : null;
                if (userValue != null)
                {
                    try
                    {
                        user = userValue.ToObject<Dictionary<string, string>>();
                    }
                    catch (Exception)
                    {
                        _logger.Debug("error converting user to dictionary");
                    }
                }
            }

            try
            {
                bool value = await _schematic.CheckFlag(flagKey, company, user);
                _logger.Debug("evaluated flag: {0} => {1}", flagKey, value);
                return new ResolutionDetails<bool>(
                    flagKey: flagKey,
                    value: value,
                    variant: value ? "on" : "off",
                    reason: "schematic evaluation"
                );
            }
            catch (Exception ex)
            {
                _logger.Debug("error evaluating flag {0}: {1}. using default {2}", flagKey, ex.Message, defaultValue);
                return new ResolutionDetails<bool>(
                    flagKey: flagKey,
                    value: defaultValue,
                    variant: defaultValue ? "on" : "off",
                    errorType: ErrorType.General,
                    reason: "error",
                    errorMessage: ex.Message
                );
            }
        }

        public override Task<ResolutionDetails<string>> ResolveStringValueAsync(
            string flagKey,
            string defaultValue,
            EvaluationContext context,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new ResolutionDetails<string>(
                flagKey: flagKey,
                value: defaultValue,
                reason: "unsupported type"
            ));
        }

        public override Task<ResolutionDetails<int>> ResolveIntegerValueAsync(
            string flagKey,
            int defaultValue,
            EvaluationContext context,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new ResolutionDetails<int>(
                flagKey: flagKey,
                value: defaultValue,
                variant: defaultValue.ToString(),
                reason: "unsupported type"
            ));
        }

        public override Task<ResolutionDetails<double>> ResolveDoubleValueAsync(
            string flagKey,
            double defaultValue,
            EvaluationContext context,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new ResolutionDetails<double>(
                flagKey: flagKey,
                value: defaultValue,
                variant: defaultValue.ToString(),
                reason: "unsupported type"
            ));
        }

        public override Task<ResolutionDetails<Value>> ResolveStructureValueAsync(
            string flagKey,
            Value defaultValue,
            EvaluationContext context,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new ResolutionDetails<Value>(
                flagKey: flagKey,
                value: defaultValue,
                variant: defaultValue?.ToString() ?? string.Empty,
                reason: "unsupported type"
            ));
        }

        public override Task InitializeAsync(EvaluationContext context, CancellationToken cancellationToken)
        {
            _logger.Debug("initializing schematic provider");
            return Task.CompletedTask;
        }

        public override async Task ShutdownAsync(CancellationToken cancellationToken)
        {
            _logger.Debug("shutting down schematic provider");
            await _schematic.Shutdown();
        }

        public Task TrackEventAsync(string eventName, EvaluationContext context, CancellationToken cancellationToken)
        {
            _logger.Debug("tracking event: {0}", eventName);

            Dictionary<string, string> company = null;
            Dictionary<string, string> user = null;
            Dictionary<string, object> traits = null;

            if (context != null)
            {
                var companyValue = context.GetValue("company");
                if (companyValue != null)
                {
                    try
                    {
                        company = companyValue.ToObject<Dictionary<string, string>>();
                    }
                    catch (Exception)
                    {
                    }
                }
                var userValue = context.GetValue("user");
                if (userValue != null)
                {
                    try
                    {
                        user = userValue.ToObject<Dictionary<string, string>>();
                    }
                    catch (Exception)
                    {
                    }
                }
                var traitsValue = context.GetValue("traits");
                if (traitsValue != null)
                {
                    try
                    {
                        traits = traitsValue.ToObject<Dictionary<string, object>>();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            _schematic.Track(eventName, company, user, traits);
            return Task.CompletedTask;
        }
    }
}
