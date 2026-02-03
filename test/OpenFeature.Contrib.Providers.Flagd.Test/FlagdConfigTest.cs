using System;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.Test;

public class UnitTestFlagdConfig
{
    [Fact]
    public void TestFlagdConfigDefault()
    {
        Utils.CleanEnvVars();
        var config = FlagdConfig.Builder().Build();

        Assert.False(config.CacheEnabled);
        Assert.Equal(new Uri("http://localhost:8013"), config.GetUri());
    }

    [Fact]
    public void TestFlagdConfigDefaultLogger()
    {
        Utils.CleanEnvVars();
        var config = FlagdConfig.Builder().Build();

        Assert.NotNull(config.Logger);
        Assert.Equal(NullLogger.Instance, config.Logger);
    }

    [Theory]
    [InlineData(ResolverType.RPC, 8013)]
    [InlineData(ResolverType.IN_PROCESS, 8015)]
    public void WithResolverType_DefaultsPortCorrectly(ResolverType resolverType, int expectedPort)
    {
        var config = FlagdConfig.Builder()
            .WithResolverType(resolverType)
            .Build();

        Assert.Equal(expectedPort, config.Port);
    }

    [Fact]
    public void TestFlagdConfigUseTLS()
    {
        Utils.CleanEnvVars();
        Environment.SetEnvironmentVariable(FlagdConfig.EnvVarTLS, "true");

        var config = FlagdConfig.Builder().Build();

        Assert.Equal(new Uri("https://localhost:8013"), config.GetUri());
    }

    [Fact]
    public void TestFlagdConfigUnixSocket()
    {
        Utils.CleanEnvVars();
        Environment.SetEnvironmentVariable(FlagdConfig.EnvVarSocketPath, "tmp.sock");

        var config = FlagdConfig.Builder().Build();

        Assert.Equal(new Uri("unix://tmp.sock/"), config.GetUri());
    }

    [Fact]
    public void TestFlagdConfigEnabledCacheDefaultCacheSize()
    {
        Utils.CleanEnvVars();
        Environment.SetEnvironmentVariable(FlagdConfig.EnvVarCache, "lru");

        var config = FlagdConfig.Builder().Build();

        Assert.True(config.CacheEnabled);
        Assert.Equal(FlagdConfig.CacheSizeDefault, config.MaxCacheSize);
    }

    [Fact]
    public void TestFlagdConfigEnabledCacheApplyCacheSize()
    {
        Utils.CleanEnvVars();
        Environment.SetEnvironmentVariable(FlagdConfig.EnvVarCache, "LRU");
        Environment.SetEnvironmentVariable(FlagdConfig.EnvVarMaxCacheSize, "20");

        var config = FlagdConfig.Builder().Build();

        Assert.True(config.CacheEnabled);
        Assert.Equal(20, config.MaxCacheSize);
    }

    [Fact]
    public void TestFlagdConfigSetCertificatePath()
    {
        Utils.CleanEnvVars();
        Environment.SetEnvironmentVariable(FlagdConfig.EnvCertPart, "/cert/path");

        var config = FlagdConfig.Builder().Build();

        Assert.Equal("/cert/path", config.CertificatePath);
        Assert.True(config.UseCertificate);

        Utils.CleanEnvVars();

        config = FlagdConfig.Builder().Build();

        Assert.Equal("", config.CertificatePath);
        Assert.False(config.UseCertificate);
    }

    [Fact]
    public void TestFlagdConfigFromUriHttp()
    {
        Utils.CleanEnvVars();
        var config = FlagdConfig.Builder()
            .WithHost("domain")
            .WithPort(8123)
            .WithTls(false)
            .Build();

        Assert.False(config.CacheEnabled);
        Assert.Equal(new Uri("http://domain:8123"), config.GetUri());
    }

    [Fact]
    public void TestFlagdConfigFromUriHttps()
    {
        Utils.CleanEnvVars();
        var config = FlagdConfig.Builder()
            .WithHost("domain")
            .WithPort(8123)
            .WithTls(true)
            .Build();

        Assert.False(config.CacheEnabled);
        Assert.Equal(new Uri("https://domain:8123"), config.GetUri());
    }

