using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.Flipt.Converters;

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
                if (reader.TryGetInt32(out var intValue)) return new Value(intValue);
                if (reader.TryGetDouble(out var dblValue)) return new Value(dblValue);
                break;
            case JsonTokenType.StartArray:
                return new Value(GenerateValueArray(ref reader, typeToConvert, options));
        }

        return value;
    }

    private IList<Value> GenerateValueArray(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var valuesArray = new List<Value>();
        var val = new Value();
        var startDepth = reader.CurrentDepth;

        while (reader.Read())
            switch (reader.TokenType)
            {
                case JsonTokenType.EndArray when reader.CurrentDepth == startDepth:
                    return valuesArray;
                case JsonTokenType.StartObject:
                    val = new Value();
                    break;
                case JsonTokenType.EndObject:
                    valuesArray.Add(val);
                    break;
                default:
                    valuesArray.Add(Read(ref reader, typeToConvert, options));
                    break;
            }

        return valuesArray;
    }

    public override void Write(Utf8JsonWriter writer, Value value, JsonSerializerOptions options)
    {
        if (value.IsList)
        {
            writer.WriteStartArray();
            foreach (var val in value.AsList!)
                writer.WriteRawValue(JsonSerializer.Serialize(val.AsObject,
                    JsonConverterExtensions.DefaultSerializerSettings));
            writer.WriteEndArray();
        }
        else
        {
            writer.WriteRawValue(JsonSerializer.Serialize(value.AsObject));
        }
    }
}