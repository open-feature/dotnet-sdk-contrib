using System.Text.Json;

namespace OpenFeature.Contrib.Providers.Flipt.Converters;

/// <summary>
///     Extensions for default JsonConverter behavior
/// </summary>
public static class JsonConverterExtensions
{
    public static readonly JsonSerializerOptions DefaultSerializerSettings = new()
    {
        WriteIndented = true,
        Converters =
        {
            new OpenFeatureStructureConverter(),
            new OpenFeatureValueConverter()
        }
    };
}