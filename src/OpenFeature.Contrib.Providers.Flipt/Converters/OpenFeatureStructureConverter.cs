using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.Flipt.Converters;

/// <summary>
///     JsonConverter for OpenFeature Structure type
/// </summary>
public class OpenFeatureStructureConverter : JsonConverter<Structure>
{
    /// <inheritdoc />
    public override Structure Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Structure value, JsonSerializerOptions options)
    {
        var serializeOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new OpenFeatureValueConverter() }
        };
        writer.WriteRawValue(JsonSerializer.Serialize(value.AsDictionary(), serializeOptions));
    }
}