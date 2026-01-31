using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NJsonSchema;
using NJsonSchema.Generation;

namespace OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess;

internal class JsonSchemaValidator : IJsonSchemaValidator
{
    private readonly ILogger _logger;
    private readonly IFlagdJsonSchemaProvider _flagdJsonSchemaProvider;

    private JsonSchema _validator;

    internal JsonSchemaValidator(ILogger logger)
        : this(logger, new FlagdJsonSchemaEmbeddedResourceReader())
    {
    }

    internal JsonSchemaValidator(ILogger logger, IFlagdJsonSchemaProvider flagdJsonSchemaProvider)
    {
        this._logger = logger;
        this._flagdJsonSchemaProvider = flagdJsonSchemaProvider;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var targetingJson = await this._flagdJsonSchemaProvider.ReadSchemaAsync(FlagdSchema.Targeting, cancellationToken).ConfigureAwait(false);
            var targetingSchema = await JsonSchema.FromJsonAsync(targetingJson, "targeting.json", schema =>
            {
                var schemaResolver = new JsonSchemaResolver(schema, new SystemTextJsonSchemaGeneratorSettings());
                var resolver = new JsonReferenceResolver(schemaResolver);

                return resolver;
            }, cancellationToken).ConfigureAwait(false);

            var flagJson = await this._flagdJsonSchemaProvider.ReadSchemaAsync(FlagdSchema.Flags, cancellationToken).ConfigureAwait(false);
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
