using OpenFeature.Constant;
using OpenFeature.Model;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.Common.Utils;

public class Event
{
    public ProviderEventTypes EventType { get; }
    public ProviderEventPayload Details { get; }

    public Event(ProviderEventTypes eventType, ProviderEventPayload details)
    {
        this.EventType = eventType;
        this.Details = details;
    }
}
