using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OpenFeature.Constant;
using OpenFeature.Contrib.Providers.GOFeatureFlag.exception;
using OpenFeature.Contrib.Providers.GOFeatureFlag.extensions;
using OpenFeature.Contrib.Providers.GOFeatureFlag.model;
using OpenFeature.Contrib.Providers.GOFeatureFlag.Test.mock;
using OpenFeature.Model;
using Xunit;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.Test;

public class GoFeatureFlagProviderTest
{
    private static readonly EvaluationContext DefaultEvaluationContext = EvaluationContext.Builder()
        .SetTargetingKey("d45e303a-38c2-11ed-a261-0242ac120002")
        .Set("email", "john.doe@gofeatureflag.org")
        .Set("firstname", "john")
        .Set("lastname", "doe")
        .Set("anonymous", false)
        .Set("professional", true)
        .Set("rate", 3.14)
        .Set("age", 30)
        .Set("company_info",
            Structure.Builder().Set("name", "my_company").Set("size", 120).Build())
        .Set("labels",
            new Value(new List<Value> { new("pro"), new("beta") }))
        .Build();


    [Collection("In Process Evaluation")]
    public class InProcessEvaluationTest
    {
        [Fact(DisplayName = "Should use in process evaluation by default")]
        public async Task ShouldUseInProcessByDefault()
        {
            var mockHttp = new RelayProxyMock();
            var provider = new GoFeatureFlagProvider(
                new GoFeatureFlagProviderOptions
                {
                    HttpMessageHandler = mockHttp.GetRelayProxyMock(""), Endpoint = RelayProxyMock.baseUrl
                }
            );

            await Api.Instance.SetProviderAsync("client_test", provider);
            var client = Api.Instance.GetClient("client_test");
            await client.GetBooleanDetailsAsync("bool_targeting_match", false, EvaluationContext.Empty);
            var want = "/v1/flag/configuration";
            Assert.Equal(want,
                mockHttp.LastRequest.RequestUri?.AbsolutePath);
            // await Api.Instance.ShutdownAsync();
        }

        [Fact(DisplayName = "Should use in process evaluation if option is set")]
        public async Task ShouldUseInProcessIfOptionIsSet()
        {
            var mockHttp = new RelayProxyMock();
            var provider = new GoFeatureFlagProvider(
                new GoFeatureFlagProviderOptions
                {
                    HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                    Endpoint = RelayProxyMock.baseUrl,
                    EvaluationType = EvaluationType.InProcess
                }
            );

            await Api.Instance.SetProviderAsync("client_test", provider);
            var want = "/v1/flag/configuration";
            Assert.Equal(want,
                mockHttp.LastRequest.RequestUri?.AbsolutePath);
            // await Api.Instance.ShutdownAsync();
        }

        // [Fact(DisplayName = "Should throw an error if the endpoint is not available")]
        // public async Task ShouldThrowAnErrorIfEndpointNotAvailable()
        // {
        //     var mockHttp = new RelayProxyMock();
        //     var provider = new GoFeatureFlagProvider(
        //         new GoFeatureFlagProviderOptions
        //         {
        //             Endpoint = "http://localhost:9999", // Unavailable endpoint
        //             EvaluationType = EvaluationType.InProcess
        //         }
        //     );
        //     await Assert.ThrowsAsync<ImpossibleToRetrieveConfiguration>(async () =>
        //         await Api.Instance.SetProviderAsync("client_test", provider)
        //     );
        // }

        [Fact(DisplayName = "Should return FLAG_NOT_FOUND if the flag does not exists")]
        public async Task ShouldReturnFlagNotFoundIfFlagDoesNotExists()
        {
            var mockHttp = new RelayProxyMock();
            var provider = new GoFeatureFlagProvider(
                new GoFeatureFlagProviderOptions
                {
                    HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                    Endpoint = RelayProxyMock.baseUrl,
                    EvaluationType = EvaluationType.InProcess
                }
            );

            await Api.Instance.SetProviderAsync("client_test", provider);
            var client = Api.Instance.GetClient("client_test");
            var got = await client.GetBooleanDetailsAsync("DOES_NOT_EXISTS", false, EvaluationContext.Empty);
            var want = new FlagEvaluationDetails<bool>(
                "DOES_NOT_EXISTS",
                false,
                ErrorType.FlagNotFound,
                Reason.Error,
                "",
                "Flag with key 'DOES_NOT_EXISTS' not found");
            Assert.Equivalent(want, got);
            // await Api.Instance.ShutdownAsync();
        }


        [Fact(DisplayName = "Should error if we expect a boolean and got another type")]
        public async Task ShouldErrorIfWeExpectABooleanAndGotAnotherType()
        {
            var mockHttp = new RelayProxyMock();
            var provider = new GoFeatureFlagProvider(
                new GoFeatureFlagProviderOptions
                {
                    HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                    Endpoint = RelayProxyMock.baseUrl,
                    EvaluationType = EvaluationType.InProcess
                }
            );

            await Api.Instance.SetProviderAsync("client_test", provider);
            var client = Api.Instance.GetClient("client_test");
            var evaluationContext = EvaluationContext.Builder().SetTargetingKey("d4a4ed17-83ea-4cbb-a608-ac9e498e0a77")
                .Build();
            var got = await client.GetBooleanDetailsAsync("string_key", false, evaluationContext);
            var want = new FlagEvaluationDetails<bool>(
                "string_key",
                false,
                ErrorType.TypeMismatch,
                Reason.Error,
                "",
                "Flag string_key had unexpected type, expected boolean.");
            Assert.Equivalent(want, got);
            // await Api.Instance.ShutdownAsync();
        }

        [Fact(DisplayName = "Should resolve a valid boolean flag with TARGETING MATCH reason")]
        public async Task ShouldResolveAValidBooleanFlagWithTargetingMatchReason()
        {
            var mockHttp = new RelayProxyMock();
            var provider = new GoFeatureFlagProvider(
                new GoFeatureFlagProviderOptions
                {
                    HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                    Endpoint = RelayProxyMock.baseUrl,
                    EvaluationType = EvaluationType.InProcess
                }
            );

            await Api.Instance.SetProviderAsync("client_test", provider);
            var client = Api.Instance.GetClient("client_test");
            var got = await client.GetBooleanDetailsAsync("bool_targeting_match", false,
                DefaultEvaluationContext);
            var want = new FlagEvaluationDetails<bool>(
                "bool_targeting_match",
                true,
                ErrorType.None,
                Reason.TargetingMatch,
                "enabled",
                null,
                new Dictionary<string, object> { { "description", "this is a test flag" }, { "defaultValue", false } }
                    .ToImmutableMetadata());
            Assert.Equivalent(want, got);
            // await Api.Instance.ShutdownAsync();
        }

