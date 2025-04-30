using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NJsonSchema;
using NJsonSchema.Generation;

namespace OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess;

internal interface IJsonSchemaValidator
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    void Validate(string configuration);
}

internal class JsonSchemaValidator : IJsonSchemaValidator
{
    private readonly HttpClient _client;
    private readonly ILogger _logger;
    private JsonSchema _validator;

    internal JsonSchemaValidator(HttpClient client, ILogger logger)
    {
        if (client == null)
        {
            client = new HttpClient
            {
                BaseAddress = new Uri("https://flagd.dev"),
            };
        }

        _client = client;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var targetingTask = _client.GetAsync("/schema/v0/targeting.json", cancellationToken);
            var flagTask = _client.GetAsync("/schema/v0/flags.json", cancellationToken);

            await Task.WhenAll(targetingTask, flagTask).ConfigureAwait(false);

            var targeting = targetingTask.Result;
            var flag = flagTask.Result;

            if (!targeting.IsSuccessStatusCode)
            {
                _logger.LogWarning("Unable to retrieve Flagd targeting JSON Schema, status code: {StatusCode}", targeting.StatusCode);
                return;
            }

            if (!flag.IsSuccessStatusCode)
            {
                _logger.LogWarning("Unable to retrieve Flagd flags JSON Schema, status code: {StatusCode}", flag.StatusCode);
                return;
            }

#if NET5_0_OR_GREATER
            var targetingJson = await targeting.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
            var targetingJson = await targeting.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif

            var targetingSchema = await JsonSchema.FromJsonAsync(targetingJson, "targeting.json", schema =>
            {
                var schemaResolver = new JsonSchemaResolver(schema, new SystemTextJsonSchemaGeneratorSettings());
                var resolver = new JsonReferenceResolver(schemaResolver);

                return resolver;
            }, cancellationToken).ConfigureAwait(false);

#if NET5_0_OR_GREATER
            var flagJson = await flag.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
            var flagJson = await flag.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
            var flagSchema = await JsonSchema.FromJsonAsync(flagJson, "flags.json", schema =>
            {
                var schemaResolver = new JsonSchemaResolver(schema, new SystemTextJsonSchemaGeneratorSettings());
                var resolver = new JsonReferenceResolver(schemaResolver);

                resolver.AddDocumentReference("targeting.json", targetingSchema);
                return resolver;
            }, cancellationToken).ConfigureAwait(false);

            _validator = flagSchema;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to retrieve Flagd flags and targeting JSON Schemas");
        }
    }

    public void Validate(string configuration)
    {
        if (_validator != null)
        {
            var errors = _validator.Validate(configuration);
            if (errors.Count > 0)
            {
                _logger.LogWarning("Validating Flagd configuration resulted in Schema Validation errors {Errors}",
                    errors);
            }
        }
    }
}
