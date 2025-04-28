using Newtonsoft.Json.Linq;
using OpenFeature.Constant;
using OpenFeature.Contrib.Providers.GOFeatureFlag.exception;
using OpenFeature.Contrib.Providers.GOFeatureFlag.models;
using OpenFeature.Model;
using RichardSzalay.MockHttp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.Test;

public class GoFeatureFlagProviderTest
{
    private static readonly string baseUrl = "http://gofeatureflag.org";
    private static readonly string prefixEval = baseUrl + "/ofrep/v1/evaluate/flags/";
    private readonly EvaluationContext _defaultEvaluationCtx = InitDefaultEvaluationCtx();
    private readonly HttpMessageHandler _mockHttp = InitMock();

    private static HttpMessageHandler InitMock()
    {
        const string mediaType = "application/json";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"{prefixEval}fail_500").Respond(HttpStatusCode.InternalServerError);
        mockHttp.When($"{prefixEval}api_key_missing").Respond(HttpStatusCode.BadRequest);
        mockHttp.When($"{prefixEval}invalid_api_key").Respond(HttpStatusCode.Unauthorized);
        mockHttp.When($"{prefixEval}flag_not_found").Respond(HttpStatusCode.NotFound);
        mockHttp.When($"{prefixEval}bool_targeting_match").Respond(mediaType,
            "{ \"value\":true, \"key\":\"bool_targeting_match\", \"reason\":\"TARGETING_MATCH\", \"variant\":\"True\", \"cacheable\":true }");
        mockHttp.When($"{prefixEval}disabled").Respond(mediaType,
            "{ \"value\":false, \"key\":\"disabled\", \"reason\":\"DISABLED\", \"variant\":\"defaultSdk\", \"cacheable\":true}");
        mockHttp.When($"{prefixEval}disabled_double").Respond(mediaType,
            "{ \"value\":100.25, \"key\":\"disabled_double\", \"reason\":\"DISABLED\", \"variant\":\"defaultSdk\", \"cacheable\":true}");
        mockHttp.When($"{prefixEval}disabled_integer").Respond(mediaType,
            "{ \"value\":100, \"key\":\"disabled_integer\", \"reason\":\"DISABLED\", \"variant\":\"defaultSdk\", \"cacheable\":true}");
        mockHttp.When($"{prefixEval}disabled_object").Respond(mediaType,
            "{ \"value\":null, \"key\":\"disabled_object\", \"reason\":\"DISABLED\", \"variant\":\"defaultSdk\", \"cacheable\":true}");
        mockHttp.When($"{prefixEval}disabled_string").Respond(mediaType,
            "{ \"value\":\"\", \"key\":\"disabled_string\", \"reason\":\"DISABLED\", \"variant\":\"defaultSdk\", \"cacheable\":true}");
        mockHttp.When($"{prefixEval}double_key").Respond(mediaType,
            "{ \"value\":100.25, \"key\":\"double_key\", \"reason\":\"TARGETING_MATCH\", \"variant\":\"True\", \"cacheable\":true}");
        mockHttp.When($"{prefixEval}flag_not_found").Respond(mediaType,
            "{ \"value\":false, \"key\":\"flag_not_found\", \"reason\":\"FLAG_NOT_FOUND\", \"variant\":\"True\", \"cacheable\":true}");
        mockHttp.When($"{prefixEval}integer_key").Respond(mediaType,
            "{ \"value\":100, \"key\":\"integer_key\", \"reason\":\"TARGETING_MATCH\", \"variant\":\"True\", \"cacheable\":true}");
        mockHttp.When($"{prefixEval}list_key").Respond(mediaType,
            "{ \"value\":[\"test\",\"test1\",\"test2\",\"false\",\"test3\"], \"key\":\"list_key\", \"reason\":\"TARGETING_MATCH\", \"variant\":\"True\", \"cacheable\":true}");
        mockHttp.When($"{prefixEval}object_key").Respond(mediaType,
            "{ \"value\":{\"test\":\"test1\",\"test2\":false,\"test3\":123.3,\"test4\":1,\"test5\":null}, \"key\":\"object_key\", \"reason\":\"TARGETING_MATCH\", \"variant\":\"True\", \"cacheable\":true}");
        mockHttp.When($"{prefixEval}string_key").Respond(mediaType,
            "{ \"value\":\"CC0000\", \"key\":\"string_key\", \"reason\":\"TARGETING_MATCH\", \"variant\":\"True\", \"cacheable\":true}");
        mockHttp.When($"{prefixEval}unknown_reason").Respond(mediaType,
            "{ \"value\":\"true\", \"key\":\"unknown_reason\", \"reason\":\"CUSTOM_REASON\", \"variant\":\"True\", \"cacheable\":true}");
        mockHttp.When($"{prefixEval}does_not_exists").Respond(mediaType,
            "{ \"value\":\"\", \"key\":\"does_not_exists\", \"errorCode\":\"FLAG_NOT_FOUND\", \"variant\":\"defaultSdk\", \"cacheable\":true, \"errorDetails\":\"flag does_not_exists was not found in your configuration\"}");
        mockHttp.When($"{prefixEval}integer_with_metadata").Respond(mediaType,
            "{ \"value\":100, \"key\":\"integer_key\", \"reason\":\"TARGETING_MATCH\", \"variant\":\"True\", \"cacheable\":true, \"metadata\":{\"key1\": \"key1\", \"key2\": 1, \"key3\": 1.345, \"key4\": true}}");
        return mockHttp;
    }

    private static EvaluationContext InitDefaultEvaluationCtx()
    {
        return EvaluationContext.Builder()
            .Set("targetingKey", "d45e303a-38c2-11ed-a261-0242ac120002")
            .Set("email", "john.doe@gofeatureflag.org")
            .Set("firstname", "john")
            .Set("lastname", "doe")
            .Set("anonymous", false)
            .Set("professional", true)
            .Set("rate", 3.14)
            .Set("age", 30)
            .Set("company_info", new Value(new Structure(new Dictionary<string, Value>
            {
                { "name", new Value("my_company") },
                { "size", new Value(120) }
            })))
            .Set("labels", new Value(new List<Value>
            {
                new("pro"),
                new("beta")
            }))
            .Build();
    }


    [Fact]
    public async Task getMetadata_validate_name()
    {
        var goFeatureFlagProvider = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Timeout = new TimeSpan(19 * TimeSpan.TicksPerHour),
            Endpoint = baseUrl
        });
        await Api.Instance.SetProviderAsync(goFeatureFlagProvider);
        Assert.Equal("GO Feature Flag Provider", Api.Instance.GetProvider().GetMetadata().Name);
    }


    [Fact]
    private void constructor_options_null()
    {
        Assert.Throws<InvalidOption>(() => new GoFeatureFlagProvider(null));
    }

    [Fact]
    private void constructor_options_empty()
    {
        Assert.Throws<InvalidOption>(() => new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions()));
    }

    [Fact]
    private void constructor_options_empty_endpoint()
    {
        Assert.Throws<InvalidOption>(
            () => new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions { Endpoint = "" }));
    }

    [Fact]
    private void constructor_options_only_timeout()
    {
        Assert.Throws<InvalidOption>(
            () => new GoFeatureFlagProvider(
                new GoFeatureFlagProviderOptions { Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond) }
            )
        );
    }

    [Fact]
    private void constructor_options_valid_endpoint()
    {
        var exception = Record.Exception(() =>
            new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions { Endpoint = baseUrl }));
        Assert.Null(exception);
    }

    [Fact]
    public async Task should_throw_an_error_if_endpoint_not_available()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        await Api.Instance.SetProviderAsync(g);
        var client = Api.Instance.GetClient("test-client");
        var result = await client.GetBooleanDetailsAsync("fail_500", false, _defaultEvaluationCtx);
        Assert.NotNull(result);
        Assert.False(result.Value);
        Assert.Equal(ErrorType.General, result.ErrorType);
        Assert.Equal(Reason.Error, result.Reason);
    }

    [Fact]
    public async Task should_have_bad_request_if_no_token()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        await Api.Instance.SetProviderAsync(g);
        var client = Api.Instance.GetClient("test-client");
        var result = await client.GetBooleanDetailsAsync("api_key_missing", false, _defaultEvaluationCtx);
        Assert.NotNull(result);
        Assert.False(result.Value);
        Assert.Equal(Reason.Error, result.Reason);
        Assert.Equal(ErrorType.General, result.ErrorType);
    }

    [Fact]
    public async Task should_have_unauthorized_if_invalid_token()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond),
            ApiKey = "ff877c7a-4594-43b5-89a8-df44c9984bd8"
        });
        await Api.Instance.SetProviderAsync(g);
        var client = Api.Instance.GetClient("test-client");
        var result = await client.GetBooleanDetailsAsync("invalid_api_key", false, _defaultEvaluationCtx);
        Assert.NotNull(result);
        Assert.False(result.Value);
        Assert.Equal(Reason.Error, result.Reason);
        Assert.Equal(ErrorType.General, result.ErrorType);
    }

    [Fact]
    public async Task should_throw_an_error_if_flag_does_not_exists()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        await Api.Instance.SetProviderAsync(g);
        var client = Api.Instance.GetClient("test-client");
        var result = await client.GetBooleanDetailsAsync("flag_not_found", false, _defaultEvaluationCtx);
        Assert.NotNull(result);
        Assert.False(result.Value);
        Assert.Equal(ErrorType.FlagNotFound, result.ErrorType);
        Assert.Equal(Reason.Error, result.Reason);
    }

    [Fact]
    public async Task should_throw_an_error_if_we_expect_a_boolean_and_got_another_type()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        await Api.Instance.SetProviderAsync(g);
        var client = Api.Instance.GetClient("test-client");
        var result = await client.GetBooleanDetailsAsync("string_key", false, _defaultEvaluationCtx);
        Assert.NotNull(result);
        Assert.False(result.Value);
        Assert.Equal(ErrorType.TypeMismatch, result.ErrorType);
        Assert.Equal(Reason.Error, result.Reason);
    }

    [Fact]
    public async Task should_resolve_a_valid_boolean_flag_with_TARGETING_MATCH_reason()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        await Api.Instance.SetProviderAsync(g);
        var client = Api.Instance.GetClient("test-client");
        var result = await client.GetBooleanDetailsAsync("bool_targeting_match", false, _defaultEvaluationCtx);
        Assert.NotNull(result);
        Assert.True(result.Value);
        Assert.Equal(ErrorType.None, result.ErrorType);
        Assert.Equal(Reason.TargetingMatch, result.Reason);
        Assert.Equal("True", result.Variant);
    }

    [Fact]
    public async Task should_return_custom_reason_if_returned_by_relay_proxy()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        await Api.Instance.SetProviderAsync(g);
        var client = Api.Instance.GetClient("test-client");
        var result = await client.GetBooleanDetailsAsync("unknown_reason", false, _defaultEvaluationCtx);
        Assert.NotNull(result);
        Assert.True(result.Value);
        Assert.Equal(ErrorType.None, result.ErrorType);
        Assert.Equal("CUSTOM_REASON", result.Reason);
        Assert.Equal("True", result.Variant);
    }

    [Fact]
    public async Task should_use_boolean_default_value_if_the_flag_is_disabled()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        await Api.Instance.SetProviderAsync(g);
        var client = Api.Instance.GetClient("test-client");
        var result = await client.GetBooleanDetailsAsync("disabled", false, _defaultEvaluationCtx);
        Assert.NotNull(result);
        Assert.False(result.Value);
        Assert.Equal(Reason.Disabled, result.Reason);
    }

    [Fact]
    public async Task should_throw_an_error_if_we_expect_a_string_and_got_another_type()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        await Api.Instance.SetProviderAsync(g);
        var client = Api.Instance.GetClient("test-client");
        var result = await client.GetStringDetailsAsync("bool_targeting_match", "default", _defaultEvaluationCtx);
        Assert.NotNull(result);
        Assert.Equal("default", result.Value);
        Assert.Equal(ErrorType.TypeMismatch, result.ErrorType);
        Assert.Equal(Reason.Error, result.Reason);
    }

    [Fact]
    public async Task should_resolve_a_valid_string_flag_with_TARGETING_MATCH_reason()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        await Api.Instance.SetProviderAsync(g);
        var client = Api.Instance.GetClient("test-client");
        var result = await client.GetStringDetailsAsync("string_key", "defaultValue", _defaultEvaluationCtx);
        Assert.NotNull(result);
        Assert.Equal("CC0000", result.Value);
        Assert.Equal(ErrorType.None, result.ErrorType);
        Assert.Equal(Reason.TargetingMatch, result.Reason);
        Assert.Equal("True", result.Variant);
    }

    [Fact]
    public async Task should_use_string_default_value_if_the_flag_is_disabled()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        await Api.Instance.SetProviderAsync(g);
        var client = Api.Instance.GetClient("test-client");
        var result = await client.GetStringDetailsAsync("disabled_string", "defaultValue", _defaultEvaluationCtx);
        Assert.NotNull(result);
        Assert.Equal("defaultValue", result.Value);
        Assert.Equal(Reason.Disabled, result.Reason);
    }

    [Fact]
    public async Task should_throw_an_error_if_we_expect_a_integer_and_got_another_type()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        await Api.Instance.SetProviderAsync(g);
        var client = Api.Instance.GetClient("test-client");
        var result = await client.GetIntegerDetailsAsync("string_key", 200, _defaultEvaluationCtx);
        Assert.NotNull(result);
        Assert.Equal(200, result.Value);
        Assert.Equal(ErrorType.TypeMismatch, result.ErrorType);
        Assert.Equal(Reason.Error, result.Reason);
    }

    [Fact]
    public async Task should_resolve_a_valid_integer_flag_with_TARGETING_MATCH_reason()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        await Api.Instance.SetProviderAsync(g);
        var client = Api.Instance.GetClient("test-client");
        var result = await client.GetIntegerDetailsAsync("integer_key", 1200, _defaultEvaluationCtx);
        Assert.NotNull(result);
        Assert.Equal(100, result.Value);
        Assert.Equal(ErrorType.None, result.ErrorType);
        Assert.Equal(Reason.TargetingMatch, result.Reason);
        Assert.Equal("True", result.Variant);
    }

    [Fact]
    public async Task should_use_integer_default_value_if_the_flag_is_disabled()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        await Api.Instance.SetProviderAsync(g);
        var client = Api.Instance.GetClient("test-client");
        var result = await client.GetIntegerDetailsAsync("disabled_integer", 1225, _defaultEvaluationCtx);
        Assert.NotNull(result);
        Assert.Equal(1225, result.Value);
        Assert.Equal(Reason.Disabled, result.Reason);
    }

    [Fact]
    public async Task should_throw_an_error_if_we_expect_a_integer_and_double_type()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        await Api.Instance.SetProviderAsync(g);
        var client = Api.Instance.GetClient("test-client");
        var result = await client.GetIntegerDetailsAsync("double_key", 200, _defaultEvaluationCtx);
        Assert.NotNull(result);
        Assert.Equal(200, result.Value);
        Assert.Equal(ErrorType.TypeMismatch, result.ErrorType);
        Assert.Equal(Reason.Error, result.Reason);
    }

    [Fact]
    public async Task should_resolve_a_valid_double_flag_with_TARGETING_MATCH_reason()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        await Api.Instance.SetProviderAsync(g);
        var client = Api.Instance.GetClient("test-client");
        var result = await client.GetDoubleDetailsAsync("double_key", 1200.25, _defaultEvaluationCtx);
        Assert.NotNull(result);
        Assert.Equal(100.25, result.Value);
        Assert.Equal(ErrorType.None, result.ErrorType);
        Assert.Equal(Reason.TargetingMatch, result.Reason);
        Assert.Equal("True", result.Variant);
    }

    [Fact]
    public async Task should_use_double_default_value_if_the_flag_is_disabled()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        await Api.Instance.SetProviderAsync(g);
        var client = Api.Instance.GetClient("test-client");
        var result = await client.GetDoubleDetailsAsync("disabled_double", 1225.34, _defaultEvaluationCtx);
        Assert.NotNull(result);
        Assert.Equal(1225.34, result.Value);
        Assert.Equal(Reason.Disabled, result.Reason);
    }

    [Fact]
    public async Task should_resolve_a_valid_value_flag_with_TARGETING_MATCH_reason()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        await Api.Instance.SetProviderAsync(g);
        var client = Api.Instance.GetClient("test-client");
        var result = await client.GetObjectDetailsAsync("object_key", null, _defaultEvaluationCtx);
        Assert.NotNull(result);
        var want = JsonSerializer.Serialize(new Value(new Structure(new Dictionary<string, Value>
        {
            { "test", new Value("test1") }, { "test2", new Value(false) }, { "test3", new Value(123.3) },
            { "test4", new Value(1) }
        })));
        Assert.Equal(want, JsonSerializer.Serialize(result.Value));
        Assert.Equal(ErrorType.None, result.ErrorType);
        Assert.Equal(Reason.TargetingMatch, result.Reason);
        Assert.Equal("True", result.Variant);
    }

    [Fact]
    public async Task should_wrap_into_value_if_wrong_type()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        await Api.Instance.SetProviderAsync(g);
        var client = Api.Instance.GetClient("test-client");
        var result = await client.GetObjectDetailsAsync("string_key", null, _defaultEvaluationCtx);
        Assert.NotNull(result);
        Assert.Equal(new Value("CC0000").AsString, result.Value.AsString);
        Assert.Equal(ErrorType.None, result.ErrorType);
        Assert.Equal(Reason.TargetingMatch, result.Reason);
        Assert.Equal("True", result.Variant);
    }

    [Fact]
    public async Task should_use_object_default_value_if_the_flag_is_disabled()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        await Api.Instance.SetProviderAsync(g);
        var client = Api.Instance.GetClient("test-client");
        var result = await client.GetObjectDetailsAsync("disabled_object", new Value("default"), _defaultEvaluationCtx);
        Assert.NotNull(result);
        Assert.Equal(new Value("default").AsString, result.Value.AsString);
        Assert.Equal(Reason.Disabled, result.Reason);
    }


    [Fact]
    public async Task should_throw_an_error_if_no_targeting_key()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        await Api.Instance.SetProviderAsync(g);
        var client = Api.Instance.GetClient("test-client");
        var result = await client.GetStringDetailsAsync("list_key", "empty", EvaluationContext.Empty);
        Assert.NotNull(result);
        Assert.Equal("empty", result.Value);
        Assert.Equal(ErrorType.InvalidContext, result.ErrorType);
        Assert.Equal(Reason.Error, result.Reason);
    }

    [Fact]
    public async Task should_resolve_a_valid_value_flag_with_a_list()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        await Api.Instance.SetProviderAsync(g);
        var client = Api.Instance.GetClient("test-client");
        var result = await client.GetObjectDetailsAsync("list_key", null, _defaultEvaluationCtx);
        Assert.NotNull(result);
        var want = JsonSerializer.Serialize(new Value(new List<Value>
            { new("test"), new("test1"), new("test2"), new("false"), new("test3") }));
        Assert.Equal(want, JsonSerializer.Serialize(result.Value));
        Assert.Equal(ErrorType.None, result.ErrorType);
        Assert.Equal(Reason.TargetingMatch, result.Reason);
        Assert.Equal("True", result.Variant);
    }

    [Fact]
    public async Task should_use_object_default_value_if_flag_not_found()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        await Api.Instance.SetProviderAsync(g);
        var client = Api.Instance.GetClient("test-client");
        var result = await client.GetObjectDetailsAsync("does_not_exists", new Value("default"), _defaultEvaluationCtx);
        Assert.NotNull(result);
        Assert.Equal(new Value("default").AsString, result.Value.AsString);
        Assert.Equal(Reason.Error, result.Reason);
        Assert.Equal(ErrorType.FlagNotFound, result.ErrorType);
        Assert.Equal("flag does_not_exists was not found in your configuration", result.ErrorMessage);
    }

    [Fact]
    public async Task should_have_default_exporter_metadata_in_context()
    {
        string capturedRequestBody = null;
        var mock = new MockHttpMessageHandler();
        var mockedRequest = mock.When($"{prefixEval}integer_key").Respond(
            async request =>
            {
                capturedRequestBody = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
                return new HttpResponseMessage
                {
                    Content = new StringContent(
                        "{ \"value\":100, \"key\":\"integer_key\", \"reason\":\"TARGETING_MATCH\", \"variant\":\"True\", \"cacheable\":true}"
                        , Encoding.UTF8, "application/json")
                };
            });
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = mock,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        await Api.Instance.SetProviderAsync(g);
        var client = Api.Instance.GetClient("test-client");
        var res = await client.GetObjectDetailsAsync("integer_key", new Value("default"), _defaultEvaluationCtx);
        Assert.Equal(1, mock.GetMatchCount(mockedRequest));
        await Task.Delay(100); // time to wait to be sure body is extracted
        var want = JObject.Parse(
            "{\"context\":{\"labels\":[\"pro\",\"beta\"],\"gofeatureflag\":{\"openfeature\":true,\"provider\":\".NET\"},\"age\":30,\"firstname\":\"john\",\"professional\":true,\"company_info\":{\"name\":\"my_company\",\"size\":120},\"lastname\":\"doe\",\"anonymous\":false,\"rate\":3.14,\"email\":\"john.doe@gofeatureflag.org\",\"targetingKey\":\"d45e303a-38c2-11ed-a261-0242ac120002\"}}");
        var got = JObject.Parse(capturedRequestBody);
        Assert.True(JToken.DeepEquals(want, got), "unexpected json");
    }

    [Fact]
    public async Task should_have_custom_exporter_metadata_in_context()
    {
        string capturedRequestBody = null;
        var mock = new MockHttpMessageHandler();
        var mockedRequest = mock.When($"{prefixEval}integer_key").Respond(
            async request =>
            {
                capturedRequestBody = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
                return new HttpResponseMessage
                {
                    Content = new StringContent(
                        "{ \"value\":100, \"key\":\"integer_key\", \"reason\":\"TARGETING_MATCH\", \"variant\":\"True\", \"cacheable\":true}"
                        , Encoding.UTF8, "application/json")
                };
            });
        var exporterMetadata = new ExporterMetadata();
        exporterMetadata.Add("key1", "value1");
        exporterMetadata.Add("key2", 1.234);
        exporterMetadata.Add("key3", 10);
        exporterMetadata.Add("key4", false);

        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = mock,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond),
            ExporterMetadata = exporterMetadata
        });
        await Api.Instance.SetProviderAsync(g);
        var client = Api.Instance.GetClient("test-client");
        var res = await client.GetObjectDetailsAsync("integer_key", new Value("default"), _defaultEvaluationCtx);
        Assert.Equal(1, mock.GetMatchCount(mockedRequest));
        await Task.Delay(100); // time to wait to be sure body is extracted
        var want = JObject.Parse(
            "{\"context\":{\"labels\":[\"pro\",\"beta\"],\"gofeatureflag\":{\"openfeature\":true,\"provider\":\".NET\",\"key1\":\"value1\",\"key2\":1.234,\"key3\":10,\"key4\":false},\"age\":30,\"firstname\":\"john\",\"professional\":true,\"company_info\":{\"name\":\"my_company\",\"size\":120},\"lastname\":\"doe\",\"anonymous\":false,\"rate\":3.14,\"email\":\"john.doe@gofeatureflag.org\",\"targetingKey\":\"d45e303a-38c2-11ed-a261-0242ac120002\"}}");
        var got = JObject.Parse(capturedRequestBody);

        Assert.True(JToken.DeepEquals(want, got), "unexpected json");
    }

    [Fact]
    public async Task should_resolve_a_flag_with_metadata()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        await Api.Instance.SetProviderAsync(g);
        var client = Api.Instance.GetClient("test-client");
        var result = await client.GetIntegerDetailsAsync("integer_with_metadata", 1200, _defaultEvaluationCtx);
        Assert.NotNull(result);
        Assert.Equal(100, result.Value);
        Assert.Equal(ErrorType.None, result.ErrorType);
        Assert.Equal(Reason.TargetingMatch, result.Reason);
        Assert.Equal("True", result.Variant);
        Assert.NotNull(result.FlagMetadata);
        Assert.Equal("key1", result.FlagMetadata.GetString("key1"));
        Assert.Equal(1, result.FlagMetadata.GetInt("key2"));
        Assert.Equal(1.345, result.FlagMetadata.GetDouble("key3"));
        Assert.True(result.FlagMetadata.GetBool("key4"));
    }
}
