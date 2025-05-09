using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.models;

/// <summary>
///     This class represent the exporter metadata that will be sent in your evaluation data collectore
/// </summary>
public class ExporterMetadata
{
    private readonly StructureBuilder _exporterMetadataBuilder = Structure.Builder();

    /// <summary>
    ///     Add metadata to the exporter
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void Add(string key, string value)
    {
        _exporterMetadataBuilder.Set(key, value);
    }

    /// <summary>
    ///     Add metadata to the exporter
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void Add(string key, bool value)
    {
        _exporterMetadataBuilder.Set(key, value);
    }

    /// <summary>
    ///     Add metadata to the exporter
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void Add(string key, double value)
    {
        _exporterMetadataBuilder.Set(key, value);
    }

    /// <summary>
    ///     Add metadata to the exporter
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void Add(string key, int value)
    {
        _exporterMetadataBuilder.Set(key, value);
    }

    /// <summary>
    ///     Return the metadata as a structure
    /// </summary>
    /// <returns></returns>
    public Structure AsStructure()
    {
        return _exporterMetadataBuilder.Build();
    }
}