        [Fact(DisplayName = "Should resolve a valid string flag with TARGETING MATCH reason")]
        public async Task ShouldResolveAValidStringFlagWithTargetingMatchReason()
        {
            var mockHttp = new RelayProxyMock();
            var provider = new GoFeatureFlagProvider(
                new GoFeatureFlagProviderOptions
                {
                    HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                    Endpoint = RelayProxyMock.baseUrl,
                    EvaluationType = EvaluationType.InProcess
                }
            );

            await Api.Instance.SetProviderAsync("client_test", provider);
            var client = Api.Instance.GetClient("client_test");
            var got = await client.GetStringDetailsAsync("string_key", "",
                DefaultEvaluationContext);
            var want = new FlagEvaluationDetails<string>(
                "string_key",
                "CC0002",
                ErrorType.None,
                Reason.Static,
                "color1",
                null,
                new Dictionary<string, object>
                    {
                        { "description", "this is a test flag" }, { "defaultValue", "CC0000" }
                    }
                    .ToImmutableMetadata());
            Assert.Equivalent(want, got);
            // await Api.Instance.ShutdownAsync();
        }

        [Fact(DisplayName = "Should resolve a valid double flag with TARGETING MATCH reason")]
        public async Task ShouldResolveAValidDoubleFlagWithTargetingMatchReason()
        {
            var mockHttp = new RelayProxyMock();
            var provider = new GoFeatureFlagProvider(
                new GoFeatureFlagProviderOptions
                {
                    HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                    Endpoint = RelayProxyMock.baseUrl,
                    EvaluationType = EvaluationType.InProcess
                }
            );

            await Api.Instance.SetProviderAsync("client_test", provider);
            var client = Api.Instance.GetClient("client_test");
            var got = await client.GetDoubleDetailsAsync("double_key", 100.10,
                DefaultEvaluationContext);
            var want = new FlagEvaluationDetails<double>(
                "double_key",
                101.25,
                ErrorType.None,
                Reason.TargetingMatch,
                "medium",
                null,
                new Dictionary<string, object> { { "description", "this is a test flag" }, { "defaultValue", 100.25 } }
                    .ToImmutableMetadata());
            Assert.Equivalent(want, got);
            // await Api.Instance.ShutdownAsync();
        }

        [Fact(DisplayName = "Should resolve a valid integer flag with TARGETING MATCH reason")]
        public async Task ShouldResolveAValidIntegerFlagWithTargetingMatchReason()
        {
            var mockHttp = new RelayProxyMock();
            var provider = new GoFeatureFlagProvider(
                new GoFeatureFlagProviderOptions
                {
                    HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                    Endpoint = RelayProxyMock.baseUrl,
                    EvaluationType = EvaluationType.InProcess
                }
            );

            await Api.Instance.SetProviderAsync("client_test", provider);
            var client = Api.Instance.GetClient("client_test");
            var got = await client.GetIntegerDetailsAsync("integer_key", 1000,
                DefaultEvaluationContext);
            var want = new FlagEvaluationDetails<int>(
                "integer_key",
                101,
                ErrorType.None,
                Reason.TargetingMatch,
                "medium",
                null,
                new Dictionary<string, object> { { "description", "this is a test flag" }, { "defaultValue", 1000 } }
                    .ToImmutableMetadata());
            Assert.Equivalent(want, got);
            // await Api.Instance.ShutdownAsync();
        }

        [Fact(DisplayName = "Should resolve a valid object flag with TARGETING MATCH reason")]
        public async Task ShouldResolveAValidObjectFlagWithTargetingMatchReason()
        {
            var mockHttp = new RelayProxyMock();
            var provider = new GoFeatureFlagProvider(
                new GoFeatureFlagProviderOptions
                {
                    HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                    Endpoint = RelayProxyMock.baseUrl,
                    EvaluationType = EvaluationType.InProcess
                }
            );

            await Api.Instance.SetProviderAsync("client_test", provider);
            var client = Api.Instance.GetClient("client_test");
            var got = await client.GetObjectDetailsAsync("object_key", new Value(), DefaultEvaluationContext);
            var want = new FlagEvaluationDetails<Value>(
                "object_key",
                new Value(Structure.Builder()
                    .Set("test", "false")
                    .Build()),
                ErrorType.None,
                Reason.TargetingMatch,
                "varB");
            Assert.Equivalent(want, got);
            // await Api.Instance.ShutdownAsync();
        }

        [Fact(DisplayName = "Should use boolean default value if the flag is disabled")]
        public async Task ShouldUseBooleanDefaultValueIfTheFlagIsDisabled()
        {
            var mockHttp = new RelayProxyMock();
            var provider = new GoFeatureFlagProvider(
                new GoFeatureFlagProviderOptions
                {
                    HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                    Endpoint = RelayProxyMock.baseUrl,
                    EvaluationType = EvaluationType.InProcess
                }
            );

            await Api.Instance.SetProviderAsync("client_test", provider);
            var client = Api.Instance.GetClient("client_test");
            var got = await client.GetBooleanDetailsAsync("disabled_bool", false, DefaultEvaluationContext);
            var want = new FlagEvaluationDetails<bool>(
                "disabled_bool",
                false,
                ErrorType.None,
                Reason.Disabled,
                "SdkDefault",
                null,
                new ImmutableMetadata());
            Assert.Equivalent(want, got);
        }