    [Fact]
    public void TestFlagdConfigFromUriUnix()
    {
        Utils.CleanEnvVars();
        var config = FlagdConfig.Builder()
            .WithSocketPath("/var/run/tmp.sock")
            .Build();

        Assert.False(config.CacheEnabled);
        Assert.Equal(new Uri("unix:///var/run/tmp.sock"), config.GetUri());
    }

    [Fact]
    public void TestFlagdConfigFromUriSetCertificatePath()
    {
        Utils.CleanEnvVars();
        Environment.SetEnvironmentVariable(FlagdConfig.EnvCertPart, "/cert/path");

        var config = FlagdConfig.Builder()
            .WithHost("localhost")
            .WithPort(8013)
            .WithTls(false)
            .Build();

        Assert.Equal("/cert/path", config.CertificatePath);
        Assert.True(config.UseCertificate);

        Utils.CleanEnvVars();

        config = FlagdConfig.Builder()
            .WithHost("localhost")
            .WithPort(8013)
            .WithTls(false)
            .Build();

        Assert.Equal("", config.CertificatePath);
        Assert.False(config.UseCertificate);
    }

    [Fact]
    public void TestFlagdConfigFromUriEnabledCacheDefaultCacheSize()
    {
        Utils.CleanEnvVars();
        Environment.SetEnvironmentVariable(FlagdConfig.EnvVarCache, "LRU");

        var config = FlagdConfig.Builder()
            .WithHost("localhost")
            .WithPort(8013)
            .WithTls(false)
            .Build();

        Assert.True(config.CacheEnabled);
        Assert.Equal(FlagdConfig.CacheSizeDefault, config.MaxCacheSize);
    }

    [Fact]
    public void TestFlagdConfigFromUriEnabledCacheApplyCacheSize()
    {
        Utils.CleanEnvVars();
        Environment.SetEnvironmentVariable(FlagdConfig.EnvVarCache, "LRU");
        Environment.SetEnvironmentVariable(FlagdConfig.EnvVarMaxCacheSize, "20");

        var config = FlagdConfig.Builder()
            .WithHost("localhost")
            .WithPort(8013)
            .WithTls(false)
            .Build();

        Assert.True(config.CacheEnabled);
        Assert.Equal(20, config.MaxCacheSize);
    }

    [Fact]
    public void TestFlagdConfigResolverType()
    {
        Utils.CleanEnvVars();
        Environment.SetEnvironmentVariable(FlagdConfig.EnvVarResolverType, "in-process");
        Environment.SetEnvironmentVariable(FlagdConfig.EnvVarSourceSelector, "source-selector");

        var config = FlagdConfig.Builder()
            .WithHost("localhost")
            .WithPort(8013)
            .WithTls(false)
            .Build();

        Assert.Equal(ResolverType.IN_PROCESS, config.ResolverType);
        Assert.Equal("source-selector", config.SourceSelector);
    }

    [Fact]
    public void TestFlagdConfigBuilder()
    {
        Utils.CleanEnvVars();

        var logger = new FakeLogger<UnitTestFlagdConfig>();
        var config = new FlagdConfigBuilder()
            .WithCache(true)
            .WithMaxCacheSize(1)
            .WithCertificatePath("cert-path")
            .WithHost("some-host")
            .WithPort(8888)
            .WithResolverType(ResolverType.IN_PROCESS)
            .WithSocketPath("some-socket")
            .WithTls(true)
            .WithSourceSelector("source-selector")
            .WithLogger(logger)
            .Build();

        Assert.Equal(ResolverType.IN_PROCESS, config.ResolverType);
        Assert.Equal("source-selector", config.SourceSelector);
        Assert.True(config.CacheEnabled);
        Assert.Equal(1, config.MaxCacheSize);
        Assert.Equal("cert-path", config.CertificatePath);
        Assert.Equal("some-host", config.Host);
        Assert.Equal(8888, config.Port);
        Assert.Equal("some-socket", config.SocketPath);
        Assert.True(config.UseTls);
        Assert.True(config.UseCertificate);
        Assert.Equal(logger, config.Logger);
    }
}
