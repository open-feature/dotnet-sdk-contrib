using OpenFeature.Error;
using OpenFeature.Model;
using System.Collections.Generic;
using System.Text.Json;

namespace OpenFeature.Contrib.Providers.Flipt
{
    internal static class AttachmentParser
    {
        /// <summary>
        /// Try parse string attachment
        /// </summary>
        /// <param name="attachment">Attachment</param>
        /// <param name="value">Result value</param>
        /// <returns><c>true</c> if parsed successfully; otherwise <c>false</c>;</returns>
        public static bool TryParseString(string attachment, out string value)
        {
            if (string.IsNullOrEmpty(attachment))
            {
                value = null;
                return false;
            }

            value = attachment.Trim('"');
            return true;
        }

        /// <summary>
        /// Attempts to parse a JSON attachment into a Value object.
        /// It checks if the attachment is null or empty and tries to parse it using STJ.
        /// If successful, it converts the parsed JSON element into a Value object.
        /// </summary>
        /// <param name="attachment">JSON string attachment.</param>
        /// <param name="value">Value result.</param>
        /// <returns></returns>
        public static bool TryParseJsonValue(string attachment, out Value value)
        {
            value = null;

            if (string.IsNullOrEmpty(attachment))
            {
                return false;
            }

            try
            {
                value = ConvertJsonElementToValue(JsonDocument.Parse(attachment).RootElement);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Value ConvertJsonElementToValue(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    {
                        if (element.TryGetDateTime(out var dateTimeValue))
                        {
                            return new Value(dateTimeValue);
                        }

                        return new Value(element.GetString());
                    }
                case JsonValueKind.Number:
                    {
                        return new Value(element.GetDouble());
                    }
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return new Value(element.GetBoolean());
                case JsonValueKind.Object:
                    {
                        var structureValues = new Dictionary<string, Value>();
                        foreach (JsonProperty property in element.EnumerateObject())
                        {
                            structureValues.Add(property.Name, ConvertJsonElementToValue(property.Value));
                        }

                        return new Value(new Structure(structureValues));
                    }
                case JsonValueKind.Array:
                    {
                        var arrayValues = new List<Value>();
                        foreach (JsonElement arrayElement in element.EnumerateArray())
                        {
                            arrayValues.Add(ConvertJsonElementToValue(arrayElement));
                        }

                        return new Value(arrayValues);
                    }
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return new Value();
                default:
                    throw new ParseErrorException($"Invalid variant value: {element.GetRawText()}");
            }
        }
    }
}
