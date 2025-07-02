using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using OpenFeature.Model;
using OpenFeature.Providers.GOFeatureFlag.api;
using OpenFeature.Providers.GOFeatureFlag.Exceptions;
using OpenFeature.Providers.GOFeatureFlag.Models;
using OpenFeature.Providers.GOFeatureFlag.Test.mock;
using OpenFeature.Providers.GOFeatureFlag.Test.utils;
using Xunit;

namespace OpenFeature.Providers.GOFeatureFlag.Test.api;

public class GoFeatureFlagApiTest
{
    [Collection("Constructor")]
    public class ConstructorTest
    {
        [Fact(DisplayName = "Should throw if option is missing")]
        public void ShouldThrowIfOptionMissing()
        {
            Assert.Throws<ArgumentNullException>(() => new GoFeatureFlagApi(null));
        }
    }

    [Collection("Retriever Flag Configuration")]
    public class RetrieveConfigTest
    {
        [Fact(DisplayName = "request should have an api key")]
        public async Task ShouldThrowIfFlagDisabled()
        {
            var mockHttp = new RelayProxyMock();
            var options = new GOFeatureFlagProviderOptions
            {
                HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                Endpoint = RelayProxyMock.baseUrl,
                ApiKey = "my-api-key"
            };
            var api = new GoFeatureFlagApi(options);
            await api.RetrieveFlagConfigurationAsync("", []);

            var request = mockHttp.LastRequest;
            Assert.NotNull(request);
            Assert.True(request.Headers.Contains("Authorization"));
            Assert.Equal("Bearer my-api-key", request.Headers.GetValues("Authorization").First());
        }

        [Fact(DisplayName = "request should call the configuration endpoint")]
        public async Task ShouldCallConfigurationEndpoint()
        {
            var mockHttp = new RelayProxyMock();
            var options = new GOFeatureFlagProviderOptions
            {
                HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                Endpoint = RelayProxyMock.baseUrl
            };
            var api = new GoFeatureFlagApi(options);
            await api.RetrieveFlagConfigurationAsync("", []);

            var request = mockHttp.LastRequest;
            Assert.NotNull(request);
            Assert.Equal("/v1/flag/configuration", request.RequestUri?.AbsolutePath);
        }

        [Fact(DisplayName = "request should not set an api key if empty")]
        public async Task ShouldNotSetApiKeyIfEmpty()
        {
            var mockHttp = new RelayProxyMock();
            var options = new GOFeatureFlagProviderOptions
            {
                HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                Endpoint = RelayProxyMock.baseUrl
            };
            var api = new GoFeatureFlagApi(options);
            await api.RetrieveFlagConfigurationAsync("", []);

            var request = mockHttp.LastRequest;
            Assert.NotNull(request);
            Assert.False(request.Headers.Contains("Authorization"));
        }

        [Fact(DisplayName = "request should have the default headers")]
        public async Task ShouldHaveDefaultHeaders()
        {
            var mockHttp = new RelayProxyMock();
            var options = new GOFeatureFlagProviderOptions
            {
                HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                Endpoint = RelayProxyMock.baseUrl
            };
            var api = new GoFeatureFlagApi(options);
            await api.RetrieveFlagConfigurationAsync("", []);

            var request = mockHttp.LastRequest;
            Assert.NotNull(request);
            Assert.Contains(request.Headers.Accept, h => h.MediaType == "application/json");
        }

        [Fact(DisplayName = "request should have an if-none-match header if a etag is provided")]
        public async Task ShouldHaveIfNoneMatchHeaderIfEtagProvided()
        {
            var mockHttp = new RelayProxyMock();
            var options = new GOFeatureFlagProviderOptions
            {
                HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                Endpoint = RelayProxyMock.baseUrl
            };
            var api = new GoFeatureFlagApi(options);
            await api.RetrieveFlagConfigurationAsync("12345", []);

            var request = mockHttp.LastRequest;
            Assert.NotNull(request);
            Assert.True(request.Headers.Contains("If-None-Match"));
            Assert.Equal("12345", request.Headers.GetValues("If-None-Match").First());
        }

