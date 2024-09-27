using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.Flipt.Converters;

public class OpenFeatureValueConverter : JsonConverter<Value>
{
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
            case JsonTokenType.Null:
            case JsonTokenType.None:
            case JsonTokenType.StartObject:
            case JsonTokenType.EndObject:
            case JsonTokenType.StartArray:
            case JsonTokenType.EndArray:
            case JsonTokenType.PropertyName:
            case JsonTokenType.Comment:
            default:
                break;
        }

        return value;
    }

    public override void Write(Utf8JsonWriter writer, Value value, JsonSerializerOptions options)
    {
        writer.WriteRawValue(JsonSerializer.Serialize(value.AsObject));
    }
}

public class StructureConverter : JsonConverter<Structure>
{
    public override Structure Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

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