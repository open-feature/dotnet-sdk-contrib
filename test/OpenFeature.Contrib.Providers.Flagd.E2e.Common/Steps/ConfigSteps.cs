using System;
using System.Collections.Generic;
using OpenFeature.Contrib.Providers.Flagd.E2e.Common.Utils;
using Reqnroll;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.Common.Steps;

#nullable enable

[Binding]
public class ConfigSteps
{
    private readonly State _state;

    private FlagdConfig? _config;
    private bool _errorOccurred;

    private static readonly HashSet<string> _environmentVariables =
    [
        "FLAGD_RESOLVER",
        "FLAGD_HOST",
        "FLAGD_PORT",
        "FLAGD_TARGET_URI",
        "FLAGD_TLS",
        "FLAGD_SOCKET_PATH",
        "FLAGD_SERVER_CERT_PATH",
        "FLAGD_DEADLINE_MS",
        "FLAGD_STREAM_DEADLINE_MS",
        "FLAGD_RETRY_BACKOFF_MS",
        "FLAGD_RETRY_BACKOFF_MAX_MS",
        "FLAGD_RETRY_GRACE_PERIOD",
        "FLAGD_KEEP_ALIVE_TIME_MS",
        "FLAGD_CACHE",
        "FLAGD_MAX_CACHE_SIZE",
        "FLAGD_SOURCE_SELECTOR",
        "FLAGD_OFFLINE_FLAG_SOURCE_PATH",
        "FLAGD_OFFLINE_POLL_MS",
        "FLAGD_FATAL_STATUS_CODES"
    ];

    private static readonly HashSet<string> _unsupportedEnvironmentVairables =
    [
        "FLAGD_PROVIDER_ID"
    ];

    private static readonly HashSet<string> _unsupportedConfigOptions =
    [
        "deadlineMs",
        "fatalStatusCodes",
        "targetUri",
        "providerId",
        "offlineFlagSourcePath",
        "offlinePollIntervalMs",
        "streamDeadlineMs",
        "keepAliveTime",
        "retryBackoffMs",
        "retryBackoffMaxMs",
        "retryGracePeriod"
    ];

    public ConfigSteps(State state)
    {
        this._state = state;
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        foreach (var envVar in _environmentVariables)
        {
            Environment.SetEnvironmentVariable(envVar, null);
        }
    }

    [Given("an option {string} of type {string} with value {string}")]
    public void GivenAnOptionOfTypeWithValue(string option, string _, string value)
    {
        Skip.If(_unsupportedConfigOptions.Contains(option), "Config option is not supported");

        this._state.FlagdConfig ??= FlagdConfig.Builder();

        switch (option)
        {
            case "host":
                {
                    this._state.FlagdConfig = this._state.FlagdConfig.WithHost(value);
                    break;
                }
            case "port":
                {
                    var port = int.Parse(value);
                    this._state.FlagdConfig = this._state.FlagdConfig.WithPort(port);
                    break;
                }
            case "tls":
                {
                    var useTls = bool.Parse(value);
                    this._state.FlagdConfig = this._state.FlagdConfig.WithTls(useTls);
                    break;
                }
            case "certPath":
                {
                    this._state.FlagdConfig = this._state.FlagdConfig.WithCertificatePath(value);
                    break;
                }
            case "socketPath":
                {
                    this._state.FlagdConfig = this._state.FlagdConfig.WithSocketPath(value);
                    break;
                }
            case "cache":
                {
                    var enabled = value == "enabled";
                    this._state.FlagdConfig = this._state.FlagdConfig.WithCache(enabled);
                    break;
                }
            case "maxCacheSize":
                {
                    var maxCacheSize = int.Parse(value);
                    this._state.FlagdConfig = this._state.FlagdConfig.WithMaxCacheSize(maxCacheSize);
                    break;
                }
            case "selector":
                {
                    this._state.FlagdConfig = this._state.FlagdConfig.WithSourceSelector(value);
                    break;
                }
            case "resolver":
                {
                    var resolverType = value == "rpc" ? ResolverType.RPC : ResolverType.IN_PROCESS;
                    this._state.FlagdConfig = this._state.FlagdConfig.WithResolverType(resolverType);
                    break;
                }
            default:
                break;
        }
    }

    [When("a config was initialized")]
    public void WhenAConfigWasInitialized()
    {
        try
        {
            var flagdConfigBuilder = this._state.FlagdConfig ?? FlagdConfig.Builder();
            this._config = flagdConfigBuilder.Build();

            Assert.NotNull(this._config);
        }
        catch (Exception)
        {
            this._errorOccurred = true;
        }
    }

    [Then("the option {string} of type {string} should have the value {string}")]
    public void ThenTheOptionOfTypeShouldHaveTheValue(string option, string _, string value)
    {
        Skip.If(_unsupportedConfigOptions.Contains(option), "Config option is not supported");

        switch (option)
        {
            case "resolver":
                {
                    var expected = value.ToLower();
                    var actual = this._config!.ResolverType == ResolverType.RPC ? "rpc" : "in-process";
                    Assert.Equal(expected, actual);
                    break;
                }
            case "host":
                {
                    var expected = value;
                    var actual = this._config!.Host;
                    Assert.Equal(expected, actual);
                    break;
                }
            case "port":
                {
                    var expected = int.Parse(value);
                    var actual = this._config!.Port;
                    Assert.Equal(expected, actual);
                    break;
                }
            case "tls":
                {
                    var expected = bool.Parse(value);
                    var actual = this._config!.UseTls;
                    Assert.Equal(expected, actual);
                    break;
                }
            case "certPath":
                {
                    var expected = value == "null" ? string.Empty : value;
                    var actual = this._config!.CertificatePath;
                    Assert.Equal(expected, actual);
                    break;
                }
            case "socketPath":
                {
                    var expected = value == "null" ? string.Empty : value;
                    var actual = this._config!.SocketPath;
                    Assert.Equal(expected, actual);
                    break;
                }
            case "selector":
                {
                    var expected = value == "null" ? string.Empty : value;
                    var actual = this._config!.SourceSelector;
                    Assert.Equal(expected, actual);
                    break;
                }

            default:
                break;
        }
    }

    [Then("we should have an error")]
    public void ThenWeShouldHaveAnError()
    {
        Assert.True(this._errorOccurred);
    }

    [Given("an environment variable {string} with value {string}")]
    public void GivenAnEnvironmentVariableWithValue(string env, string value)
    {
        Skip.If(_unsupportedEnvironmentVairables.Contains(env), "Environment variable is not supported");

        Assert.Contains(_environmentVariables, e => e == env); // Ensure only known env vars are set

        Environment.SetEnvironmentVariable(env, value);
    }
}