        [Fact(DisplayName = "Should emit configuration change event, if config has changed")]
        public async Task ShouldEmitConfigurationChangeEventIfConfigHasChanged()
        {
            var mockHttp = new RelayProxyMock();
            var provider = new GoFeatureFlagProvider(
                new GoFeatureFlagProviderOptions
                {
                    HttpMessageHandler = mockHttp.GetRelayProxyMock("CHANGE_CONFIG"),
                    Endpoint = RelayProxyMock.baseUrl,
                    EvaluationType = EvaluationType.InProcess,
                    FlagChangePollingIntervalMs = TimeSpan.FromMilliseconds(50)
                }
            );


            await Api.Instance.SetProviderAsync("client_test_handler", provider);
            var client = Api.Instance.GetClient("client_test_handler");
            var handlerCalled = false;
            client.AddHandler(ProviderEventTypes.ProviderConfigurationChanged, _ =>
            {
                handlerCalled = true;
            });
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            Assert.True(handlerCalled);
        }

        [Fact(DisplayName = "Should not emit configuration change event, if config has not changed")]
        public async Task ShouldNotEmitConfigurationChangeEventIfConfigHasNotChanged()
        {
            var mockHttp = new RelayProxyMock();
            var provider = new GoFeatureFlagProvider(
                new GoFeatureFlagProviderOptions
                {
                    HttpMessageHandler = mockHttp.GetRelayProxyMock("SIMPLE_FLAG_CONFIG"),
                    Endpoint = RelayProxyMock.baseUrl,
                    EvaluationType = EvaluationType.InProcess,
                    FlagChangePollingIntervalMs = TimeSpan.FromMilliseconds(50)
                }
            );

            await Api.Instance.SetProviderAsync("client_test_handler_no_change", provider);
            var client = Api.Instance.GetClient("client_test_handler_no_change");
            var handlerCalled = false;
            client.AddHandler(ProviderEventTypes.ProviderConfigurationChanged, _ =>
            {
                handlerCalled = true;
            });
            await Task.Delay(TimeSpan.FromMilliseconds(150));
            Assert.False(handlerCalled);
        }

        [Fact(DisplayName = "Should change evaluation details if config has changed")]
        public async Task ShouldChangeEvaluationDetailsIfConfigHasChanged()
        {
            var mockHttp = new RelayProxyMock();
            var provider = new GoFeatureFlagProvider(
                new GoFeatureFlagProviderOptions
                {
                    HttpMessageHandler = mockHttp.GetRelayProxyMock("CHANGE_CONFIG"),
                    Endpoint = RelayProxyMock.baseUrl,
                    EvaluationType = EvaluationType.InProcess,
                    FlagChangePollingIntervalMs = TimeSpan.FromMilliseconds(500)
                }
            );
            await Api.Instance.SetProviderAsync("client_test_handler_2", provider);
            var client = Api.Instance.GetClient("client_test_handler_2");
            var handlerCalled = false;
            client.AddHandler(ProviderEventTypes.ProviderConfigurationChanged, _ =>
            {
                handlerCalled = true;
            });
            var v1 = await client.GetBooleanDetailsAsync("TEST", false, DefaultEvaluationContext);
            while (!handlerCalled)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }

            var v2 = await client.GetBooleanDetailsAsync("TEST", false, DefaultEvaluationContext);
            Assert.NotEqual(JsonSerializer.Serialize(v1), JsonSerializer.Serialize(v2));
        }

        [Fact(DisplayName = "Should error if flag configuration endpoint return a 404")]
        public async Task ShouldErrorIfFlagConfigurationEndpointReturnA404()
        {
            var mockHttp = new RelayProxyMock();
            var provider = new GoFeatureFlagProvider(
                new GoFeatureFlagProviderOptions
                {
                    HttpMessageHandler = mockHttp.GetRelayProxyMock("404"),
                    Endpoint = RelayProxyMock.baseUrl,
                    EvaluationType = EvaluationType.InProcess
                }
            );

            await Api.Instance.SetProviderAsync("client_test", provider);
            var client = Api.Instance.GetClient("client_test");
            var handlerCalled = false;
            client.AddHandler(ProviderEventTypes.ProviderError, _ =>
            {
                handlerCalled = true;
            });
            await Task.Delay(50);
            Assert.True(handlerCalled);
        }

        [Fact(DisplayName = "Should error if flag configuration endpoint return a 403")]
        public async Task ShouldErrorIfFlagConfigurationEndpointReturnA403()
        {
            var mockHttp = new RelayProxyMock();
            var provider = new GoFeatureFlagProvider(
                new GoFeatureFlagProviderOptions
                {
                    HttpMessageHandler = mockHttp.GetRelayProxyMock("403"),
                    Endpoint = RelayProxyMock.baseUrl,
                    EvaluationType = EvaluationType.InProcess
                }
            );

            await Api.Instance.SetProviderAsync("client_test", provider);
            var client = Api.Instance.GetClient("client_test");
            var handlerCalled = false;
            client.AddHandler(ProviderEventTypes.ProviderError, _ =>
            {
                handlerCalled = true;
            });
            await Task.Delay(50);
            Assert.True(handlerCalled);
        }

        [Fact(DisplayName = "Should error if flag configuration endpoint return a 401")]
        public async Task ShouldErrorIfFlagConfigurationEndpointReturnA401()
        {
            var mockHttp = new RelayProxyMock();
            var provider = new GoFeatureFlagProvider(
                new GoFeatureFlagProviderOptions
                {
                    HttpMessageHandler = mockHttp.GetRelayProxyMock("401"),
                    Endpoint = RelayProxyMock.baseUrl,
                    EvaluationType = EvaluationType.InProcess
                }
            );

            await Api.Instance.SetProviderAsync("client_test", provider);
            var client = Api.Instance.GetClient("client_test");
            var handlerCalled = false;
            client.AddHandler(ProviderEventTypes.ProviderError, _ =>
            {
                handlerCalled = true;
            });
            await Task.Delay(50);
            Assert.True(handlerCalled);
        }


