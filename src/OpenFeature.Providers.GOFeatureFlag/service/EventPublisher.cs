using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenFeature.Contrib.Providers.GOFeatureFlag.api;
using OpenFeature.Contrib.Providers.GOFeatureFlag.helper;
using OpenFeature.Contrib.Providers.GOFeatureFlag.model;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.service;

/// <summary>
///     EventPublisher is used to collect events and publish them in batch before they are published.
/// </summary>
public class EventPublisher
{
    private readonly GoFeatureFlagApi _api;

    /// <summary>
    ///     _events is a thread-safe collection of events that will be published.
    /// </summary>
    private readonly List<IEvent> _events = new();

    private readonly object _lock = new();

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
    public Task StartAsync()
    {
        Task.Run(async () => await this._periodicAsyncRunner.StartAsync().ConfigureAwait(false));
        return Task.CompletedTask;
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
        var shouldPublish = false;
        lock (this._lock)
        {
            if (this._events.Count + 1 >= this._options.MaxPendingEvents)
            {
                shouldPublish = true;
            }

            this._events.Add(eventToAdd);
        }

        if (shouldPublish)
        {
            _ = this.PublishEventsAsync();
        }
    }

    /// <summary>
    ///     Publishes the collected events to the GO Feature Flag relay proxy.
    /// </summary>
    private async Task PublishEventsAsync()
    {
        List<IEvent> eventsToPublish;
        lock (this._lock)
        {
            if (this._events.Count == 0)
            {
                return;
            }

            eventsToPublish = new List<IEvent>(this._events);
            this._events.Clear();
        }

        try
        {
            await this._api.SendEventToDataCollectorAsync(eventsToPublish, this._options.ExporterMetadata)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._options.Logger.LogError(ex, "An error occurred while publishing events: {Message}", ex.Message);
            lock (this._lock)
            {
                this._events.AddRange(eventsToPublish);
            }
        }
    }
}
