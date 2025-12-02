using System.Collections.Generic;
using OpenFeature.Constant;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.Flagd;

public class FlagdProviderEvent
{
    public ProviderEventTypes EventType { get; set; }

    public List<string> FlagsChanged { get; set; }

    public Structure SyncMetadata { get; set; }
}