        [Fact(DisplayName = "Should ignore configuration if etag is different by last-modified is older")]
        public async Task ShouldIgnoreConfigurationIfEtagIsDifferentByLastModifiedIsOlder()
        {
            var mockHttp = new RelayProxyMock();
            var provider = new GoFeatureFlagProvider(
                new GoFeatureFlagProviderOptions
                {
                    HttpMessageHandler = mockHttp.GetRelayProxyMock("CHANGE_CONFIG_LAST_MODIFIED_OLDER"),
                    Endpoint = RelayProxyMock.baseUrl,
                    EvaluationType = EvaluationType.InProcess,
                    FlagChangePollingIntervalMs = TimeSpan.FromMilliseconds(50)
                }
            );

            await Api.Instance.SetProviderAsync("client_test_handler_no_change_lastmodified", provider);
            var client = Api.Instance.GetClient("client_test_handler_no_change_lastmodified");
            var handlerCalled = false;
            client.AddHandler(ProviderEventTypes.ProviderConfigurationChanged, _ =>
            {
                handlerCalled = true;
            });
            await Task.Delay(TimeSpan.FromMilliseconds(150));
            Assert.False(handlerCalled);
        }

        [Fact(DisplayName = "Should apply a scheduled rollout step")]
        public async Task ShouldApplyAScheduledRolloutStep()
        {
            var mockHttp = new RelayProxyMock();
            var provider = new GoFeatureFlagProvider(
                new GoFeatureFlagProviderOptions
                {
                    HttpMessageHandler = mockHttp.GetRelayProxyMock("SCHEDULED_ROLLOUT_FLAG_CONFIG"),
                    Endpoint = RelayProxyMock.baseUrl,
                    EvaluationType = EvaluationType.InProcess
                }
            );

            await Api.Instance.SetProviderAsync("client_test", provider);
            var client = Api.Instance.GetClient("client_test");
            var got = await client.GetBooleanDetailsAsync("my-flag", false, DefaultEvaluationContext);
            var want = new FlagEvaluationDetails<bool>(
                "my-flag",
                true,
                ErrorType.None,
                Reason.TargetingMatch,
                "enabled",
                null,
                new Dictionary<string, object> { { "description", "this is a test flag" }, { "defaultValue", false } }
                    .ToImmutableMetadata());
            Assert.Equivalent(want, got);
        }

