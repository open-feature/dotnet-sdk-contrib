using Microsoft.Extensions.Logging;

namespace OpenFeature.Providers.Flagd.Core;

/// <summary>
/// Options for <see cref="FlagdCore"/>.
/// </summary>
public sealed class FlagdCoreOptions
{
    /// <summary>
    /// Logger used to report schema validation issues. When <c>null</c>, nothing is logged.
    /// </summary>
    public ILogger Logger { get; set; }
}
