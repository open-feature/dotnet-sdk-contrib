using System;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.Test;

public class UnitTestFlagdConfig
{
    public UnitTestFlagdConfig()
    {
        Utils.CleanEnvVars();
    }

    [Fact]
    public void TestFlagdConfigDefault()
    {
        var config = FlagdConfig.Builder().Build();

        Assert.False(config.CacheEnabled);
        Assert.Equal(new Uri("http://localhost:8013"), config.GetUri());
    }

    [Fact]
    public void TestFlagdConfigDefaultLogger()
    {
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
        Environment.SetEnvironmentVariable(FlagdConfig.EnvVarTLS, "true");

        var config = FlagdConfig.Builder().Build();

        Assert.Equal(new Uri("https://localhost:8013"), config.GetUri());
    }

    [Fact]
    public void TestFlagdConfigUnixSocket()
    {
        Environment.SetEnvironmentVariable(FlagdConfig.EnvVarSocketPath, "tmp.sock");

        var config = FlagdConfig.Builder().Build();

        Assert.Equal(new Uri("unix://tmp.sock/"), config.GetUri());
    }

    [Fact]
    public void TestFlagdConfigEnabledCacheDefaultCacheSize()
    {
        Environment.SetEnvironmentVariable(FlagdConfig.EnvVarCache, "lru");

        var config = FlagdConfig.Builder().Build();

        Assert.True(config.CacheEnabled);
        Assert.Equal(FlagdConfig.CacheSizeDefault, config.MaxCacheSize);
    }

    [Fact]
    public void TestFlagdConfigEnabledCacheApplyCacheSize()
    {
        Environment.SetEnvironmentVariable(FlagdConfig.EnvVarCache, "LRU");
        Environment.SetEnvironmentVariable(FlagdConfig.EnvVarMaxCacheSize, "20");

        var config = FlagdConfig.Builder().Build();

        Assert.True(config.CacheEnabled);
        Assert.Equal(20, config.MaxCacheSize);
    }

    [Fact]
    public void TestFlagdConfigSetCertificatePath()
    {
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
        var config = FlagdConfig.Builder(new Uri("http://domain:8123")).Build();

        Assert.False(config.CacheEnabled);
        Assert.Equal(new Uri("http://domain:8123"), config.GetUri());
    }

    [Fact]
    public void TestFlagdConfigFromUriHttps()
    {
        var config = FlagdConfig.Builder(new Uri("https://domain:8123")).Build();

        Assert.False(config.CacheEnabled);
        Assert.Equal(new Uri("https://domain:8123"), config.GetUri());
    }

    [Fact]
    public void TestFlagdConfigFromUriUnix()
    {
        var config = FlagdConfig.Builder(new Uri("unix:///var/run/tmp.sock")).Build();

        Assert.False(config.CacheEnabled);
        Assert.Equal(new Uri("unix:///var/run/tmp.sock"), config.GetUri());
    }

    [Fact]
    public void TestFlagdConfigWithNullUri_ThrowsArgumentNullException()
    {
        var ex = Assert.ThrowsAny<ArgumentNullException>(() => FlagdConfig.Builder(null).Build());

        Assert.Equal("uri", ex.ParamName);
    }

    [Fact]
    public void TestFlagdConfigFromUriSetCertificatePath()
    {
        Environment.SetEnvironmentVariable(FlagdConfig.EnvCertPart, "/cert/path");

        var config = FlagdConfig.Builder(new Uri("http://localhost:8013")).Build();

        Assert.Equal("/cert/path", config.CertificatePath);
        Assert.True(config.UseCertificate);

        Utils.CleanEnvVars();

        config = FlagdConfig.Builder(new Uri("http://localhost:8013")).Build();

        Assert.Equal("", config.CertificatePath);
        Assert.False(config.UseCertificate);
    }

    [Fact]
    public void TestFlagdConfigFromUriEnabledCacheDefaultCacheSize()
    {
        Utils.CleanEnvVars();
        Environment.SetEnvironmentVariable(FlagdConfig.EnvVarCache, "LRU");

        var config = FlagdConfig.Builder(new Uri("http://localhost:8013")).Build();

        Assert.True(config.CacheEnabled);
        Assert.Equal(FlagdConfig.CacheSizeDefault, config.MaxCacheSize);
    }

    [Fact]
    public void TestFlagdConfigFromUriEnabledCacheApplyCacheSize()
    {
        Environment.SetEnvironmentVariable(FlagdConfig.EnvVarCache, "LRU");
        Environment.SetEnvironmentVariable(FlagdConfig.EnvVarMaxCacheSize, "20");

        var config = FlagdConfig.Builder(new Uri("http://localhost:8013")).Build();

        Assert.True(config.CacheEnabled);
        Assert.Equal(20, config.MaxCacheSize);
    }

    [Fact]
    public void TestFlagdConfigResolverType()
    {
        Environment.SetEnvironmentVariable(FlagdConfig.EnvVarResolverType, "in-process");
        Environment.SetEnvironmentVariable(FlagdConfig.EnvVarSourceSelector, "source-selector");

        var config = FlagdConfig.Builder(new Uri("http://localhost:8013")).Build();

        Assert.Equal(ResolverType.IN_PROCESS, config.ResolverType);
        Assert.Equal("source-selector", config.SourceSelector);
    }

    [Fact]
    public void TestFlagdConfigBuilder()
    {
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
