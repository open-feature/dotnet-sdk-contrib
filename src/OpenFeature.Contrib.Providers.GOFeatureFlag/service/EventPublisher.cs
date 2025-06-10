using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenFeature.Contrib.Providers.GOFeatureFlag.v2.api;
using OpenFeature.Contrib.Providers.GOFeatureFlag.v2.helper;
using OpenFeature.Contrib.Providers.GOFeatureFlag.v2.model;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.v2.service;

/// <summary>
///     EventPublisher is used to collect events and publish them in batch before they are published.
/// </summary>
public class EventPublisher
{
    private readonly GoFeatureFlagApi _api;

    /// <summary>
    ///     _events is a thread-safe collection of events that will be published.
    /// </summary>
    private readonly ConcurrentBag<IEvent> _events = new();

    /// <summary>
    ///     ExporterMetadata contains static information about the exporter that will be sent with the events.
    /// </summary>
    private readonly GoFeatureFlagProviderOptions _options;

    /// <summary>
    ///     PeriodicAsyncRunner is used to periodically check for configuration changes.
    /// </summary>
    private readonly PeriodicAsyncRunner _periodicAsyncRunner;

    /// <summary>
    ///     Initialize the event publisher with a specified publication interval.
    /// </summary>
    /// <param name="api">GoFeatureFlagApi is the API used to communicate with the GO Feature Flag relay proxy.</param>
    /// <param name="options">GoFeatureFlagProviderOptions contains the options to initialise the provider.</param>
    public EventPublisher(GoFeatureFlagApi api, GoFeatureFlagProviderOptions options)
    {
        this._api = api ?? throw new ArgumentNullException(nameof(api), "API cannot be null");
        this._options = options ?? throw new ArgumentNullException(nameof(options), "Options cannot be null");
        this._periodicAsyncRunner =
            new PeriodicAsyncRunner(this.PublishEventsAsync, this._options.FlushIntervalMs, this._options.Logger);
    }

    /// <summary>
    ///     Starts the periodic runner that publishes events.
    /// </summary>
    public async Task StartAsync()
    {
        Task.Run(async () => await this._periodicAsyncRunner.StartAsync().ConfigureAwait(false));
    }

    /// <summary>
    ///     Stops the periodic runner that publishes events.
    /// </summary>
    public async Task StopAsync()
    {
        await this._periodicAsyncRunner.StopAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Add event for aggregation before publishing.
    /// </summary>
    public void AddEvent(IEvent eventToAdd)
    {
        if (this._events.Count + 1 >= this._options.MaxPendingEvents)
        {
            Task.Run(this.PublishEventsAsync);
        }

        this._events.Add(eventToAdd);
    }

    /// <summary>
    ///     Publishes the collected events to the GO Feature Flag relay proxy.
    /// </summary>
    private async Task PublishEventsAsync()
    {
        var eventsToPublish = new List<IEvent>();
        while (this._events.TryTake(out var ev))
        {
            eventsToPublish.Add(ev);
        }

        try
        {
            if (eventsToPublish.Count == 0) { return; }

            await this._api.SendEventToDataCollector(eventsToPublish, this._options.ExporterMetadata)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._options.Logger.LogError(ex, "An error occurred while publishing events: {Message}", ex.Message);
            foreach (var failedEvent in eventsToPublish)
            {
                this._events.Add(failedEvent);
            }
        }
    }
}
