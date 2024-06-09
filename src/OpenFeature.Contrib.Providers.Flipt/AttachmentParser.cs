using OpenFeature.Error;
using OpenFeature.Model;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace OpenFeature.Contrib.Providers.Flipt
{
    internal static class AttachmentParser
    {
        /// <summary>
        /// Converts the JSON string representation of a number to its double-precision 
        /// floating-point number equivalent.
        /// </summary>
        /// <param name="attachment">Attachment.</param>
        /// <param name="value">Double-precision floating-point number result.</param>
        /// <returns>true if attachment was converted successfully; otherwise false.</returns>
        public static bool TryParseDouble(string attachment, out double value)
        {
            if (string.IsNullOrEmpty(attachment))
            {
                value = default;
                return false;
            }

            return Utf8Parser.TryParse(Encoding.UTF8.GetBytes(attachment), out value, out int _); ;
        }

        /// <summary>
        /// Converts the JSON string representation of a number to its 32-bit signed integer equivalent.
        /// </summary>
        /// <param name="attachment">Attachment.</param>
        /// <param name="value">32-bit signed integer result.</param>
        /// <returns>true if attachment was converted successfully; otherwise false.</returns>
        public static bool TryParseInteger(string attachment, out int value)
        {
            if (string.IsNullOrEmpty(attachment))
            {
                value = default;
                return false;
            }

            return Utf8Parser.TryParse(Encoding.UTF8.GetBytes(attachment), out value, out int _);
        }

        /// <summary>
        /// Converts the JSON string.
        /// </summary>
        /// <param name="attachment">Attachment.</param>
        /// <param name="value">String result.</param>
        /// <returns>true if attachment was converted successfully; otherwise false.</returns>
        public static bool TryParseString(string attachment, out string value)
        {
            if (string.IsNullOrEmpty(attachment))
            {
                value = default;
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
        /// <returns>true if attachment was converted successfully; otherwise false.</returns>
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
