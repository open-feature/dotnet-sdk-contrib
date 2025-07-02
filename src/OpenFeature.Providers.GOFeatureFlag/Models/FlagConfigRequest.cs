using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenFeature.Providers.GOFeatureFlag.Models;

/// <summary>
///     Represents the request body for the flag configuration API.
/// </summary>
public class FlagConfigRequest
{
    /// <summary>
    ///     Constructor for FlagConfigRequest.
    ///     Specifies the flags to be checked in the flag management system.
    /// </summary>
    /// <param name="flags"></param>
    public FlagConfigRequest(List<string> flags)
    {
        this.Flags = flags ?? new List<string>();
    }

    /// <summary>
    ///     List of flag keys to be checked in the flag management system.
    ///     If the list is empty, all flags will be returned.
    /// </summary>
    [JsonPropertyName("flags")]
    public List<string> Flags { get; set; }
}
