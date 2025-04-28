using OpenFeature.Model;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using OpenTelemetry.Trace;
using System.Threading;

namespace OpenFeature.Contrib.Hooks.Otel;


/// <summary>
/// Stub.
/// </summary>
public class TracingHook : Hook
{

    /// <inheritdoc/>
    public override ValueTask AfterAsync<T>(HookContext<T> context, FlagEvaluationDetails<T> details,
        IReadOnlyDictionary<string, object> hints = null, CancellationToken cancellationToken = default)
    {
        Activity.Current?
            .SetTag("feature_flag.key", details.FlagKey)
            .SetTag("feature_flag.variant", details.Variant)
            .SetTag("feature_flag.provider_name", context.ProviderMetadata.Name)
            .AddEvent(new ActivityEvent("feature_flag", tags: new ActivityTagsCollection
            {
                ["feature_flag.key"] = details.FlagKey,
                ["feature_flag.variant"] = details.Variant,
                ["feature_flag.provider_name"] = context.ProviderMetadata.Name
            }));

        return default;
    }

    /// <inheritdoc/>
    public override ValueTask ErrorAsync<T>(HookContext<T> context, System.Exception error,
        IReadOnlyDictionary<string, object> hints = null, CancellationToken cancellationToken = default)
    {
        Activity.Current?.RecordException(error);

        return default;
    }

}


