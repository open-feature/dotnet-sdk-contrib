using System.Text.Json;

namespace OpenFeature.Contrib.Providers.Flipt.Converters;

/// <summary>
///     Extensions for default JsonConverter behavior
/// </summary>
public static class JsonConverterExtensions
{
    /// <summary>
    ///     JsonConverter serializer settings for Flipt to OpenFeature model deserialization
    /// </summary>
    public static readonly JsonSerializerOptions DefaultSerializerSettings = new()
    {
        WriteIndented = true,
        AllowTrailingCommas = true,
        Converters =
        {
            new OpenFeatureStructureConverter(),
            new OpenFeatureValueConverter()
        }
    };
}
