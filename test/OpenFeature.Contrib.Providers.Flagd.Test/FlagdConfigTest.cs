using System;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.Test;

public class UnitTestFlagdConfig
{
    [Fact]
    public void TestFlagdConfigDefault()
    {
        Utils.CleanEnvVars();
        var config = new FlagdConfig();

        Assert.False(config.CacheEnabled);
        Assert.Equal(new Uri("http://localhost:8013"), config.GetUri());
    }

    [Fact]
    public void TestFlagdConfigUseTLS()
    {
        Utils.CleanEnvVars();
        Environment.SetEnvironmentVariable(FlagdConfig.EnvVarTLS, "true");

        var config = new FlagdConfig();

        Assert.Equal(new Uri("https://localhost:8013"), config.GetUri());
    }

    [Fact]
    public void TestFlagdConfigUnixSocket()
    {
        Utils.CleanEnvVars();
        Environment.SetEnvironmentVariable(FlagdConfig.EnvVarSocketPath, "tmp.sock");

        var config = new FlagdConfig();

        Assert.Equal(new Uri("unix://tmp.sock/"), config.GetUri());
    }

    [Fact]
    public void TestFlagdConfigEnabledCacheDefaultCacheSize()
    {
        Utils.CleanEnvVars();
        Environment.SetEnvironmentVariable(FlagdConfig.EnvVarCache, "lru");

        var config = new FlagdConfig();

        Assert.True(config.CacheEnabled);
        Assert.Equal(FlagdConfig.CacheSizeDefault, config.MaxCacheSize);
    }

    [Fact]
    public void TestFlagdConfigEnabledCacheApplyCacheSize()
    {
        Utils.CleanEnvVars();
        Environment.SetEnvironmentVariable(FlagdConfig.EnvVarCache, "LRU");
        Environment.SetEnvironmentVariable(FlagdConfig.EnvVarMaxCacheSize, "20");

        var config = new FlagdConfig();

        Assert.True(config.CacheEnabled);
        Assert.Equal(20, config.MaxCacheSize);
    }

    [Fact]
    public void TestFlagdConfigSetCertificatePath()
    {
        Utils.CleanEnvVars();
        Environment.SetEnvironmentVariable(FlagdConfig.EnvCertPart, "/cert/path");

        var config = new FlagdConfig();

        Assert.Equal("/cert/path", config.CertificatePath);
        Assert.True(config.UseCertificate);

        Utils.CleanEnvVars();

        config = new FlagdConfig();

        Assert.Equal("", config.CertificatePath);
        Assert.False(config.UseCertificate);
    }

    [Fact]
    public void TestFlagdConfigFromUriHttp()
    {
        Utils.CleanEnvVars();
        var config = new FlagdConfig(new Uri("http://domain:8123"));

        Assert.False(config.CacheEnabled);
        Assert.Equal(new Uri("http://domain:8123"), config.GetUri());
    }

    [Fact]
    public void TestFlagdConfigFromUriHttps()
    {
        Utils.CleanEnvVars();
        var config = new FlagdConfig(new Uri("https://domain:8123"));

        Assert.False(config.CacheEnabled);
        Assert.Equal(new Uri("https://domain:8123"), config.GetUri());
    }

    [Fact]
    public void TestFlagdConfigFromUriUnix()
    {
        Utils.CleanEnvVars();
        var config = new FlagdConfig(new Uri("unix:///var/run/tmp.sock"));

        Assert.False(config.CacheEnabled);
        Assert.Equal(new Uri("unix:///var/run/tmp.sock"), config.GetUri());
    }

    [Fact]
    public void TestFlagdConfigFromUriSetCertificatePath()
    {
        Utils.CleanEnvVars();
        Environment.SetEnvironmentVariable(FlagdConfig.EnvCertPart, "/cert/path");

        var config = new FlagdConfig(new Uri("http://localhost:8013"));

        Assert.Equal("/cert/path", config.CertificatePath);
        Assert.True(config.UseCertificate);

        Utils.CleanEnvVars();

        config = new FlagdConfig(new Uri("http://localhost:8013"));

        Assert.Equal("", config.CertificatePath);
        Assert.False(config.UseCertificate);
    }

    [Fact]
    public void TestFlagdConfigFromUriEnabledCacheDefaultCacheSize()
    {
        Utils.CleanEnvVars();
        Environment.SetEnvironmentVariable(FlagdConfig.EnvVarCache, "LRU");

        var config = new FlagdConfig(new Uri("http://localhost:8013"));

        Assert.True(config.CacheEnabled);
        Assert.Equal(FlagdConfig.CacheSizeDefault, config.MaxCacheSize);
    }

    [Fact]
    public void TestFlagdConfigFromUriEnabledCacheApplyCacheSize()
    {
        Utils.CleanEnvVars();
        Environment.SetEnvironmentVariable(FlagdConfig.EnvVarCache, "LRU");
        Environment.SetEnvironmentVariable(FlagdConfig.EnvVarMaxCacheSize, "20");

        var config = new FlagdConfig(new Uri("http://localhost:8013"));

        Assert.True(config.CacheEnabled);
        Assert.Equal(20, config.MaxCacheSize);
    }

    [Fact]
    public void TestFlagdConfigResolverType()
    {
        Utils.CleanEnvVars();
        Environment.SetEnvironmentVariable(FlagdConfig.EnvVarResolverType, "in-process");
        Environment.SetEnvironmentVariable(FlagdConfig.EnvVarSourceSelector, "source-selector");

        var config = new FlagdConfig(new Uri("http://localhost:8013"));

        Assert.Equal(ResolverType.IN_PROCESS, config.ResolverType);
        Assert.Equal("source-selector", config.SourceSelector);
    }

    [Fact]
    public void TestFlagdConfigBuilder()
    {
        Utils.CleanEnvVars();
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

    }
}