        [Fact(DisplayName = "request should have flags in body if flags provided")]
        public async Task ShouldHaveFlagsInBodyIfFlagsProvided()
        {
            var mockHttp = new RelayProxyMock();
            var options = new GOFeatureFlagProviderOptions
            {
                HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                Endpoint = RelayProxyMock.baseUrl
            };
            var api = new GoFeatureFlagApi(options);
            await api.RetrieveFlagConfigurationAsync("", new[] { "flag1", "flag2" }.ToList());

            var request = mockHttp.LastRequest;
            Assert.NotNull(request);
            Assert.NotNull(request.Content);
            var body = await request.Content.ReadAsStringAsync();
            Assert.Contains("\"flags\":[\"flag1\",\"flag2\"]", body);
        }

        [Fact(DisplayName = "request should return a an error if 401 received")]
        public async Task ShouldReturnErrorIfUnauthorized()
        {
            var mockHttp = new RelayProxyMock();
            var options = new GOFeatureFlagProviderOptions
            {
                HttpMessageHandler = mockHttp.GetRelayProxyMock("401"),
                Endpoint = RelayProxyMock.baseUrl
            };
            var api = new GoFeatureFlagApi(options);

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                api.RetrieveFlagConfigurationAsync("", []));
        }

        [Fact(DisplayName = "request should return a an error if 403 received")]
        public async Task ShouldReturnErrorIfForbidden()
        {
            var mockHttp = new RelayProxyMock();
            var options = new GOFeatureFlagProviderOptions
            {
                HttpMessageHandler = mockHttp.GetRelayProxyMock("403"),
                Endpoint = RelayProxyMock.baseUrl
            };
            var api = new GoFeatureFlagApi(options);

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                api.RetrieveFlagConfigurationAsync("", []));
        }

        [Fact(DisplayName = "request should return a an error if 400 received")]
        public async Task RequestShouldHaveReturn400()
        {
            var mockHttp = new RelayProxyMock();
            var options = new GOFeatureFlagProviderOptions
            {
                HttpMessageHandler = mockHttp.GetRelayProxyMock("400"),
                Endpoint = RelayProxyMock.baseUrl
            };
            var api = new GoFeatureFlagApi(options);

            await Assert.ThrowsAsync<ImpossibleToRetrieveConfigurationException>(() =>
                api.RetrieveFlagConfigurationAsync("", []));
        }

        [Fact(DisplayName = "request should return a an error if 500 received")]
        public async Task RequestShouldHaveReturn500()
        {
            var mockHttp = new RelayProxyMock();
            var options = new GOFeatureFlagProviderOptions
            {
                HttpMessageHandler = mockHttp.GetRelayProxyMock("500"),
                Endpoint = RelayProxyMock.baseUrl
            };
            var api = new GoFeatureFlagApi(options);

            await Assert.ThrowsAsync<ImpossibleToRetrieveConfigurationException>(() =>
                api.RetrieveFlagConfigurationAsync("", []));
        }

        [Fact(DisplayName = "request should return a valid FlagConfigResponse if 200 received")]
        public async Task RequestShouldHaveReturn200SimpleFlags()
        {
            var mockHttp = new RelayProxyMock();
            var options = new GOFeatureFlagProviderOptions
            {
                HttpMessageHandler = mockHttp.GetRelayProxyMock("SIMPLE_FLAG_CONFIG"),
                Endpoint = RelayProxyMock.baseUrl
            };
            var api = new GoFeatureFlagApi(options);
            var want = new FlagConfigResponse
            {
                Flags = new Dictionary<string, Flag>
                {
                    {
                        "TEST", new Flag
                        {
                            Variations =
                                new Dictionary<string, object>
                                {
                                    { "on", JsonDocument.Parse("true").RootElement },
                                    { "off", JsonDocument.Parse("false").RootElement }
                                },
                            DefaultRule = new Rule { Variation = "off" }
                        }
                    },
                    {
                        "TEST2", new Flag
                        {
                            Variations =
                                new Dictionary<string, object>
                                {
                                    { "on", JsonDocument.Parse("true").RootElement },
                                    { "off", JsonDocument.Parse("false").RootElement }
                                },
                            DefaultRule = new Rule { Variation = "on" }
                        }
                    }
                },
                EvaluationContextEnrichment =
                    new Dictionary<string, object> { { "env", JsonDocument.Parse("\"production\"").RootElement } },
                Etag = "\"123456789\"",
                LastUpdated = DateTime.Parse("Wed, 21 Oct 2015 07:28:00 GMT")
            };

            var got = await api.RetrieveFlagConfigurationAsync("", []);
            Assert.Equivalent(want, got);
        }

        [Fact(DisplayName = "request should not return last modified date if invalid header")]
        public async Task RequestShouldNotReturnLastModifiedDateIfInvalidHeader()
        {
            var mockHttp = new RelayProxyMock();
            var options = new GOFeatureFlagProviderOptions
            {
                HttpMessageHandler = mockHttp.GetRelayProxyMock("SIMPLE_FLAG_CONFIG_INVALID_HEADERS"),
                Endpoint = RelayProxyMock.baseUrl
            };
            var api = new GoFeatureFlagApi(options);
            var got = await api.RetrieveFlagConfigurationAsync("", []);
            Assert.Equal(DateTime.MinValue.ToUniversalTime(), got.LastUpdated);
        }

        [Fact(DisplayName =
            "request should return a valid FlagConfigResponse without flags and context if 304 received")]
        public async Task RequestShouldHaveReturn304WithoutFlagsAndContext()
        {
            var mockHttp = new RelayProxyMock();
            var options = new GOFeatureFlagProviderOptions
            {
                HttpMessageHandler = mockHttp.GetRelayProxyMock("304"),
                Endpoint = RelayProxyMock.baseUrl
            };
            var api = new GoFeatureFlagApi(options);
            var want = new FlagConfigResponse
            {
                Etag = "\"123456789\"",
                LastUpdated = DateTime.Parse("Wed, 21 Oct 2015 07:28:00 GMT")
            };

            var got = await api.RetrieveFlagConfigurationAsync("", []);
            Assert.Equivalent(want, got);
        }
    }

    [Collection("Data Collector")]
    public class SendEventToDataCollectorTest
    {
        [Fact(DisplayName = "request should have an api key")]
        public async Task RequestShouldHaveAnApiKey()
        {
            var mockHttp = new RelayProxyMock();
            var options = new GOFeatureFlagProviderOptions
            {
                HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                Endpoint = RelayProxyMock.baseUrl,
                ApiKey = "my-api-key"
            };
            var api = new GoFeatureFlagApi(options);
            var events = new List<IEvent>();
            var exporterMetadata = new ExporterMetadata();
            await api.SendEventToDataCollectorAsync(events, exporterMetadata);

            mockHttp.LastRequest.Headers.TryGetValues("Authorization", out var values);
            Assert.NotNull(values);
            Assert.Equivalent("Bearer my-api-key", values.First());
        }

        [Fact(DisplayName = "request should not set an api key if empty")]
        public async Task RequestShouldNotSetAnApiKeyIfEmpty()
        {
            var mockHttp = new RelayProxyMock();
            var options = new GOFeatureFlagProviderOptions
            {
                HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                Endpoint = RelayProxyMock.baseUrl
            };
            var api = new GoFeatureFlagApi(options);
            var events = new List<IEvent>();
            var exporterMetadata = new ExporterMetadata();
            await api.SendEventToDataCollectorAsync(events, exporterMetadata);

            mockHttp.LastRequest.Headers.TryGetValues("Authorization", out var values);
            Assert.Null(values);
        }

        [Fact(DisplayName = "request should have the default headers")]
        public async Task RequestShouldHaveTheDefaultHeaders()
        {
            var mockHttp = new RelayProxyMock();
            var options = new GOFeatureFlagProviderOptions
            {
                HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                Endpoint = RelayProxyMock.baseUrl
            };
            var api = new GoFeatureFlagApi(options);
            var events = new List<IEvent>();
            var exporterMetadata = new ExporterMetadata();
            await api.SendEventToDataCollectorAsync(events, exporterMetadata);
            Assert.Equivalent("application/json; charset=utf-8",
                mockHttp.LastRequest.Content.Headers.ContentType.ToString());
        }

        [Fact(DisplayName = "request should have events in the body")]
        public async Task RequestShouldHaveEventsInTheBody()
        {
            var mockHttp = new RelayProxyMock();
            var options = new GOFeatureFlagProviderOptions
            {
                HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                Endpoint = RelayProxyMock.baseUrl,
                ApiKey = "my-api-key"
            };
            var api = new GoFeatureFlagApi(options);
            var events = new List<IEvent>
            {
                new FeatureEvent
                {
                    CreationDate = 1750406145,
                    ContextKind = "user",
                    Key = "TEST",
                    UserKey = "642e135a-1df9-4419-a3d3-3c42e0e67509",
                    DefaultValue = false,
                    Value = "toto",
                    Variation = "on",
                    Version = "1.0.0"
                },
                new TrackingEvent
                {
                    CreationDate = 1750406145,
                    ContextKind = "user",
                    Key = "TEST2",
                    UserKey = "642e135a-1df9-4419-a3d3-3c42e0e67509",
                    TrackingEventDetails = new Dictionary<string, Value>
                    {
                        { "action", new Value("click") }, { "label", new Value("button1") }
                    }.ToImmutableDictionary()
                }
            };

            var meta = new ExporterMetadata();
            meta.Add("env", "production");
            await api.SendEventToDataCollectorAsync(events, meta);

            var request = mockHttp.LastRequest;
            Assert.NotNull(request);
            Assert.NotNull(request.Content);
            var body = await request.Content.ReadAsStringAsync();
            var want =
                "{\n  \"meta\": {\n    \"env\": \"production\"\n  },\n  \"events\": [\n    {\n      \"kind\": \"feature\",\n      \"defaultValue\": false,\n      \"value\": \"toto\",\n      \"variation\": \"on\",\n      \"version\": \"1.0.0\",\n      \"creationDate\": 1750406145,\n      \"contextKind\": \"user\",\n      \"key\": \"TEST\",\n      \"userKey\": \"642e135a-1df9-4419-a3d3-3c42e0e67509\"\n    },\n    {\n      \"kind\": \"tracking\",\n      \"evaluationContext\": null,\n      \"trackingEventDetails\": {\n        \"action\": \"click\",\n        \"label\": \"button1\"\n      },\n      \"creationDate\": 1750406145,\n      \"contextKind\": \"user\",\n      \"key\": \"TEST2\",\n      \"userKey\": \"642e135a-1df9-4419-a3d3-3c42e0e67509\"\n    }\n  ]\n}";
            AssertUtil.JsonEqual(want, body);
        }

        [Fact(DisplayName = "request should return a an error if 401 received")]
        public async Task RequestShouldReturnAnErrorIf401Received()
        {
            var mockHttp = new RelayProxyMock();
            var options = new GOFeatureFlagProviderOptions
            {
                HttpMessageHandler = mockHttp.GetRelayProxyMock("401"),
                Endpoint = RelayProxyMock.baseUrl
            };
            var api = new GoFeatureFlagApi(options);
            var events = new List<IEvent>();
            var exporterMetadata = new ExporterMetadata();
            await Assert.ThrowsAsync<UnauthorizedException>(async () =>
                await api.SendEventToDataCollectorAsync(events, exporterMetadata));
        }

        [Fact(DisplayName = "request should return a an error if 403 received")]
        public async Task RequestShouldReturnAnErrorIf403Received()
        {
            var mockHttp = new RelayProxyMock();
            var options = new GOFeatureFlagProviderOptions
            {
                HttpMessageHandler = mockHttp.GetRelayProxyMock("403"),
                Endpoint = RelayProxyMock.baseUrl
            };
            var api = new GoFeatureFlagApi(options);
            var events = new List<IEvent>();
            var exporterMetadata = new ExporterMetadata();
            await Assert.ThrowsAsync<UnauthorizedException>(async () =>
                await api.SendEventToDataCollectorAsync(events, exporterMetadata));
        }

        [Fact(DisplayName = "request should return a an error if 400 received")]
        public async Task RequestShouldReturnAnErrorIf400Received()
        {
            var mockHttp = new RelayProxyMock();
            var options = new GOFeatureFlagProviderOptions
            {
                HttpMessageHandler = mockHttp.GetRelayProxyMock("400"),
                Endpoint = RelayProxyMock.baseUrl
            };
            var api = new GoFeatureFlagApi(options);
            var events = new List<IEvent>();
            var exporterMetadata = new ExporterMetadata();
            await Assert.ThrowsAsync<ImpossibleToSendDataToTheCollectorException>(async () =>
                await api.SendEventToDataCollectorAsync(events, exporterMetadata));
        }

        [Fact(DisplayName = "request should return a an error if 500 received")]
        public async Task RequestShouldReturnAnErrorIf500Received()
        {
            var mockHttp = new RelayProxyMock();
            var options = new GOFeatureFlagProviderOptions
            {
                HttpMessageHandler = mockHttp.GetRelayProxyMock("500"),
                Endpoint = RelayProxyMock.baseUrl
            };
            var api = new GoFeatureFlagApi(options);
            var events = new List<IEvent>();
            var exporterMetadata = new ExporterMetadata();
            await Assert.ThrowsAsync<ImpossibleToSendDataToTheCollectorException>(async () =>
                await api.SendEventToDataCollectorAsync(events, exporterMetadata));
        }
    }
}
