using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using OpenFeature.Constant;
using OpenFeature.Contrib.Providers.GOFeatureFlag.exception;
using OpenFeature.Model;
using RichardSzalay.MockHttp;
using Xunit;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.Test;

public class GoFeatureFlagProviderTest
{
    private static readonly string baseUrl = "http://gofeatureflag.org";
    private static readonly string prefixEval = baseUrl + "/v1/feature/";
    private static readonly string suffixEval = "/eval";
    private readonly EvaluationContext _defaultEvaluationCtx = InitDefaultEvaluationCtx();
    private readonly HttpMessageHandler _mockHttp = InitMock();

    private static HttpMessageHandler InitMock()
    {
        const string mediaType = "application/json";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"{prefixEval}fail_500{suffixEval}").Respond(HttpStatusCode.InternalServerError);
        mockHttp.When($"{prefixEval}flag_not_found{suffixEval}").Respond(HttpStatusCode.NotFound);
        mockHttp.When($"{prefixEval}bool_targeting_match{suffixEval}").Respond(mediaType,
            "{\"trackEvents\":true,\"variationType\":\"True\",\"failed\":false,\"version\":\"\",\"reason\":\"TARGETING_MATCH\",\"errorCode\":\"\",\"value\":true}");
        mockHttp.When($"{prefixEval}disabled{suffixEval}").Respond(mediaType,
            "{\"trackEvents\":true,\"variationType\":\"defaultSdk\",\"failed\":false,\"version\":\"\",\"reason\":\"DISABLED\",\"errorCode\":\"\",\"value\":true}");
        mockHttp.When($"{prefixEval}disabled_double{suffixEval}").Respond(mediaType,
            "{\"trackEvents\":true,\"variationType\":\"defaultSdk\",\"failed\":false,\"version\":\"\",\"reason\":\"DISABLED\",\"errorCode\":\"\",\"value\":100.25}");
        mockHttp.When($"{prefixEval}disabled_integer{suffixEval}").Respond(mediaType,
            "{\"trackEvents\":true,\"variationType\":\"defaultSdk\",\"failed\":false,\"version\":\"\",\"reason\":\"DISABLED\",\"errorCode\":\"\",\"value\":100}");
        mockHttp.When($"{prefixEval}disabled_object{suffixEval}").Respond(mediaType,
            "{\"trackEvents\":true,\"variationType\":\"defaultSdk\",\"failed\":false,\"version\":\"\",\"reason\":\"DISABLED\",\"errorCode\":\"\",\"value\":null}");
        mockHttp.When($"{prefixEval}disabled_string{suffixEval}").Respond(mediaType,
            "{\"trackEvents\":true,\"variationType\":\"defaultSdk\",\"failed\":false,\"version\":\"\",\"reason\":\"DISABLED\",\"errorCode\":\"\",\"value\":\"\"}");
        mockHttp.When($"{prefixEval}double_key{suffixEval}").Respond(mediaType,
            "{\"trackEvents\":true,\"variationType\":\"True\",\"failed\":false,\"version\":\"\",\"reason\":\"TARGETING_MATCH\",\"errorCode\":\"\",\"value\":100.25}");
        mockHttp.When($"{prefixEval}flag_not_found{suffixEval}").Respond(mediaType,
            "{\"trackEvents\":true,\"variationType\":\"SdkDefault\",\"failed\":true,\"version\":\"\",\"reason\":\"ERROR\",\"errorCode\":\"FLAG_NOT_FOUND\",\"value\":\"false\"}");
        mockHttp.When($"{prefixEval}integer_key{suffixEval}").Respond(mediaType,
            "{\"trackEvents\":true,\"variationType\":\"True\",\"failed\":false,\"version\":\"\",\"reason\":\"TARGETING_MATCH\",\"errorCode\":\"\",\"value\":100}");
        mockHttp.When($"{prefixEval}list_key{suffixEval}").Respond(mediaType,
            "{\"trackEvents\":true,\"variationType\":\"True\",\"failed\":false,\"version\":\"\",\"reason\":\"TARGETING_MATCH\",\"errorCode\":\"\",\"value\":[\"test\",\"test1\",\"test2\",\"false\",\"test3\"]}");
        mockHttp.When($"{prefixEval}object_key{suffixEval}").Respond(mediaType,
            "{\"trackEvents\":true,\"variationType\":\"True\",\"failed\":false,\"version\":\"\",\"reason\":\"TARGETING_MATCH\",\"errorCode\":\"\",\"value\":{\"test\":\"test1\",\"test2\":false,\"test3\":123.3,\"test4\":1,\"test5\":null}}");
        mockHttp.When($"{prefixEval}string_key{suffixEval}").Respond(mediaType,
            "{\"trackEvents\":true,\"variationType\":\"True\",\"failed\":false,\"version\":\"\",\"reason\":\"TARGETING_MATCH\",\"errorCode\":\"\",\"value\":\"CC0000\"}");
        mockHttp.When($"{prefixEval}unknown_reason{suffixEval}").Respond(mediaType,
            "{\"trackEvents\":true,\"variationType\":\"True\",\"failed\":false,\"version\":\"\",\"reason\":\"CUSTOM_REASON\",\"errorCode\":\"\",\"value\":true}");

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
    private void getMetadata_validate_name()
    {
        var goFeatureFlagProvider = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Timeout = new TimeSpan(19 * TimeSpan.TicksPerHour),
            Endpoint = baseUrl
        });
        Api.Instance.SetProvider(goFeatureFlagProvider);
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
    private void should_throw_an_error_if_endpoint_not_available()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        Api.Instance.SetProvider(g);
        var client = Api.Instance.GetClient("test-client");
        var res = client.GetBooleanDetails("fail_500", false, _defaultEvaluationCtx);
        Assert.NotNull(res.Result);
        Assert.False(res.Result.Value);
        Assert.Equal(ErrorType.General, res.Result.ErrorType);
        Assert.Equal(Reason.Error, res.Result.Reason);
    }

    [Fact]
    private void should_throw_an_error_if_flag_does_not_exists()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        Api.Instance.SetProvider(g);
        var client = Api.Instance.GetClient("test-client");
        var res = client.GetBooleanDetails("flag_not_found", false, _defaultEvaluationCtx);
        Assert.NotNull(res.Result);
        Assert.False(res.Result.Value);
        Assert.Equal(ErrorType.FlagNotFound, res.Result.ErrorType);
        Assert.Equal(Reason.Error, res.Result.Reason);
    }

    [Fact]
    private void should_throw_an_error_if_we_expect_a_boolean_and_got_another_type()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        Api.Instance.SetProvider(g);
        var client = Api.Instance.GetClient("test-client");
        var res = client.GetBooleanDetails("string_key", false, _defaultEvaluationCtx);
        Assert.NotNull(res.Result);
        Assert.False(res.Result.Value);
        Assert.Equal(ErrorType.TypeMismatch, res.Result.ErrorType);
        Assert.Equal(Reason.Error, res.Result.Reason);
    }

    [Fact]
    private void should_resolve_a_valid_boolean_flag_with_TARGETING_MATCH_reason()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        Api.Instance.SetProvider(g);
        var client = Api.Instance.GetClient("test-client");
        var res = client.GetBooleanDetails("bool_targeting_match", false, _defaultEvaluationCtx);
        Assert.NotNull(res.Result);
        Assert.True(res.Result.Value);
        Assert.Equal(ErrorType.None, res.Result.ErrorType);
        Assert.Equal(Reason.TargetingMatch, res.Result.Reason);
        Assert.Equal("True", res.Result.Variant);
    }

    [Fact]
    private void should_return_custom_reason_if_returned_by_relay_proxy()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        Api.Instance.SetProvider(g);
        var client = Api.Instance.GetClient("test-client");
        var res = client.GetBooleanDetails("unknown_reason", false, _defaultEvaluationCtx);
        Assert.NotNull(res.Result);
        Assert.True(res.Result.Value);
        Assert.Equal(ErrorType.None, res.Result.ErrorType);
        Assert.Equal("CUSTOM_REASON", res.Result.Reason);
        Assert.Equal("True", res.Result.Variant);
    }

    [Fact]
    private void should_use_boolean_default_value_if_the_flag_is_disabled()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        Api.Instance.SetProvider(g);
        var client = Api.Instance.GetClient("test-client");
        var res = client.GetBooleanDetails("disabled", false, _defaultEvaluationCtx);
        Assert.NotNull(res.Result);
        Assert.False(res.Result.Value);
        Assert.Equal(Reason.Disabled, res.Result.Reason);
    }

    [Fact]
    private void should_throw_an_error_if_we_expect_a_string_and_got_another_type()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        Api.Instance.SetProvider(g);
        var client = Api.Instance.GetClient("test-client");
        var res = client.GetStringDetails("bool_targeting_match", "default", _defaultEvaluationCtx);
        Assert.NotNull(res.Result);
        Assert.Equal("default", res.Result.Value);
        Assert.Equal(ErrorType.TypeMismatch, res.Result.ErrorType);
        Assert.Equal(Reason.Error, res.Result.Reason);
    }

    [Fact]
    private void should_resolve_a_valid_string_flag_with_TARGETING_MATCH_reason()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        Api.Instance.SetProvider(g);
        var client = Api.Instance.GetClient("test-client");
        var res = client.GetStringDetails("string_key", "defaultValue", _defaultEvaluationCtx);
        Assert.NotNull(res.Result);
        Assert.Equal("CC0000", res.Result.Value);
        Assert.Equal(ErrorType.None, res.Result.ErrorType);
        Assert.Equal(Reason.TargetingMatch, res.Result.Reason);
        Assert.Equal("True", res.Result.Variant);
    }

    [Fact]
    private void should_use_string_default_value_if_the_flag_is_disabled()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        Api.Instance.SetProvider(g);
        var client = Api.Instance.GetClient("test-client");
        var res = client.GetStringDetails("disabled_string", "defaultValue", _defaultEvaluationCtx);
        Assert.NotNull(res.Result);
        Assert.Equal("defaultValue", res.Result.Value);
        Assert.Equal(Reason.Disabled, res.Result.Reason);
    }

    [Fact]
    private void should_throw_an_error_if_we_expect_a_integer_and_got_another_type()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        Api.Instance.SetProvider(g);
        var client = Api.Instance.GetClient("test-client");
        var res = client.GetIntegerDetails("string_key", 200, _defaultEvaluationCtx);
        Assert.NotNull(res.Result);
        Assert.Equal(200, res.Result.Value);
        Assert.Equal(ErrorType.TypeMismatch, res.Result.ErrorType);
        Assert.Equal(Reason.Error, res.Result.Reason);
    }

    [Fact]
    private void should_resolve_a_valid_integer_flag_with_TARGETING_MATCH_reason()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        Api.Instance.SetProvider(g);
        var client = Api.Instance.GetClient("test-client");
        var res = client.GetIntegerDetails("integer_key", 1200, _defaultEvaluationCtx);
        Assert.NotNull(res.Result);
        Assert.Equal(100, res.Result.Value);
        Assert.Equal(ErrorType.None, res.Result.ErrorType);
        Assert.Equal(Reason.TargetingMatch, res.Result.Reason);
        Assert.Equal("True", res.Result.Variant);
    }

    [Fact]
    private void should_use_integer_default_value_if_the_flag_is_disabled()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        Api.Instance.SetProvider(g);
        var client = Api.Instance.GetClient("test-client");
        var res = client.GetIntegerDetails("disabled_integer", 1225, _defaultEvaluationCtx);
        Assert.NotNull(res.Result);
        Assert.Equal(1225, res.Result.Value);
        Assert.Equal(Reason.Disabled, res.Result.Reason);
    }

    [Fact]
    private void should_throw_an_error_if_we_expect_a_integer_and_double_type()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        Api.Instance.SetProvider(g);
        var client = Api.Instance.GetClient("test-client");
        var res = client.GetIntegerDetails("double_key", 200, _defaultEvaluationCtx);
        Assert.NotNull(res.Result);
        Assert.Equal(200, res.Result.Value);
        Assert.Equal(ErrorType.TypeMismatch, res.Result.ErrorType);
        Assert.Equal(Reason.Error, res.Result.Reason);
    }

    [Fact]
    private void should_resolve_a_valid_double_flag_with_TARGETING_MATCH_reason()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        Api.Instance.SetProvider(g);
        var client = Api.Instance.GetClient("test-client");
        var res = client.GetDoubleDetails("double_key", 1200.25, _defaultEvaluationCtx);
        Assert.NotNull(res.Result);
        Assert.Equal(100.25, res.Result.Value);
        Assert.Equal(ErrorType.None, res.Result.ErrorType);
        Assert.Equal(Reason.TargetingMatch, res.Result.Reason);
        Assert.Equal("True", res.Result.Variant);
    }

    [Fact]
    private void should_use_double_default_value_if_the_flag_is_disabled()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        Api.Instance.SetProvider(g);
        var client = Api.Instance.GetClient("test-client");
        var res = client.GetDoubleDetails("disabled_double", 1225.34, _defaultEvaluationCtx);
        Assert.NotNull(res.Result);
        Assert.Equal(1225.34, res.Result.Value);
        Assert.Equal(Reason.Disabled, res.Result.Reason);
    }

    [Fact]
    private void should_resolve_a_valid_value_flag_with_TARGETING_MATCH_reason()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        Api.Instance.SetProvider(g);
        var client = Api.Instance.GetClient("test-client");
        var res = client.GetObjectDetails("object_key", null, _defaultEvaluationCtx);
        Assert.NotNull(res.Result);
        var want = JsonSerializer.Serialize(new Value(new Structure(new Dictionary<string, Value>
        {
            { "test", new Value("test1") }, { "test2", new Value(false) }, { "test3", new Value(123.3) },
            { "test4", new Value(1) }
        })));
        Assert.Equal(want, JsonSerializer.Serialize(res.Result.Value));
        Assert.Equal(ErrorType.None, res.Result.ErrorType);
        Assert.Equal(Reason.TargetingMatch, res.Result.Reason);
        Assert.Equal("True", res.Result.Variant);
    }

    [Fact]
    private void should_wrap_into_value_if_wrong_type()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        Api.Instance.SetProvider(g);
        var client = Api.Instance.GetClient("test-client");
        var res = client.GetObjectDetails("string_key", null, _defaultEvaluationCtx);
        Assert.NotNull(res.Result);
        Assert.Equal(new Value("CC0000").AsString, res.Result.Value.AsString);
        Assert.Equal(ErrorType.None, res.Result.ErrorType);
        Assert.Equal(Reason.TargetingMatch, res.Result.Reason);
        Assert.Equal("True", res.Result.Variant);
    }

    [Fact]
    private void should_use_object_default_value_if_the_flag_is_disabled()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        Api.Instance.SetProvider(g);
        var client = Api.Instance.GetClient("test-client");
        var res = client.GetObjectDetails("disabled_object", new Value("default"), _defaultEvaluationCtx);
        Assert.NotNull(res.Result);
        Assert.Equal(new Value("default").AsString, res.Result.Value.AsString);
        Assert.Equal(Reason.Disabled, res.Result.Reason);
    }


    [Fact]
    private void should_throw_an_error_if_no_targeting_key()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        Api.Instance.SetProvider(g);
        var client = Api.Instance.GetClient("test-client");
        var res = client.GetStringDetails("list_key", "empty", EvaluationContext.Empty);
        Assert.NotNull(res.Result);
        Assert.Equal("empty", res.Result.Value);
        Assert.Equal(ErrorType.InvalidContext, res.Result.ErrorType);
        Assert.Equal(Reason.Error, res.Result.Reason);
    }

    [Fact]
    private void should_resolve_a_valid_value_flag_with_a_list()
    {
        var g = new GoFeatureFlagProvider(new GoFeatureFlagProviderOptions
        {
            Endpoint = baseUrl,
            HttpMessageHandler = _mockHttp,
            Timeout = new TimeSpan(1000 * TimeSpan.TicksPerMillisecond)
        });
        Api.Instance.SetProvider(g);
        var client = Api.Instance.GetClient("test-client");
        var res = client.GetObjectDetails("list_key", null, _defaultEvaluationCtx);
        Assert.NotNull(res.Result);
        var want = JsonSerializer.Serialize(new Value(new List<Value>
            { new("test"), new("test1"), new("test2"), new("false"), new("test3") }));
        Assert.Equal(want, JsonSerializer.Serialize(res.Result.Value));
        Assert.Equal(ErrorType.None, res.Result.ErrorType);
        Assert.Equal(Reason.TargetingMatch, res.Result.Reason);
        Assert.Equal("True", res.Result.Variant);
    }
}