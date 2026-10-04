using Microsoft.Extensions.Logging;

namespace OpenFeature.Providers.Flagd.Core;

/// <summary>
/// Options for <see cref="FlagdCore"/>.
/// </summary>
public sealed class FlagdCoreOptions
{
    /// <summary>
    /// The flag source selector the configurations passed to <see cref="FlagdCore"/> were requested with.
    /// </summary>
    public string SourceSelector { get; set; } = string.Empty;

    /// <summary>
    /// Logger used to report schema validation issues. When <c>null</c>, nothing is logged.
    /// </summary>
    public ILogger Logger { get; set; }
}