        [Fact(DisplayName = "Should not apply a scheduled rollout step if the date is in the future")]
        public async Task ShouldNotApplyAScheduledRolloutStepIfTheDateIsInTheFuture()
        {
            var mockHttp = new RelayProxyMock();
            var provider = new GoFeatureFlagProvider(
                new GoFeatureFlagProviderOptions
                {
                    HttpMessageHandler = mockHttp.GetRelayProxyMock("SCHEDULED_ROLLOUT_FLAG_CONFIG"),
                    Endpoint = RelayProxyMock.baseUrl,
                    EvaluationType = EvaluationType.InProcess
                }
            );

            await Api.Instance.SetProviderAsync("client_test", provider);
            var client = Api.Instance.GetClient("client_test");
            var got = await client.GetBooleanDetailsAsync("my-flag-scheduled-in-future", false,
                DefaultEvaluationContext);
            var want = new FlagEvaluationDetails<bool>(
                "my-flag-scheduled-in-future",
                false,
                ErrorType.None,
                Reason.Static,
                "disabled",
                null,
                new Dictionary<string, object> { { "description", "this is a test flag" }, { "defaultValue", false } }
                    .ToImmutableMetadata());
            Assert.Equivalent(want, got);
        }
    }

    [Collection("Constructor")]
    public class ConstructorTest
    {
        [Fact]
        public void GetMetadata_ValidateName()
        {
            var provider = new GoFeatureFlagProvider(
                new GoFeatureFlagProviderOptions { Endpoint = "https://gofeatureflag.org" }
            );
            Assert.Equal("GO Feature Flag Provider", provider.GetMetadata()?.Name);
        }

        [Fact]
        public void Constructor_Options_Null()
        {
            Assert.Throws<InvalidOption>(() => new GoFeatureFlagProvider(null));
        }

        [Fact]
        public void Constructor_Options_Empty()
        {
            Assert.Throws<InvalidOption>(() =>
                new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions())
            );
        }

        [Fact]
        public void Constructor_Options_EmptyEndpoint()
        {
            Assert.Throws<InvalidOption>(() =>
                new GoFeatureFlagProvider(
                    new GoFeatureFlagProviderOptions { Endpoint = "" }
                )
            );
        }

        [Fact]
        public void Constructor_Options_OnlyTimeout()
        {
            Assert.Throws<InvalidOption>(() =>
                new GoFeatureFlagProvider(
                    new GoFeatureFlagProviderOptions { Timeout = TimeSpan.FromMilliseconds(1000) }
                )
            );
        }

        [Fact]
        public void Constructor_Options_ValidEndpoint()
        {
            var exception = Record.Exception(() =>
                new GoFeatureFlagProvider(
                    new GoFeatureFlagProviderOptions { Endpoint = "https://gofeatureflag.org" }
                )
            );
            Assert.Null(exception);
        }

        [Fact(DisplayName = "Should error if invalid FlagChangePollingIntervalMs set")]
        public void ShouldErrorIfInvalidFlushIntervalIsSet()
        {
            var baseUrl = "http://localhost:1031";
            Assert.Throws<InvalidOption>(() =>
                new GoFeatureFlagProvider(
                    new GoFeatureFlagProviderOptions
                    {
                        Endpoint = baseUrl, FlagChangePollingIntervalMs = TimeSpan.FromMilliseconds(-1000)
                    }
                )
            );
        }

        [Fact(DisplayName = "Should error if invalid max pending events is set")]
        public void ShouldErrorIfInvalidMaxPendingEventsIsSet()
        {
            var baseUrl = "http://localhost:1031";
            Assert.Throws<InvalidOption>(() =>
                new GoFeatureFlagProvider(
                    new GoFeatureFlagProviderOptions { Endpoint = baseUrl, MaxPendingEvents = -1 }
                )
            );
        }
    }

    [Collection("Tracking")]
    public class TrackingTest
    {
        [Fact(DisplayName = "Should commit events if max pending events is reached")]
        public async Task ShouldCallMultipleTimeTheDataCollectorIfMaxPendingEventsIsReached()
        {
            var mockHttp = new RelayProxyMock();
            var provider = new GoFeatureFlagProvider(
                new GoFeatureFlagProviderOptions
                {
                    HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                    Endpoint = RelayProxyMock.baseUrl,
                    EvaluationType = EvaluationType.InProcess,
                    FlushIntervalMs = TimeSpan.FromMilliseconds(10000),
                    MaxPendingEvents = 1
                }
            );

            await Api.Instance.SetProviderAsync("client_test", provider);
            var client = Api.Instance.GetClient("client_test");

            client.Track("my-key", DefaultEvaluationContext,
                TrackingEventDetails.Builder().Set("revenue", 123).Set("user_id", "123ABC").Build());
            await Task.Delay(TimeSpan.FromMilliseconds(200));
            var got = await mockHttp.LastRequest.Content.ReadAsStringAsync();
            var want =
                "{ \"meta\": {},\"events\": [  {    \"kind\": \"tracking\",    \"evaluationContext\": {      \"email\": \"john.doe@gofeatureflag.org\",      \"labels\": [        \"pro\",        \"beta\"      ],      \"rate\": 3.14,      \"company_info\": {        \"size\": 120,        \"name\": \"my_company\"      },      \"anonymous\": false,      \"targetingKey\": \"d45e303a-38c2-11ed-a261-0242ac120002\",      \"professional\": true,      \"firstname\": \"john\",      \"lastname\": \"doe\",      \"age\": 30    },    \"trackingEventDetails\": {      \"revenue\": 123,      \"user_id\": \"123ABC\"    },    \"creationDate\": 1750679098,    \"contextKind\": \"user\",    \"key\": \"my-key\",    \"userKey\": \"d45e303a-38c2-11ed-a261-0242ac120002\"  }]\n}";
            var gotJson = JObject.Parse(got);
            Assert.NotNull(gotJson["events"].First);
        }

        [Fact(DisplayName = "Should send the evaluation information to the data collector")]
        public async Task ShouldSendTrackingEventToTheDataCollector()
        {
            var mockHttp = new RelayProxyMock();
            var provider = new GoFeatureFlagProvider(
                new GoFeatureFlagProviderOptions
                {
                    HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                    Endpoint = RelayProxyMock.baseUrl,
                    EvaluationType = EvaluationType.InProcess,
                    FlushIntervalMs = TimeSpan.FromMilliseconds(100)
                }
            );

            await Api.Instance.SetProviderAsync("client_test", provider);
            var client = Api.Instance.GetClient("client_test");

            client.Track("my-key", DefaultEvaluationContext,
                TrackingEventDetails.Builder().Set("revenue", 123).Set("user_id", "123ABC").Build());
            await Task.Delay(TimeSpan.FromMilliseconds(200));
            var got = await mockHttp.LastRequest.Content.ReadAsStringAsync();
            var gotJson = JObject.Parse(got);
            Assert.Equal(1, gotJson["events"].Count());
        }
    }

    [Collection("DataCollectorHook")]
    public class DataCollectorHook
    {
        [Fact(DisplayName = "Should commit events if max pending events is reached")]
        public async Task ShouldCommitEventsIfMaxPendingEventsIsReached()
        {
            var mockHttp = new RelayProxyMock();
            var provider = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
            {
                Endpoint = RelayProxyMock.baseUrl,
                HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                EvaluationType = EvaluationType.InProcess,
                MaxPendingEvents = 1
            });

            await Api.Instance.SetProviderAsync("test-client", provider);
            var client = Api.Instance.GetClient("test-client");
            await client.GetBooleanDetailsAsync("bool_flag", false, DefaultEvaluationContext);
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            var got = await mockHttp.LastRequest.Content.ReadAsStringAsync();
            var gotJson = JObject.Parse(got);
            Assert.Equal(1, gotJson["events"].Count());
        }

        [Fact(DisplayName = "Should call multiple times the data collector if max pending events is reached")]
        public async Task ShouldCallMultipleTimeTheDataCollectorIfMaxPendingEventsIsReached()
        {
            var mockHttp = new RelayProxyMock();
            var provider = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
            {
                Endpoint = RelayProxyMock.baseUrl,
                HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                EvaluationType = EvaluationType.InProcess,
                MaxPendingEvents = 2
            });

            await Api.Instance.SetProviderAsync("test-client", provider);
            var client = Api.Instance.GetClient("test-client");
            await client.GetBooleanDetailsAsync("bool_flag", false, DefaultEvaluationContext);
            await client.GetBooleanDetailsAsync("bool_flag", false, DefaultEvaluationContext);
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            var got = await mockHttp.LastRequest.Content.ReadAsStringAsync();
            var gotJson = JObject.Parse(got);
            Assert.Equal(2, gotJson["events"].Count());
        }
    }

    [Collection("EnrichEvaluationContextHook")]
    public class EnrichEvaluationContextHook
    {
        [Fact(DisplayName = "Should add gofeatureflag key in evaluation context if metadata is set")]
        public async Task ShouldAddGofeatureflagKeyInEvaluationContextIfMetadataIsSet()
        {
            var meta = new ExporterMetadata();
            meta.Add("test", "this is a test value");
            meta.Add("test2", 42);
            var ofrepProvider = new OfrepProviderMock();

            var mockHttp = new RelayProxyMock();
            var provider = new GoFeatureFlagProvider(
                new GoFeatureFlagProviderOptions
                {
                    HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                    Endpoint = RelayProxyMock.baseUrl,
                    EvaluationType = EvaluationType.Remote,
                    ExporterMetadata = meta
                },
                ofrepProvider
            );

            await Api.Instance.SetProviderAsync("test-client", provider);
            var client = Api.Instance.GetClient("test-client");
            var got = await client.GetStringDetailsAsync("flag", "default", DefaultEvaluationContext);
            var lastEvaluationContext = ofrepProvider.LastEvaluationContext;
            lastEvaluationContext.TryGetValue("gofeatureflag", out var goFeatureFlagValue);
            Assert.NotNull(goFeatureFlagValue);
        }

        [Fact(DisplayName = "Should not add gofeatureflag key in evaluation context if metadata is set")]
        public async Task ShouldNotAddGofeatureflagKeyInEvaluationContextIfMetadataIsSet()
        {
            var ofrepProvider = new OfrepProviderMock();
            var mockHttp = new RelayProxyMock();
            var provider = new GoFeatureFlagProvider(
                new GoFeatureFlagProviderOptions
                {
                    HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                    Endpoint = RelayProxyMock.baseUrl,
                    EvaluationType = EvaluationType.Remote
                },
                ofrepProvider
            );

            await Api.Instance.SetProviderAsync("test-client", provider);
            var client = Api.Instance.GetClient("test-client");
            var got = await client.GetStringDetailsAsync("flag", "default", DefaultEvaluationContext);
            var lastEvaluationContext = ofrepProvider.LastEvaluationContext;
            lastEvaluationContext.TryGetValue("gofeatureflag", out var goFeatureFlagValue);
            Assert.Null(goFeatureFlagValue);
        }
    }

    [Collection("Remote Evaluation")]
    public class RemoteEvaluationTest
    {
        [Fact(DisplayName = "Should error if the endpoint is not available")]
        public async Task ShouldErrorIfEndpointNotAvailable()
        {
            var mockHttp = new RelayProxyMock();
            Assert.Throws<InvalidOption>(() =>
            {
                new GoFeatureFlagProvider(
                    new GoFeatureFlagProviderOptions
                    {
                        HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                        Endpoint = "",
                        EvaluationType = EvaluationType.Remote
                    }
                );
            });
        }

        [Fact(DisplayName = "Should evaluate a string flag with remote evaluation")]
        public async Task ShouldEvaluateAStringFlagWithRemoteEvaluation()
        {
            var mockHttp = new RelayProxyMock();
            var provider = new GoFeatureFlagProvider(
                new GoFeatureFlagProviderOptions
                {
                    HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                    Endpoint = RelayProxyMock.baseUrl,
                    EvaluationType = EvaluationType.Remote
                },
                new OfrepProviderMock()
            );

            await Api.Instance.SetProviderAsync("test-client", provider);
            var client = Api.Instance.GetClient("test-client");
            var got = await client.GetStringDetailsAsync("flag", "default", DefaultEvaluationContext);
            var want = new ResolutionDetails<string>(
                "flag",
                "this is a test value",
                ErrorType.None,
                Reason.TargetingMatch,
                "enabled",
                null,
                new ImmutableMetadata(new Dictionary<string, object>
                {
                    { "test", new Value("this is a test value") },
                    { "test2", new Value(42) },
                    { "test3", new Value(true) },
                    { "test4", new Value(3.14) }
                })
            );

            Assert.Equivalent(want, got);
        }

        [Fact(DisplayName = "Should evaluate a bool flag with remote evaluation")]
        public async Task ShouldEvaluateABoolFlagWithRemoteEvaluation()
        {
            var mockHttp = new RelayProxyMock();
            var provider = new GoFeatureFlagProvider(
                new GoFeatureFlagProviderOptions
                {
                    HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                    Endpoint = RelayProxyMock.baseUrl,
                    EvaluationType = EvaluationType.Remote
                },
                new OfrepProviderMock()
            );

            await Api.Instance.SetProviderAsync("test-client", provider);
            var client = Api.Instance.GetClient("test-client");
            var got = await client.GetBooleanDetailsAsync("flag", false, DefaultEvaluationContext);
            var want = new ResolutionDetails<bool>(
                "flag",
                true,
                ErrorType.None,
                Reason.TargetingMatch,
                "enabled",
                null,
                new ImmutableMetadata(new Dictionary<string, object>
                {
                    { "test", new Value("this is a test value") },
                    { "test2", new Value(42) },
                    { "test3", new Value(true) },
                    { "test4", new Value(3.14) }
                })
            );

            Assert.Equivalent(want, got);
        }

        [Fact(DisplayName = "Should evaluate a double flag with remote evaluation")]
        public async Task ShouldEvaluateADoubleFlagWithRemoteEvaluation()
        {
            var mockHttp = new RelayProxyMock();
            var provider = new GoFeatureFlagProvider(
                new GoFeatureFlagProviderOptions
                {
                    HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                    Endpoint = RelayProxyMock.baseUrl,
                    EvaluationType = EvaluationType.Remote
                },
                new OfrepProviderMock()
            );

            await Api.Instance.SetProviderAsync("test-client", provider);
            var client = Api.Instance.GetClient("test-client");
            var got = await client.GetDoubleDetailsAsync("flag", 1.2, DefaultEvaluationContext);
            var want = new ResolutionDetails<double>(
                "flag",
                12.21,
                ErrorType.None,
                Reason.TargetingMatch,
                "enabled",
                null,
                new ImmutableMetadata(new Dictionary<string, object>
                {
                    { "test", new Value("this is a test value") },
                    { "test2", new Value(42) },
                    { "test3", new Value(true) },
                    { "test4", new Value(3.14) }
                })
            );

            Assert.Equivalent(want, got);
        }

        [Fact(DisplayName = "Should evaluate an int flag with remote evaluation")]
        public async Task ShouldEvaluateAIntFlagWithRemoteEvaluation()
        {
            var mockHttp = new RelayProxyMock();
            var provider = new GoFeatureFlagProvider(
                new GoFeatureFlagProviderOptions
                {
                    HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                    Endpoint = RelayProxyMock.baseUrl,
                    EvaluationType = EvaluationType.Remote
                },
                new OfrepProviderMock()
            );

            await Api.Instance.SetProviderAsync("test-client", provider);
            var client = Api.Instance.GetClient("test-client");
            var got = await client.GetIntegerDetailsAsync("flag", 1, DefaultEvaluationContext);
            var want = new ResolutionDetails<int>(
                "flag",
                12,
                ErrorType.None,
                Reason.TargetingMatch,
                "enabled",
                null,
                new ImmutableMetadata(new Dictionary<string, object>
                {
                    { "test", new Value("this is a test value") },
                    { "test2", new Value(42) },
                    { "test3", new Value(true) },
                    { "test4", new Value(3.14) }
                })
            );

            Assert.Equivalent(want, got);
        }

        [Fact(DisplayName = "Should evaluate a value flag with remote evaluation")]
        public async Task ShouldEvaluateAValueFlagWithRemoteEvaluation()
        {
            var mockHttp = new RelayProxyMock();
            var provider = new GoFeatureFlagProvider(
                new GoFeatureFlagProviderOptions
                {
                    HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
                    Endpoint = RelayProxyMock.baseUrl,
                    EvaluationType = EvaluationType.Remote
                },
                new OfrepProviderMock()
            );

            await Api.Instance.SetProviderAsync("test-client", provider);
            var client = Api.Instance.GetClient("test-client");
            var got = await client.GetObjectDetailsAsync("flag", new Value(1), DefaultEvaluationContext);
            var want = new ResolutionDetails<Value>(
                "flag",
                new Value("this is a test value"),
                ErrorType.None,
                Reason.TargetingMatch,
                "enabled",
                null,
                new ImmutableMetadata(new Dictionary<string, object>
                {
                    { "test", new Value("this is a test value") },
                    { "test2", new Value(42) },
                    { "test3", new Value(true) },
                    { "test4", new Value(3.14) }
                })
            );

            Assert.Equivalent(want, got);
        }
        // @DisplayName("Should error if the endpoint is not available")
        // @SneakyThrows
        // @Test
        // void shouldErrorIfEndpointNotAvailable() {
        //     try (val s = new MockWebServer()) {
        //         val goffAPIMock = new GoffApiMock(GoffApiMock.MockMode.ENDPOINT_ERROR);
        //         s.setDispatcher(goffAPIMock.dispatcher);
        //         GoFeatureFlagProvider provider = new GoFeatureFlagProvider(GoFeatureFlagProviderOptions.builder()
        //                 .endpoint(s.url("").toString())
        //                 .evaluationType(EvaluationType.REMOTE)
        //                 .timeout(1000)
        //                 .build());
        //         OpenFeatureAPI.getInstance().setProviderAndWait(testName, provider);
        //         val client = OpenFeatureAPI.getInstance().getClient(testName);
        //         val got = client.getBooleanDetails("bool_flag", false, TestUtils.defaultEvaluationContext);
        //         val want = FlagEvaluationDetails.<Boolean>builder()
        //                 .value(false)
        //                 .flagKey("bool_flag")
        //                 .reason(Reason.ERROR.name())
        //                 .errorCode(ErrorCode.GENERAL)
        //                 .errorMessage("Unknown error while retrieving flag ")
        //                 .build();
        //         assertEquals(want, got);
        //     }
        // }
        //
        // @DisplayName("Should error if no API Key provided")
        // @SneakyThrows
        // @Test
        // void shouldErrorIfApiKeyIsMissing() {
        //     try (val s = new MockWebServer()) {
        //         val goffAPIMock = new GoffApiMock(GoffApiMock.MockMode.API_KEY_MISSING);
        //         s.setDispatcher(goffAPIMock.dispatcher);
        //         GoFeatureFlagProvider provider = new GoFeatureFlagProvider(GoFeatureFlagProviderOptions.builder()
        //                 .endpoint(s.url("").toString())
        //                 .evaluationType(EvaluationType.REMOTE)
        //                 .timeout(1000)
        //                 .build());
        //         OpenFeatureAPI.getInstance().setProviderAndWait(testName, provider);
        //         val client = OpenFeatureAPI.getInstance().getClient(testName);
        //         val got = client.getBooleanDetails("bool_flag", false, TestUtils.defaultEvaluationContext);
        //         val want = FlagEvaluationDetails.<Boolean>builder()
        //                 .value(false)
        //                 .flagKey("bool_flag")
        //                 .reason(Reason.ERROR.name())
        //                 .errorCode(ErrorCode.GENERAL)
        //                 .errorMessage("authentication/authorization error")
        //                 .build();
        //         assertEquals(want, got);
        //     }
        // }
        //
        // @DisplayName("Should error if API Key is invalid")
        // @SneakyThrows
        // @Test
        // void shouldErrorIfApiKeyIsInvalid() {
        //     try (val s = new MockWebServer()) {
        //         val goffAPIMock = new GoffApiMock(GoffApiMock.MockMode.INVALID_API_KEY);
        //         s.setDispatcher(goffAPIMock.dispatcher);
        //         GoFeatureFlagProvider provider = new GoFeatureFlagProvider(GoFeatureFlagProviderOptions.builder()
        //                 .endpoint(s.url("").toString())
        //                 .evaluationType(EvaluationType.REMOTE)
        //                 .apiKey("invalid")
        //                 .timeout(1000)
        //                 .build());
        //         OpenFeatureAPI.getInstance().setProviderAndWait(testName, provider);
        //         val client = OpenFeatureAPI.getInstance().getClient(testName);
        //         val got = client.getBooleanDetails("bool_flag", false, TestUtils.defaultEvaluationContext);
        //         val want = FlagEvaluationDetails.<Boolean>builder()
        //                 .value(false)
        //                 .flagKey("bool_flag")
        //                 .reason(Reason.ERROR.name())
        //                 .errorCode(ErrorCode.GENERAL)
        //                 .errorMessage("authentication/authorization error")
        //                 .build();
        //         assertEquals(want, got);
        //     }
        // }
        //
        // @DisplayName("Should error if the flag is not found")
        // @SneakyThrows
        // @Test
        // void shouldErrorIfFlagNotFound() {
        //     GoFeatureFlagProvider provider = new GoFeatureFlagProvider(GoFeatureFlagProviderOptions.builder()
        //             .endpoint(baseUrl.toString())
        //             .evaluationType(EvaluationType.REMOTE)
        //             .build());
        //     OpenFeatureAPI.getInstance().setProviderAndWait(testName, provider);
        //     val client = OpenFeatureAPI.getInstance().getClient(testName);
        //     val got = client.getBooleanDetails("does-not-exists", false, TestUtils.defaultEvaluationContext);
        //     val want = FlagEvaluationDetails.<Boolean>builder()
        //             .value(false)
        //             .flagKey("does-not-exists")
        //             .reason(Reason.ERROR.name())
        //             .errorCode(ErrorCode.FLAG_NOT_FOUND)
        //             .errorMessage("Flag does-not-exists not found")
        //             .build();
        //     assertEquals(want, got);
        // }
        //
        // @DisplayName("Should error if evaluating the wrong type")
        // @SneakyThrows
        // @Test
        // void shouldErrorIfEvaluatingTheWrongType() {
        //     GoFeatureFlagProvider provider = new GoFeatureFlagProvider(GoFeatureFlagProviderOptions.builder()
        //             .endpoint(baseUrl.toString())
        //             .evaluationType(EvaluationType.REMOTE)
        //             .build());
        //     OpenFeatureAPI.getInstance().setProviderAndWait(testName, provider);
        //     val client = OpenFeatureAPI.getInstance().getClient(testName);
        //     val got = client.getStringDetails("bool_flag", "default", TestUtils.defaultEvaluationContext);
        //     val want = FlagEvaluationDetails.<String>builder()
        //             .value("default")
        //             .flagKey("bool_flag")
        //             .reason(Reason.ERROR.name())
        //             .errorMessage(
        //                     "Flag value bool_flag had unexpected type class java.lang.Boolean, expected class java.lang.String.")
        //             .errorCode(ErrorCode.TYPE_MISMATCH)
        //             .build();
        //     assertEquals(want, got);
        // }
        //
        // @DisplayName("Should resolve a valid boolean flag")
        // @SneakyThrows
        // @Test
        // void shouldResolveAValidBooleanFlag() {
        //     GoFeatureFlagProvider provider = new GoFeatureFlagProvider(GoFeatureFlagProviderOptions.builder()
        //             .endpoint(baseUrl.toString())
        //             .evaluationType(EvaluationType.REMOTE)
        //             .build());
        //     OpenFeatureAPI.getInstance().setProviderAndWait(testName, provider);
        //     val client = OpenFeatureAPI.getInstance().getClient(testName);
        //     val got = client.getBooleanDetails("bool_flag", false, TestUtils.defaultEvaluationContext);
        //     val want = FlagEvaluationDetails.<Boolean>builder()
        //             .value(true)
        //             .variant("enabled")
        //             .flagKey("bool_flag")
        //             .reason(Reason.TARGETING_MATCH.name())
        //             .flagMetadata(ImmutableMetadata.builder()
        //                     .addString("description", "A flag that is always off")
        //                     .build())
        //             .build();
        //     assertEquals(want, got);
        // }
        //
        // @DisplayName("Should resolve a valid string flag")
        // @SneakyThrows
        // @Test
        // void shouldResolveAValidStringFlag() {
        //     GoFeatureFlagProvider provider = new GoFeatureFlagProvider(GoFeatureFlagProviderOptions.builder()
        //             .endpoint(baseUrl.toString())
        //             .evaluationType(EvaluationType.REMOTE)
        //             .build());
        //     OpenFeatureAPI.getInstance().setProviderAndWait(testName, provider);
        //     val client = OpenFeatureAPI.getInstance().getClient(testName);
        //     val got = client.getStringDetails("string_flag", "false", TestUtils.defaultEvaluationContext);
        //     val want = FlagEvaluationDetails.<String>builder()
        //             .value("string value")
        //             .variant("variantA")
        //             .flagKey("string_flag")
        //             .reason(Reason.TARGETING_MATCH.name())
        //             .flagMetadata(ImmutableMetadata.builder()
        //                     .addString("description", "A flag that is always off")
        //                     .build())
        //             .build();
        //     assertEquals(want, got);
        // }
        //
        // @DisplayName("Should resolve a valid int flag")
        // @SneakyThrows
        // @Test
        // void shouldResolveAValidIntFlag() {
        //     GoFeatureFlagProvider provider = new GoFeatureFlagProvider(GoFeatureFlagProviderOptions.builder()
        //             .endpoint(baseUrl.toString())
        //             .evaluationType(EvaluationType.REMOTE)
        //             .build());
        //     OpenFeatureAPI.getInstance().setProviderAndWait(testName, provider);
        //     val client = OpenFeatureAPI.getInstance().getClient(testName);
        //     val got = client.getIntegerDetails("int_flag", 0, TestUtils.defaultEvaluationContext);
        //     val want = FlagEvaluationDetails.<Integer>builder()
        //             .value(100)
        //             .variant("variantA")
        //             .flagKey("int_flag")
        //             .reason(Reason.TARGETING_MATCH.name())
        //             .flagMetadata(ImmutableMetadata.builder()
        //                     .addString("description", "A flag that is always off")
        //                     .build())
        //             .build();
        //     assertEquals(want, got);
        // }
        //
        // @DisplayName("Should resolve a valid double flag")
        // @SneakyThrows
        // @Test
        // void shouldResolveAValidDoubleFlag() {
        //     GoFeatureFlagProvider provider = new GoFeatureFlagProvider(GoFeatureFlagProviderOptions.builder()
        //             .endpoint(baseUrl.toString())
        //             .evaluationType(EvaluationType.REMOTE)
        //             .build());
        //     OpenFeatureAPI.getInstance().setProviderAndWait(testName, provider);
        //     val client = OpenFeatureAPI.getInstance().getClient(testName);
        //     val got = client.getDoubleDetails("double_flag", 0.0, TestUtils.defaultEvaluationContext);
        //     val want = FlagEvaluationDetails.<Double>builder()
        //             .value(100.11)
        //             .variant("variantA")
        //             .flagKey("double_flag")
        //             .reason(Reason.TARGETING_MATCH.name())
        //             .flagMetadata(ImmutableMetadata.builder()
        //                     .addString("description", "A flag that is always off")
        //                     .build())
        //             .build();
        //     assertEquals(want, got);
        // }
        //
        // @DisplayName("Should resolve a valid object flag")
        // @SneakyThrows
        // @Test
        // void shouldResolveAValidObjectFlag() {
        //     GoFeatureFlagProvider provider = new GoFeatureFlagProvider(GoFeatureFlagProviderOptions.builder()
        //             .endpoint(baseUrl.toString())
        //             .evaluationType(EvaluationType.REMOTE)
        //             .build());
        //     OpenFeatureAPI.getInstance().setProviderAndWait(testName, provider);
        //     val client = OpenFeatureAPI.getInstance().getClient(testName);
        //     val got = client.getObjectDetails("object_flag", new Value("default"), TestUtils.defaultEvaluationContext);
        //
        //     val want = FlagEvaluationDetails.<Value>builder()
        //             .value(new Value(new MutableStructure().add("name", "foo").add("age", 100)))
        //             .variant("variantA")
        //             .flagKey("object_flag")
        //             .reason(Reason.TARGETING_MATCH.name())
        //             .flagMetadata(ImmutableMetadata.builder()
        //                     .addString("description", "A flag that is always off")
        //                     .build())
        //             .build();
        //     assertEquals(want, got);
        // }
    }
}
