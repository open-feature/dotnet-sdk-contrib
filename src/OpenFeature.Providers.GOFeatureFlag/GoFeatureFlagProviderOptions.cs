using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenFeature.Providers.GOFeatureFlag.Models;
using Wasmtime;

namespace OpenFeature.Providers.GOFeatureFlag;

/// <Summary>
///     GoFeatureFlagProviderOptions contains the options to initialise the provider.
/// </Summary>
public class GOFeatureFlagProviderOptions
{
    /// <Summary>
    ///     (optional) interval time we poll the proxy to check if the configuration has changed. If the
    ///     cache is enabled, we will poll the relay-proxy every X milliseconds to check if the
    ///     configuration has changed. default: 120000
    /// </Summary>
    public TimeSpan FlagChangePollingIntervalMs { get; set; } = TimeSpan.FromMilliseconds(120000);


    /// <Summary>
    ///     (mandatory) endpoint contains the DNS of your GO Feature Flag relay proxy
    ///     example: https://mydomain.com/gofeatureflagproxy/
    /// </Summary>
    public string Endpoint { get; set; }

    /// <summary>
    ///     EvaluationType defines how the evaluation is done.
    ///     Default is InProcess.
    /// </summary>
    public EvaluationType EvaluationType { get; set; } = EvaluationType.InProcess;

    /// <Summary>
    ///     (optional) timeout we are waiting when calling the go-feature-flag relay proxy API.
    ///     Default: 10000 ms
    /// </Summary>
    public TimeSpan Timeout { get; set; } = new(10000 * TimeSpan.TicksPerMillisecond);

    /// <Summary>
    ///     (optional) If you want to provide your own HttpMessageHandler.
    ///     Default: null
    /// </Summary>
    public HttpMessageHandler HttpMessageHandler { get; set; }

    /// <Summary>
    ///     (optional) If the relay proxy is configured to authenticate the request, you should provide
    ///     an API Key to the provider.
    ///     Please ask the administrator of the relay proxy to provide an API Key.
    ///     (This feature is available only if you are using GO Feature Flag relay proxy v1.7.0 or above)
    ///     Default: null
    /// </Summary>
    public string ApiKey { get; set; }

    /// <summary>
    ///     (optional) ExporterMetadata are static information you can set that will be available in the
    ///     evaluation data sent to the exporter.
    /// </summary>
    public ExporterMetadata ExporterMetadata { get; set; } = new();

    /// <Summary>
    ///     (optional) If you are using in process evaluation, by default, we will load in memory all the flags available
    ///     in the relay proxy. If you want to limit the number of flags loaded in memory, you can use this parameter.
    ///     By setting this parameter, you will only load the flags available in the list.
    ///     <p>If null or empty, all the flags available in the relay proxy will be loaded.</p>
    /// </Summary>
    public List<string> EvaluationFlagList { get; set; }

    /// <summary>
    ///     Logger for the provider. When not specified <see cref="Instance" /> is used.
    /// </summary>
    public ILogger Logger { get; set; } = NullLogger.Instance;

    /// <summary>
    ///     (optional) interval time we publish statistics collection data to the proxy. The parameter is
    ///     used only if the cache is enabled, otherwise the collection of the data is done directly when
    ///     calling the evaluation API. default: 1000 ms
    /// </summary>
    public TimeSpan FlushIntervalMs { get; set; } = TimeSpan.FromMilliseconds(1000);

    /// <summary>
    ///     (optional) max pending events aggregated before publishing for collection data to the proxy.
    ///     When an event is added while an events collection is full, the event is omitted. default: 10000
    /// </summary>
    public int MaxPendingEvents { get; set; } = 10000;


    /// <summary>
    ///     (optional) disableDataCollection set to true if you don't want to collect the usage of flags retrieved in the
    ///     cache. default: false
    /// </summary>
    public bool DisableDataCollection { get; set; } = false;
}
