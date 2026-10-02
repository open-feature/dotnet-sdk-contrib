using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenFeature.Providers.GOFeatureFlag.Converters;
using OpenFeature.Providers.GOFeatureFlag.Exceptions;
using OpenFeature.Providers.GOFeatureFlag.Models;

namespace OpenFeature.Providers.GOFeatureFlag.Api;

/// <summary>
/// GOFeatureFlagApi is a class that provides methods to interact with the GO Feature Flag API.
/// </summary>
public class GOFeatureFlagApi
{
    private const string HeaderApplicationJson = "application/json";
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;


    /// <summary>
    /// Constructor for GOFeatureFlagApi.
    /// </summary>
    /// <param name="options">Options provided during the initialization of the provider</param>
    /// <exception cref="ArgumentNullException">Thrown when options are not provided</exception>
    public GOFeatureFlagApi(GOFeatureFlagProviderOptions options)
    {
        if (options == null) { throw new ArgumentNullException(nameof(options), "Options cannot be null"); }

        this._logger = options.Logger;
        this._httpClient = options.HttpMessageHandler != null
            ? new HttpClient(options.HttpMessageHandler)
            : new HttpClient
            {
                Timeout = options.Timeout.Ticks.Equals(0)
                    // default timeout is 10 seconds
                    ? new TimeSpan(10000 * TimeSpan.TicksPerMillisecond)
                    : options.Timeout
            };
        this._httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(HeaderApplicationJson));
        this._httpClient.BaseAddress = new Uri(options.Endpoint);

        if (options.ApiKey != null)
        {
            this._httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", options.ApiKey);
        }
    }

    /// <summary>
    ///     RetrieveFlagConfiguration is a method that retrieves the flag configuration from the GO Feature Flag API.
    /// </summary>
    /// <param name="etag">If provided, we call the API with "If-None-Match" header.</param>
    /// <param name="flags">List of flags to retrieve, if not set or empty, we will retrieve all available flags.</param>
    /// <returns>
    ///     A <see cref="FlagConfigResponse" /> when the relay proxy returned a configuration, or
    ///     <see cref="FlagConfigNotModified" /> when it answered 304 Not Modified.
    /// </returns>
    /// <exception cref="FlagConfigurationEndpointNotFoundException">Thrown if the endpoint is not reachable.</exception>
    /// <exception cref="ImpossibleToRetrieveConfigurationException">
    ///     Thrown if the endpoint is returning an error, or a configuration without a flags map.
    /// </exception>
    public async Task<FlagConfigResult> RetrieveFlagConfigurationAsync(string? etag, List<string>? flags)
    {
        var requestStr = JsonSerializer.Serialize(new FlagConfigRequest(flags));
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/flag/configuration")
        {
            Content = new StringContent(requestStr, Encoding.UTF8, HeaderApplicationJson)
        };
        // Adding the If-None-Match header if etag is provided
        if (!string.IsNullOrEmpty(etag))
        {
            httpRequest.Headers.TryAddWithoutValidation("If-None-Match", etag);
        }

        var response = await this._httpClient.SendAsync(httpRequest).ConfigureAwait(false);
        switch (response.StatusCode)
        {
            case HttpStatusCode.NotModified:
                return FlagConfigNotModified.Instance;
            case HttpStatusCode.OK:
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return HandleFlagConfigurationSuccess(response, body);
            case HttpStatusCode.NotFound:
                throw new FlagConfigurationEndpointNotFoundException();
            case HttpStatusCode.Unauthorized:
            case HttpStatusCode.Forbidden:
                throw new UnauthorizedException("Impossible to retrieve flag configuration: authentication/authorization error");
            case HttpStatusCode.BadRequest:
                var badRequestErrBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new ImpossibleToRetrieveConfigurationException(
                    "retrieve flag configuration error: Bad request: " + badRequestErrBody);
            default:
                var defaultErrBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false) ?? String.Empty;
                throw new ImpossibleToRetrieveConfigurationException(
                    "retrieve flag configuration error: unexpected http code " + defaultErrBody);
        }
    }

    /// <summary>
    ///     Sends a list of events to the GO Feature Flag data collector.
    /// </summary>
    /// <param name="eventsList">List of events</param>
    /// <param name="exporterMetadata">Metadata associated.</param>
    /// <exception cref="UnauthorizedException">Thrown when we are not authorized to call the API</exception>
    /// <exception cref="ImpossibleToSendDataToTheCollectorException">Thrown when an error occured when calling the API</exception>
    public async Task SendEventToDataCollectorAsync(List<IEvent> eventsList, ExporterMetadata exporterMetadata)
    {
        var requestStr =
            JsonSerializer.Serialize(
                new ExporterRequest { metadata = exporterMetadata.AsStructure(), Events = eventsList },
                JsonConverterExtensions.DefaultSerializerSettings);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/data/collector")
        {
            Content = new StringContent(requestStr, Encoding.UTF8, HeaderApplicationJson)
        };
        var response = await this._httpClient.SendAsync(httpRequest).ConfigureAwait(false);

        switch (response.StatusCode)
        {
            case HttpStatusCode.OK:
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                this._logger.LogInformation("Published {Count} events successfully: {Body}", eventsList.Count, body);
                return;
            case HttpStatusCode.Unauthorized:
            case HttpStatusCode.Forbidden:
                throw new UnauthorizedException("Impossible to send events: authentication/authorization error");
            case HttpStatusCode.BadRequest:
                var badRequestErrBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new ImpossibleToSendDataToTheCollectorException("Bad request: " + badRequestErrBody);
            default:
                var defaultErrBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false) ?? String.Empty;
                throw new ImpossibleToSendDataToTheCollectorException(
                    "send data to the collector error: unexpected http code " + defaultErrBody);
        }
    }


    /// <summary>
    ///     HandleFlagConfigurationSuccess is handling the 200 response of the flag configuration request.
    /// </summary>
    /// <param name="response">HTTP response.</param>
    /// <param name="body">String of the body.</param>
    /// <returns>A FlagConfigResponse object.</returns>
    /// <exception cref="ImpossibleToRetrieveConfigurationException">Thrown if the body has no flags map.</exception>
    private static FlagConfigResponse HandleFlagConfigurationSuccess(HttpResponseMessage response, string body)
    {
        var goffResp = JsonSerializer.Deserialize<FlagConfigResponse>(body,
            JsonConverterExtensions.DefaultSerializerSettings);
        // A missing or null flags map is a failed refresh: applying it would wipe every flag.
        if (goffResp?.Flags is null)
        {
            throw new ImpossibleToRetrieveConfigurationException(
                "retrieve flag configuration error: the response has no flags map");
        }

        var etagHeader = response.Headers.TryGetValues("ETag", out var values) ? values.FirstOrDefault() : null;
        var lastUpdated = response.Content.Headers.LastModified?.UtcDateTime
                          ?? DateTime.MinValue.ToUniversalTime();
        return new FlagConfigResponse
        {
            Etag = etagHeader,
            LastUpdated = lastUpdated,
            Flags = goffResp.Flags,
            // The relay proxy sends a nil enrichment map as null, which means "no enrichment".
            EvaluationContextEnrichment = goffResp.EvaluationContextEnrichment ?? new Dictionary<string, object>()
        };
    }
}
