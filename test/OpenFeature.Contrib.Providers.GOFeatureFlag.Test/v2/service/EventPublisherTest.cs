using System;
using System.Net.Http;
using System.Threading.Tasks;
using OpenFeature.Contrib.Providers.GOFeatureFlag.Test.mock;
using OpenFeature.Contrib.Providers.GOFeatureFlag.Test.v2.utils;
using OpenFeature.Contrib.Providers.GOFeatureFlag.v2;
using OpenFeature.Contrib.Providers.GOFeatureFlag.v2.api;
using OpenFeature.Contrib.Providers.GOFeatureFlag.v2.model;
using OpenFeature.Contrib.Providers.GOFeatureFlag.v2.service;
using Xunit;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.Test.v2.service;

public class EventPublisherTests
{
    private readonly GoFeatureFlagApi _apiMock;
    private readonly HttpMessageHandler _httpMessageHandler;
    private readonly RelayProxyMock _mockHttp;
    private readonly GoFeatureFlagProviderOptions _options;

    public EventPublisherTests()
    {
        this._mockHttp = new RelayProxyMock();
        this._httpMessageHandler = this._mockHttp.GetRelayProxyMock("");
        this._options = new GoFeatureFlagProviderOptions
        {
            Endpoint = RelayProxyMock.baseUrl,
            HttpMessageHandler = this._httpMessageHandler,
            FlushIntervalMs = TimeSpan.FromMilliseconds(100)
        };
        this._apiMock = new GoFeatureFlagApi(this._options);
    }

    [Fact]
    public async Task StartAsync_ShouldStartPeriodicRunner()
    {
        var publisher = new EventPublisher(this._apiMock, this._options);
        await publisher.StartAsync();
// No exception means success, as PeriodicAsyncRunner is internal
    }

    [Fact]
    public async Task StopAsync_ShouldStopPeriodicRunner()
    {
        var publisher = new EventPublisher(this._apiMock, this._options);
        await publisher.StartAsync();
        await publisher.StopAsync();
// No exception means success
    }

    [Fact]
    public void AddEvent_ShouldAddEvent()
    {
        var publisher = new EventPublisher(this._apiMock, this._options);
        var eventMock = new FeatureEvent
        {
            CreationDate = 1750406145,
            ContextKind = "user",
            Key = "TEST",
            UserKey = "642e135a-1df9-4419-a3d3-3c42e0e67509",
            DefaultValue = false,
            Value = "toto",
            Variation = "on",
            Version = "1.0.0"
        };
        publisher.AddEvent(eventMock);
// No exception means event was added
    }

    [Fact]
    public async Task PublishEventsAsync_ShouldSendEvents()
    {
        var publisher = new EventPublisher(this._apiMock, this._options);
        await publisher.StartAsync();
        var eventMock = new FeatureEvent
        {
            CreationDate = 1750406145,
            ContextKind = "user",
            Key = "TEST",
            UserKey = "642e135a-1df9-4419-a3d3-3c42e0e67509",
            DefaultValue = false,
            Value = "toto",
            Variation = "on",
            Version = "1.0.0"
        };
        publisher.AddEvent(eventMock);
        await Task.Delay(TimeSpan.FromMilliseconds(200));

        var got = await this._mockHttp.LastRequest.Content.ReadAsStringAsync();
        var want =
            "{\"meta\": {},\"events\": [{\"kind\": \"feature\",\"defaultValue\": false,\"value\": \"toto\",\"variation\": \"on\",\"version\": \"1.0.0\",\"creationDate\": 1750406145,\"contextKind\": \"user\",\"key\": \"TEST\",\"userKey\": \"642e135a-1df9-4419-a3d3-3c42e0e67509\"}]}";
        AssertUtil.JsonEqual(want, got);
    }

