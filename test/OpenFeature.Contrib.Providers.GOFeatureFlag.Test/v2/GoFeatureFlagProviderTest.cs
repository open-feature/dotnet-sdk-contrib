using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OpenFeature.Constant;
using OpenFeature.Contrib.Providers.GOFeatureFlag.Test.mock;
using OpenFeature.Contrib.Providers.GOFeatureFlag.v2;
using OpenFeature.Contrib.Providers.GOFeatureFlag.v2.extensions;
using OpenFeature.Contrib.Providers.GOFeatureFlag.v2.model;
using OpenFeature.Model;
using Xunit;
using InvalidOption = OpenFeature.Contrib.Providers.GOFeatureFlag.v2.exception.InvalidOption;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.Test.v2;

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

    // [Collection("EnrichEvaluationContextHook")]
    // public class EnrichEvaluationContextHook
    // {
    //     [Fact(DisplayName = "Should not add gofeatureflag key in exporterMetadata if the exporterMetadata is empty")]
    //     public async Task ShouldNotAddGofeatureflagKeyInExporterMetadataIfTheExporterMetadataIsEmpty()
    //     {
    //         var mockHttp = new RelayProxyMock();
    //         var provider = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
    //         {
    //             Endpoint = RelayProxyMock.baseUrl,
    //             HttpMessageHandler = mockHttp.GetRelayProxyMock(""),
    //             EvaluationType = EvaluationType.Remote
    //         });
    //
    //         await Api.Instance.SetProviderAsync("test-client", provider);
    //         var client = Api.Instance.GetClient("test-client");
    //         await client.GetBooleanDetailsAsync("bool_flag", false, DefaultEvaluationContext);
    //
    //         var body = await mockHttp.LastRequest.Content.ReadAsStringAsync();
    //
    //         var want = @"{
    //         ""context"": {
    //             ""targetingKey"": ""d45e303a-38c2-11ed-a261-0242ac120002"",
    //             ""rate"": 3.14,
    //             ""company_info"": { ""size"": 120, ""name"": ""my_company"" },
    //             ""anonymous"": false,
    //             ""email"": ""john.doe@gofeatureflag.org"",
    //             ""lastname"": ""doe"",
    //             ""firstname"": ""john"",
    //             ""age"": 30,
    //             ""professional"": true,
    //             ""labels"": [""pro"", ""beta""]
    //         }
    //     }";
    //
    //         AssertUtil.JsonEqual(want, body);
    //     }
    // }
}
