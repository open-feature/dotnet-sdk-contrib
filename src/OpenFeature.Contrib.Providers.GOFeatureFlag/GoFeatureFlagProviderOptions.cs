using System;
using System.Net.Http;
using OpenFeature.Contrib.Providers.GOFeatureFlag.models;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag;

/// <Summary>
///     GoFeatureFlagProviderOptions contains the options to initialise the provider.
/// </Summary>
public class GoFeatureFlagProviderOptions
{
    /// <Summary>
    ///     (mandatory) endpoint contains the DNS of your GO Feature Flag relay proxy
    ///     example: https://mydomain.com/gofeatureflagproxy/
    /// </Summary>
    public string Endpoint { get; set; }

    /// <Summary>
    ///     (optional) timeout we are waiting when calling the go-feature-flag relay proxy API.
    ///     Default: 10000 ms
    /// </Summary>
    public TimeSpan Timeout { get; set; } = new TimeSpan(10000 * TimeSpan.TicksPerMillisecond);

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
    public ExporterMetadata ExporterMetadata { get; set; }
}