    [Fact]
    public async Task PublishEventsAsync_ShouldSendEventsMultipleTimes()
    {
        var publisher = new EventPublisher(this._apiMock, this._options);
        await publisher.StartAsync();
        var eventMock = new FeatureEvent
        {
            CreationDate = 1750406145,
            ContextKind = "user",
            Key = "TEST",
            UserKey = "642e135a-1df9-4419-a3d3-3c42e0e67509",
            DefaultValue = false,
            Value = "toto",
            Variation = "on",
            Version = "1.0.0"
        };
        publisher.AddEvent(eventMock);
        await Task.Delay(TimeSpan.FromMilliseconds(200));

        var got = await this._mockHttp.LastRequest.Content.ReadAsStringAsync();
        var want =
            "{\"meta\": {},\"events\": [{\"kind\": \"feature\",\"defaultValue\": false,\"value\": \"toto\",\"variation\": \"on\",\"version\": \"1.0.0\",\"creationDate\": 1750406145,\"contextKind\": \"user\",\"key\": \"TEST\",\"userKey\": \"642e135a-1df9-4419-a3d3-3c42e0e67509\"}]}";
        AssertUtil.JsonEqual(want, got);

        await Task.Delay(TimeSpan.FromMilliseconds(200));
        var eventMock2 = new FeatureEvent
        {
            CreationDate = 1750406147,
            ContextKind = "user",
            Key = "TEST",
            UserKey = "642e135a-1df9-4419-a3d3-3c42e0e67509",
            DefaultValue = false,
            Value = "second value",
            Variation = "on",
            Version = "1.0.0"
        };
        publisher.AddEvent(eventMock2);
        await Task.Delay(TimeSpan.FromMilliseconds(200));
        var got2 = await this._mockHttp.LastRequest.Content.ReadAsStringAsync();
        var want2 =
            "{\"meta\": {},\"events\": [{\"kind\": \"feature\",\"defaultValue\": false,\"value\": \"second value\",\"variation\": \"on\",\"version\": \"1.0.0\",\"creationDate\": 1750406147,\"contextKind\": \"user\",\"key\": \"TEST\",\"userKey\": \"642e135a-1df9-4419-a3d3-3c42e0e67509\"}]}";
        AssertUtil.JsonEqual(want2, got2);
        Assert.True(this._mockHttp.RequestCount >= 2,
            $"Expected at least 2 requests, but got {this._mockHttp.RequestCount}");
    }

    [Fact]
    public async Task PublishEventsAsync_ShouldNotCallApiIfNoEventSubmitted()
    {
        var publisher = new EventPublisher(this._apiMock, this._options);
        await publisher.StartAsync();
        await Task.Delay(TimeSpan.FromMilliseconds(200));
        Assert.Equal(0, this._mockHttp.RequestCount);
        await publisher.StopAsync();
    }

    [Fact]
    public async Task PublishEventsAsync_ShouldSendEventsIfMaxPendingEventsReached()
    {
        var proxyMock = new RelayProxyMock();
        var handler = proxyMock.GetRelayProxyMock("");
        var options = new GoFeatureFlagProviderOptions
        {
            Endpoint = RelayProxyMock.baseUrl,
            HttpMessageHandler = handler,
            FlushIntervalMs = TimeSpan.FromMilliseconds(1000),
            MaxPendingEvents = 2
        };
        var api = new GoFeatureFlagApi(this._options);

        var publisher = new EventPublisher(api, options);
        await publisher.StartAsync();
        var eventMock1 = new FeatureEvent
        {
            CreationDate = 1750406145,
            ContextKind = "user",
            Key = "TEST",
            UserKey = "642e135a-1df9-4419-a3d3-3c42e0e67509",
            DefaultValue = false,
            Value = "toto",
            Variation = "on",
            Version = "1.0.0"
        };
        var eventMock2 = new FeatureEvent
        {
            CreationDate = 1750406147,
            ContextKind = "user",
            Key = "TEST",
            UserKey = "642e135a-1df9-4419-a3d3-3c42e0e67509",
            DefaultValue = false,
            Value = "toto",
            Variation = "on",
            Version = "1.0.0"
        };
        var eventMock3 = new FeatureEvent
        {
            CreationDate = 1750406149,
            ContextKind = "user",
            Key = "TEST",
            UserKey = "642e135a-1df9-4419-a3d3-3c42e0e67509",
            DefaultValue = false,
            Value = "toto",
            Variation = "on",
            Version = "1.0.0"
        };
        publisher.AddEvent(eventMock1);
        publisher.AddEvent(eventMock2);
        publisher.AddEvent(eventMock3);

        await Task.Delay(TimeSpan.FromMilliseconds(100));
        var got = await this._mockHttp.LastRequest.Content.ReadAsStringAsync();

        var want =
            "{\"meta\": {},\"events\":[{\"kind\":\"feature\",\"defaultValue\": false,\"value\":\"toto\",\"variation\":\"on\",\"version\":\"1.0.0\",\"creationDate\":1750406145,\"contextKind\":\"user\",\"key\":\"TEST\",\"userKey\":\"642e135a-1df9-4419-a3d3-3c42e0e67509\"},{\"kind\":\"feature\",\"defaultValue\":false,\"value\": \"toto\",\"variation\": \"on\",\"version\": \"1.0.0\",\"creationDate\": 1750406147,\"contextKind\": \"user\",\"key\": \"TEST\",\"userKey\": \"642e135a-1df9-4419-a3d3-3c42e0e67509\"},{\"kind\": \"feature\",\"defaultValue\": false,\"value\": \"toto\",\"variation\": \"on\",\"version\": \"1.0.0\",\"creationDate\": 1750406149,\"contextKind\": \"user\",\"key\": \"TEST\",\"userKey\": \"642e135a-1df9-4419-a3d3-3c42e0e67509\"}]}";
        AssertUtil.JsonEqual(want, got);
    }
}
