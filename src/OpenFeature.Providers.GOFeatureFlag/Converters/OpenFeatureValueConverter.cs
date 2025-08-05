using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenFeature.Model;

namespace OpenFeature.Providers.GOFeatureFlag.Converters;

/// <summary>
///     OpenFeature Value type converter
/// </summary>
public class OpenFeatureValueConverter : JsonConverter<Value>
{
    /// <inheritdoc />
    public override Value Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = new Value();
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.TryGetDateTime(out var dateTimeValue)
                    ? new Value(dateTimeValue)
                    : new Value(reader.GetString() ?? string.Empty);
            case JsonTokenType.True:
            case JsonTokenType.False:
                return new Value(reader.GetBoolean());
            case JsonTokenType.Number:
                if (reader.TryGetInt32(out var intValue))
                {
                    return new Value(intValue);
                }

                if (reader.TryGetDouble(out var dblValue))
                {
                    return new Value(dblValue);
                }

                break;
            case JsonTokenType.StartArray:
                return new Value(this.GenerateValueArray(ref reader, typeToConvert, options));
            case JsonTokenType.StartObject:
                return new Value(this.GetStructure(ref reader, typeToConvert, options));
        }

        return value;
    }

    private Structure GetStructure(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var startDepth = reader.CurrentDepth;
        var structureDictionary = new Dictionary<string, Value>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var key = reader.GetString();
                reader.Read();
                var val = this.Read(ref reader, typeToConvert, options);
                structureDictionary[key ?? string.Empty] = val;
            }

            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == startDepth)
            {
                break;
            }
        }

        return new Structure(structureDictionary);
    }


    private IList<Value> GenerateValueArray(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var valuesArray = new List<Value>();
        var startDepth = reader.CurrentDepth;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.EndArray when reader.CurrentDepth == startDepth:
                    return valuesArray;
                default:
                    valuesArray.Add(this.Read(ref reader, typeToConvert, options));
                    break;
            }
        }

        return valuesArray;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Value value, JsonSerializerOptions options)
    {
        if (value.IsList)
        {
            writer.WriteStartArray();
            foreach (var val in value.AsList!)
            {
                var jsonDoc = JsonDocument.Parse(JsonSerializer.Serialize(val.AsObject,
                    JsonConverterExtensions.DefaultSerializerSettings));
                jsonDoc.WriteTo(writer);
            }

            writer.WriteEndArray();
        }
        else
        {
            var jsonDoc = JsonDocument.Parse(JsonSerializer.Serialize(value.AsObject,
                JsonConverterExtensions.DefaultSerializerSettings));
            jsonDoc.WriteTo(writer);
        }
    }
}
