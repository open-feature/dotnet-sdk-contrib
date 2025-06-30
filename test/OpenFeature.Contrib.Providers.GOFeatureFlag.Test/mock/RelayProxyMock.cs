using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using RichardSzalay.MockHttp;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.Test.mock;

public class RelayProxyMock
{
    public static readonly string baseUrl = "http://gofeatureflag.org";
    public static readonly string mediaType = "application/json";
    public HttpRequestMessage LastRequest { get; set; }
    public int RequestCount { get; private set; }

    public MockHttpMessageHandler GetRelayProxyMock(string mode)
    {
        var mockHttp = new MockHttpMessageHandler();
        this.AddConfigurationMock(mockHttp, mode);
        this.AddDataCollector(mockHttp, mode);
        this.AddOfrep(mockHttp, mode);
        return mockHttp;
    }

    private bool RecordRequest(HttpRequestMessage request)
    {
        // Capture the request for later inspection
        this.LastRequest = request;
        this.RequestCount++;
        return true; // Always return true to allow the mock to respond
    }

    private void AddOfrep(MockHttpMessageHandler mockHttp, string mode)
    {
        var prefixEval = baseUrl + "/ofrep/v1/evaluate/flags/";
        mockHttp.When($"{prefixEval}fail_500").Respond(HttpStatusCode.InternalServerError);
        mockHttp.When($"{prefixEval}api_key_missing").Respond(HttpStatusCode.BadRequest);
        mockHttp.When($"{prefixEval}invalid_api_key").Respond(HttpStatusCode.Unauthorized);
        mockHttp.When($"{prefixEval}flag_not_found").Respond(HttpStatusCode.NotFound);
        mockHttp.When($"{prefixEval}bool_targeting_match").Respond(mediaType,
            "{ \"value\":true, \"key\":\"bool_targeting_match\", \"reason\":\"TARGETING_MATCH\", \"variant\":\"True\" }");
        mockHttp.When($"{prefixEval}disabled").Respond(mediaType,
            "{ \"value\":false, \"key\":\"disabled\", \"reason\":\"DISABLED\", \"variant\":\"defaultSdk\"}");
        mockHttp.When($"{prefixEval}disabled_double").Respond(mediaType,
            "{ \"value\":100.25, \"key\":\"disabled_double\", \"reason\":\"DISABLED\", \"variant\":\"defaultSdk\"}");
        mockHttp.When($"{prefixEval}disabled_integer").Respond(mediaType,
            "{ \"value\":100, \"key\":\"disabled_integer\", \"reason\":\"DISABLED\", \"variant\":\"defaultSdk\"}");
        mockHttp.When($"{prefixEval}disabled_object").Respond(mediaType,
            "{ \"value\":null, \"key\":\"disabled_object\", \"reason\":\"DISABLED\", \"variant\":\"defaultSdk\"}");
        mockHttp.When($"{prefixEval}disabled_string").Respond(mediaType,
            "{ \"value\":\"\", \"key\":\"disabled_string\", \"reason\":\"DISABLED\", \"variant\":\"defaultSdk\"}");
        mockHttp.When($"{prefixEval}double_key").Respond(mediaType,
            "{ \"value\":100.25, \"key\":\"double_key\", \"reason\":\"TARGETING_MATCH\", \"variant\":\"True\"}");
        mockHttp.When($"{prefixEval}flag_not_found").Respond(mediaType,
            "{ \"value\":false, \"key\":\"flag_not_found\", \"reason\":\"FLAG_NOT_FOUND\", \"variant\":\"True\"}");
        mockHttp.When($"{prefixEval}integer_key").Respond(mediaType,
            "{ \"value\":100, \"key\":\"integer_key\", \"reason\":\"TARGETING_MATCH\", \"variant\":\"True\"}");
        mockHttp.When($"{prefixEval}list_key").Respond(mediaType,
            "{ \"value\":[\"test\",\"test1\",\"test2\",\"false\",\"test3\"], \"key\":\"list_key\", \"reason\":\"TARGETING_MATCH\", \"variant\":\"True\"}");
        mockHttp.When($"{prefixEval}object_key").Respond(mediaType,
            "{ \"value\":{\"test\":\"test1\",\"test2\":false,\"test3\":123.3,\"test4\":1,\"test5\":null}, \"key\":\"object_key\", \"reason\":\"TARGETING_MATCH\", \"variant\":\"True\"}");
        mockHttp.When($"{prefixEval}string_key").Respond(mediaType,
            "{ \"value\":\"CC0000\", \"key\":\"string_key\", \"reason\":\"TARGETING_MATCH\", \"variant\":\"True\"}");
        mockHttp.When($"{prefixEval}unknown_reason").Respond(mediaType,
            "{ \"value\":\"true\", \"key\":\"unknown_reason\", \"reason\":\"CUSTOM_REASON\", \"variant\":\"True\"}");
        mockHttp.When($"{prefixEval}does_not_exists").Respond(mediaType,
            "{ \"value\":\"\", \"key\":\"does_not_exists\", \"errorCode\":\"FLAG_NOT_FOUND\", \"variant\":\"defaultSdk\", \"errorDetails\":\"flag does_not_exists was not found in your configuration\"}");
        mockHttp.When($"{prefixEval}integer_with_metadata").Respond(mediaType,
            "{ \"value\":100, \"key\":\"integer_key\", \"reason\":\"TARGETING_MATCH\", \"variant\":\"True\", \"metadata\":{\"key1\": \"key1\", \"key2\": 1, \"key3\": 1.345, \"key4\": true}}");
    }


