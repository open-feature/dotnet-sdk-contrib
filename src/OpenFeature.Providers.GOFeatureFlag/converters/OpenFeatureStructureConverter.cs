using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenFeature.Model;

namespace OpenFeature.Providers.GOFeatureFlag.Converters;

/// <summary>
///     OpenFeatureStructureConverter
/// </summary>
public class OpenFeatureStructureConverter : JsonConverter<Structure>
{
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Structure value, JsonSerializerOptions options)
    {
        var jsonDoc = JsonDocument.Parse(JsonSerializer.Serialize(value.AsDictionary(),
            JsonConverterExtensions.DefaultSerializerSettings));
        jsonDoc.WriteTo(writer);
    }

    /// <inheritdoc />
    public override Structure Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDocument = JsonDocument.ParseValue(ref reader);
        var jsonText = jsonDocument.RootElement.GetRawText();
        return new Structure(JsonSerializer.Deserialize<Dictionary<string, Value>>(jsonText,
            JsonConverterExtensions.DefaultSerializerSettings));
    }
}
