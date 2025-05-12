using System.Text.Json;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.converters;

/// <summary>
///     Extensions for default JsonConverter behavior
/// </summary>
public static class JsonConverterExtensions
{
    /// <summary>
    ///     JsonConverter serializer settings for GO Feature Flag to OpenFeature model deserialization
    /// </summary>
    public static readonly JsonSerializerOptions DefaultSerializerSettings = new JsonSerializerOptions
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