    private void AddDataCollector(MockHttpMessageHandler mockHttp, string mode)
    {
        var path = $"{baseUrl}/v1/data/collector";
        switch (mode)
        {
            case "400":
                mockHttp.When(path).With(this.RecordRequest).Respond(HttpStatusCode.BadRequest);
                break;
            case "401":
                mockHttp.When(path).With(this.RecordRequest).Respond(HttpStatusCode.Unauthorized);
                break;
            case "403":
                mockHttp.When(path).With(this.RecordRequest).Respond(HttpStatusCode.Forbidden);
                break;
            case "500":
                mockHttp.When(path).With(this.RecordRequest).Respond(HttpStatusCode.InternalServerError);
                break;
            default:
                mockHttp.When(path).With(this.RecordRequest).Respond(mediaType,
                    "{\"ingestedContentCount\":0}");
                break;
        }
    }

    private void AddConfigurationMock(MockHttpMessageHandler mockHttp, string mode)
    {
        var path = $"{baseUrl}/v1/flag/configuration";
        switch (mode)
        {
            case "304":
                mockHttp.When(path).With(this.RecordRequest).Respond(request =>
                {
                    var response = new HttpResponseMessage(HttpStatusCode.NotModified);
                    response.Content.Headers.LastModified =
                        new DateTimeOffset(2015, 10, 21, 7, 28, 0, TimeSpan.Zero).ToUniversalTime();
                    response.Headers.ETag = new EntityTagHeaderValue("\"123456789\"");
                    return response;
                });
                break;
            case "400":
                mockHttp.When(path).With(this.RecordRequest).Respond(HttpStatusCode.BadRequest);
                break;
            case "401":
                mockHttp.When(path).With(this.RecordRequest).Respond(HttpStatusCode.Unauthorized);
                break;
            case "403":
                mockHttp.When(path).With(this.RecordRequest).Respond(HttpStatusCode.Forbidden);
                break;
            case "404":
                mockHttp.When(path).With(this.RecordRequest).Respond(HttpStatusCode.NotFound);
                break;
            case "500":
                mockHttp.When(path).With(this.RecordRequest).Respond(HttpStatusCode.InternalServerError);
                break;
            case "CHANGE_CONFIG":
                var callCount = 0;
                mockHttp.When(path).With(this.RecordRequest).Respond(request =>
                {
                    callCount++;
                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            callCount == 1
                                ? "{\n  \"flags\": {\n    \"TEST\": {\n      \"variations\": {\n        \"off\": false,\n        \"on\": true\n      },\n      \"defaultRule\": {\n        \"variation\": \"off\"\n      }\n    },\n    \"TEST2\": {\n      \"variations\": {\n        \"off\": false,\n        \"on\": true\n      },\n      \"defaultRule\": {\n        \"variation\": \"on\"\n      }\n    }\n  },\n  \"evaluationContextEnrichment\": {\n    \"env\": \"production\"\n  }\n}\n"
                                : "{\n  \"flags\": {\n    \"TEST123\": {\n      \"variations\": {\n        \"off\": false,\n        \"on\": true\n      },\n      \"defaultRule\": {\n        \"variation\": \"off\"\n      }\n    },\n    \"TEST2\": {\n      \"variations\": {\n        \"off\": false,\n        \"on\": true\n      },\n      \"defaultRule\": {\n        \"variation\": \"on\"\n      }\n    }\n  },\n  \"evaluationContextEnrichment\": {\n    \"env\": \"production\"\n  }\n}\n",
                            Encoding.UTF8,
                            mediaType
                        )
                    };
                    response.Content.Headers.LastModified =
                        callCount == 1
                            ? new DateTimeOffset(2015, 10, 21, 7, 20, 0, TimeSpan.Zero).ToUniversalTime()
                            : new DateTimeOffset(2015, 10, 21, 7, 28, 0, TimeSpan.Zero).ToUniversalTime();
                    response.Headers.ETag =
                        new EntityTagHeaderValue(callCount == 1 ? "\"123456789\"" : "\"1234567891011\"");
                    return response;
                });
                break;
            case "CHANGE_CONFIG_LAST_MODIFIED_OLDER":
                var callCountLastModified = 0;
                mockHttp.When(path).With(this.RecordRequest).Respond(request =>
                {
                    callCountLastModified++;
                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            callCountLastModified == 1
                                ? "{\n  \"flags\": {\n    \"TEST\": {\n      \"variations\": {\n        \"off\": false,\n        \"on\": true\n      },\n      \"defaultRule\": {\n        \"variation\": \"off\"\n      }\n    },\n    \"TEST2\": {\n      \"variations\": {\n        \"off\": false,\n        \"on\": true\n      },\n      \"defaultRule\": {\n        \"variation\": \"on\"\n      }\n    }\n  },\n  \"evaluationContextEnrichment\": {\n    \"env\": \"production\"\n  }\n}\n"
                                : "{\n  \"flags\": {\n    \"TEST123\": {\n      \"variations\": {\n        \"off\": false,\n        \"on\": true\n      },\n      \"defaultRule\": {\n        \"variation\": \"off\"\n      }\n    },\n    \"TEST2\": {\n      \"variations\": {\n        \"off\": false,\n        \"on\": true\n      },\n      \"defaultRule\": {\n        \"variation\": \"on\"\n      }\n    }\n  },\n  \"evaluationContextEnrichment\": {\n    \"env\": \"production\"\n  }\n}\n",
                            Encoding.UTF8,
                            mediaType
                        )
                    };
                    response.Content.Headers.LastModified =
                        callCountLastModified == 1
                            ? new DateTimeOffset(2015, 10, 21, 7, 20, 0, TimeSpan.Zero).ToUniversalTime()
                            : new DateTimeOffset(2015, 10, 21, 7, 18, 0, TimeSpan.Zero).ToUniversalTime();
                    response.Headers.ETag =
                        new EntityTagHeaderValue(callCountLastModified == 1 ? "\"123456789\"" : "\"1234567891011\"");
                    return response;
                });
                break;
            case "SIMPLE_FLAG_CONFIG":
                mockHttp.When(path).With(this.RecordRequest).Respond(request =>
                {
                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            "{\n  \"flags\": {\n    \"TEST\": {\n      \"variations\": {\n        \"off\": false,\n        \"on\": true\n      },\n      \"defaultRule\": {\n        \"variation\": \"off\"\n      }\n    },\n    \"TEST2\": {\n      \"variations\": {\n        \"off\": false,\n        \"on\": true\n      },\n      \"defaultRule\": {\n        \"variation\": \"on\"\n      }\n    }\n  },\n  \"evaluationContextEnrichment\": {\n    \"env\": \"production\"\n  }\n}\n",
                            Encoding.UTF8,
                            mediaType
                        )
                    };
                    response.Content.Headers.LastModified =
                        new DateTimeOffset(2015, 10, 21, 7, 28, 0, TimeSpan.Zero).ToUniversalTime();
                    response.Headers.ETag = new EntityTagHeaderValue("\"123456789\"");
                    return response;
                });
                break;
            case "SIMPLE_FLAG_CONFIG_INVALID_HEADERS":
                mockHttp.When(path).With(this.RecordRequest).Respond(request =>
                {
                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            "{\n  \"flags\": {\n    \"TEST\": {\n      \"variations\": {\n        \"off\": false,\n        \"on\": true\n      },\n      \"defaultRule\": {\n        \"variation\": \"off\"\n      }\n    },\n    \"TEST2\": {\n      \"variations\": {\n        \"off\": false,\n        \"on\": true\n      },\n      \"defaultRule\": {\n        \"variation\": \"on\"\n      }\n    }\n  },\n  \"evaluationContextEnrichment\": {\n    \"env\": \"production\"\n  }\n}\n",
                            Encoding.UTF8,
                            mediaType
                        )
                    };
                    response.Content.Headers.Add("invalid-last-modified", "invalid-date");
                    response.Headers.ETag = new EntityTagHeaderValue("\"123456789\"");
                    return response;
                });
                break;
            case "SCHEDULED_ROLLOUT_FLAG_CONFIG":
                mockHttp.When(path).With(this.RecordRequest).Respond(request =>
                {
                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            "{\n  \"flags\": {\n    \"my-flag\": {\n      \"variations\": {\n        \"disabled\": false,\n        \"enabled\": true\n      },\n      \"defaultRule\": {\n        \"percentage\": {\n          \"enabled\": 0,\n          \"disabled\": 100\n        }\n      },\n      \"metadata\": {\n        \"description\": \"this is a test flag\",\n        \"defaultValue\": false\n      },\n      \"scheduledRollout\": [\n        {\n          \"targeting\": [\n            {\n              \"query\": \"targetingKey eq \\\"d45e303a-38c2-11ed-a261-0242ac120002\\\"\",\n              \"variation\": \"enabled\"\n            }\n          ],\n          \"date\": \"2022-07-31T22:00:00.100Z\"\n        }\n      ]\n    },\n    \"my-flag-scheduled-in-future\": {\n      \"variations\": {\n        \"disabled\": false,\n        \"enabled\": true\n      },\n      \"defaultRule\": {\n        \"percentage\": {\n          \"enabled\": 0,\n          \"disabled\": 100\n        }\n      },\n      \"metadata\": {\n        \"description\": \"this is a test flag\",\n        \"defaultValue\": false\n      },\n      \"scheduledRollout\": [\n        {\n          \"targeting\": [\n            {\n              \"query\": \"targetingKey eq \\\"d45e303a-38c2-11ed-a261-0242ac120002\\\"\",\n              \"variation\": \"enabled\"\n            }\n          ],\n          \"date\": \"3022-07-31T22:00:00.100Z\"\n        }\n      ]\n    }\n  }\n}\n",
                            Encoding.UTF8,
                            mediaType
                        )
                    };
                    response.Content.Headers.LastModified =
                        new DateTimeOffset(2015, 10, 21, 7, 28, 0, TimeSpan.Zero).ToUniversalTime();
                    response.Headers.ETag = new EntityTagHeaderValue("\"123456789\"");
                    return response;
                });
                break;
            default:
                mockHttp.When(path).With(this.RecordRequest).Respond(mediaType,
                    "{\n  \"flags\": {\n    \"bool_targeting_match\": {\n      \"variations\": {\n        \"disabled\": false,\n        \"enabled\": true\n      },\n      \"targeting\": [\n        {\n          \"query\": \"email eq \\\"john.doe@gofeatureflag.org\\\"\",\n          \"variation\": \"enabled\"\n        }\n      ],\n      \"defaultRule\": {\n        \"percentage\": {\n          \"enabled\": 0,\n          \"disabled\": 100\n        }\n      },\n      \"metadata\": {\n        \"description\": \"this is a test flag\",\n        \"defaultValue\": false\n      }\n    },\n    \"disabled_bool\": {\n      \"disable\": true,\n      \"variations\": {\n        \"disabled\": false,\n        \"enabled\": true\n      },\n      \"defaultRule\": {\n        \"percentage\": {\n          \"enabled\": 0,\n          \"disabled\": 100\n        }\n      },\n      \"metadata\": {\n        \"description\": \"this is a test flag\",\n        \"defaultValue\": false\n      }\n    },\n    \"disabled_float\": {\n      \"disable\": true,\n      \"variations\": {\n        \"high\": 103.25,\n        \"medium\": 101.25,\n        \"low\": 100.25\n      },\n      \"defaultRule\": {\n        \"percentage\": {\n          \"low\": 0,\n          \"medium\": 0,\n          \"high\": 100\n        }\n      },\n      \"metadata\": {\n        \"description\": \"this is a test\",\n        \"defaultValue\": 100.25\n      }\n    },\n    \"disabled_int\": {\n      \"disable\": true,\n      \"variations\": {\n        \"high\": 103,\n        \"medium\": 101,\n        \"low\": 100\n      },\n      \"defaultRule\": {\n        \"percentage\": {\n          \"low\": 0,\n          \"medium\": 0,\n          \"high\": 100\n        }\n      },\n      \"metadata\": {\n        \"description\": \"this is a test\",\n        \"defaultValue\": 100\n      }\n    },\n    \"disabled_interface\": {\n      \"disable\": true,\n      \"variations\": {\n        \"varA\": {\n          \"test\": \"john\"\n        },\n        \"varB\": {\n          \"test\": \"doe\"\n        }\n      },\n      \"defaultRule\": {\n        \"percentage\": {\n          \"varA\": 0,\n          \"varB\": 100\n        }\n      },\n      \"metadata\": {\n        \"description\": \"this is a test\"\n      }\n    },\n    \"disabled_string\": {\n      \"disable\": true,\n      \"variations\": {\n        \"color1\": \"CC0002\",\n        \"color2\": \"CC0001\",\n        \"color3\": \"CC0000\"\n      },\n      \"defaultRule\": {\n        \"percentage\": {\n          \"color3\": 0,\n          \"color2\": 0,\n          \"color1\": 100\n        }\n      },\n      \"metadata\": {\n        \"description\": \"this is a test\",\n        \"defaultValue\": \"CC0000\"\n      }\n    },\n    \"double_key\": {\n      \"variations\": {\n        \"high\": 103.25,\n        \"medium\": 101.25,\n        \"low\": 100.25\n      },\n      \"targeting\": [\n        {\n          \"query\": \"email eq \\\"john.doe@gofeatureflag.org\\\"\",\n          \"variation\": \"medium\"\n        }\n      ],\n      \"defaultRule\": {\n        \"percentage\": {\n          \"high\": 0,\n          \"medium\": 0,\n          \"low\": 100\n        }\n      },\n      \"metadata\": {\n        \"description\": \"this is a test flag\",\n        \"defaultValue\": 100.25\n      }\n    },\n    \"integer_key\": {\n      \"variations\": {\n        \"high\": 103,\n        \"medium\": 101,\n        \"low\": 100\n      },\n      \"defaultRule\": {\n        \"percentage\": {\n          \"low\": 0,\n          \"medium\": 0,\n          \"high\": 100\n        }\n      },\n      \"targeting\": [\n        {\n          \"query\": \"email eq \\\"john.doe@gofeatureflag.org\\\"\",\n          \"variation\": \"medium\"\n        }\n      ],\n      \"metadata\": {\n        \"defaultValue\": 1000,\n        \"description\": \"this is a test flag\"\n      }\n    },\n    \"object_key\": {\n      \"variations\": {\n        \"varA\": {\n          \"test\": \"default\"\n        },\n        \"varB\": {\n          \"test\": \"false\"\n        }\n      },\n      \"targeting\": [\n        {\n          \"query\": \"email eq \\\"john.doe@gofeatureflag.org\\\"\",\n          \"variation\": \"varB\"\n        }\n      ],\n      \"defaultRule\": {\n        \"variation\": \"varA\"\n      }\n    },\n    \"string_key\": {\n      \"trackEvents\": false,\n      \"variations\": {\n        \"color1\": \"CC0002\",\n        \"color2\": \"CC0001\",\n        \"color3\": \"CC0000\"\n      },\n      \"defaultRule\": {\n        \"percentage\": {\n          \"color3\": 0,\n          \"color2\": 0,\n          \"color1\": 100\n        }\n      },\n      \"metadata\": {\n        \"description\": \"this is a test flag\",\n        \"defaultValue\": \"CC0000\"\n      }\n    },\n    \"string_key_with_version\": {\n      \"variations\": {\n        \"color1\": \"CC0002\",\n        \"color2\": \"CC0001\",\n        \"color3\": \"CC0000\"\n      },\n      \"defaultRule\": {\n        \"percentage\": {\n          \"color3\": 0,\n          \"color2\": 0,\n          \"color1\": 100\n        }\n      },\n      \"targeting\": [\n        {\n          \"query\": \"email eq \\\"john.doe@gofeatureflag.org\\\"\",\n          \"variation\": \"color1\"\n        }\n      ],\n      \"metadata\": {\n        \"description\": \"this is a test\",\n        \"defaultValue\": \"CC0000\"\n      }\n    },\n    \"flag-use-evaluation-context-enrichment\": {\n      \"variations\": {\n        \"A\": \"A\",\n        \"B\": \"B\"\n      },\n      \"targeting\": [\n        {\n          \"query\": \"environment eq \\\"integration-test\\\"\",\n          \"variation\": \"A\"\n        }\n      ],\n      \"defaultRule\": {\n        \"variation\": \"B\"\n      }\n    }\n  },\n  \"evaluationContextEnrichment\": {\n    \"env\": \"production\"\n  }\n}\n");
                break;
        }
    }
}
