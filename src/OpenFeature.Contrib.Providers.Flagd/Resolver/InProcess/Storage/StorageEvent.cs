namespace OpenFeature.Contrib.Providers.Flagd.Resolver.InProcess.Storage;

internal class StorageEvent
{
    internal StorageEvent(Type type, string flagConfiguration = "")
    {
        EventType = type;
        FlagConfiguration = flagConfiguration;
    }

    public enum Type
    {
        READY,
        CHANGED,
        STALE,
        ERROR,
    }

    public string FlagConfiguration { get; }
    public Type EventType { get; }
}
