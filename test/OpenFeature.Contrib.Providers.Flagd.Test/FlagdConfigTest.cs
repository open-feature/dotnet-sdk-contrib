using System;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.Test
{
    public class UnitTestFlagdConfig
    {
        [Fact]
        public void TestFlagdConfigDefault()
        {
            CleanEnvVars();
            var config = new FlagdConfig();

            Assert.False(config.CacheEnabled);
            Assert.Equal(new System.Uri("http://localhost:8013"), config.GetUri());
        }

        [Fact]
        public void TestFlagdConfigUseTLS()
        {
            CleanEnvVars();
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvVarTLS, "true");

            var config = new FlagdConfig();

            Assert.Equal(new System.Uri("https://localhost:8013"), config.GetUri());
        }

        [Fact]
        public void TestFlagdConfigUnixSocket()
        {
            CleanEnvVars();
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvVarSocketPath, "tmp.sock");

            var config = new FlagdConfig();

            Assert.Equal(new System.Uri("unix://tmp.sock/"), config.GetUri());
        }

        [Fact]
        public void TestFlagdConfigEnabledCacheDefaultCacheSize()
        {
            CleanEnvVars();
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvVarCache, "LRU");

            var config = new FlagdConfig();

            Assert.True(config.CacheEnabled);
            Assert.Equal(FlagdConfig.CacheSizeDefault, config.MaxCacheSize);
        }

        [Fact]
        public void TestFlagdConfigEnabledCacheApplyCacheSize()
        {
            CleanEnvVars();
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvVarCache, "LRU");
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvVarMaxCacheSize, "20");

            var config = new FlagdConfig();

            Assert.True(config.CacheEnabled);
            Assert.Equal(20, config.MaxCacheSize);
        }

        [Fact]
        public void TestFlagdConfigSetCertificatePath()
        {
            CleanEnvVars();
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvCertPart, "/cert/path");

            var config = new FlagdConfig();

            Assert.Equal("/cert/path", config.CertificatePath);
            Assert.True(config.UseCertificate);

            CleanEnvVars();

            config = new FlagdConfig();

            Assert.Equal("", config.CertificatePath);
            Assert.False(config.UseCertificate);
        }

        [Fact]
        public void TestFlagdConfigFromUriHttp()
        {
            CleanEnvVars();
            var config = new FlagdConfig(new Uri("http://domain:8123"));

            Assert.False(config.CacheEnabled);
            Assert.Equal(new System.Uri("http://domain:8123"), config.GetUri());
        }
        
        [Fact]
        public void TestFlagdConfigFromUriHttps()
        {
            CleanEnvVars();
            var config = new FlagdConfig(new Uri("https://domain:8123"));

            Assert.False(config.CacheEnabled);
            Assert.Equal(new System.Uri("https://domain:8123"), config.GetUri());
        }
        
        [Fact]
        public void TestFlagdConfigFromUriUnix()
        {
            CleanEnvVars();
            var config = new FlagdConfig(new Uri("unix:///var/run/tmp.sock/"));

            Assert.False(config.CacheEnabled);
            Assert.Equal(new System.Uri("unix:///var/run/tmp.sock/"), config.GetUri());
        }
        
        [Fact]
        public void TestFlagdConfigFromUriSetCertificatePath()
        {
            CleanEnvVars();
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvCertPart, "/cert/path");

            var config = new FlagdConfig(new Uri("http://localhost:8013"));

            Assert.Equal("/cert/path", config.CertificatePath);
            Assert.True(config.UseCertificate);

            CleanEnvVars();

            config = new FlagdConfig(new Uri("http://localhost:8013"));

            Assert.Equal("", config.CertificatePath);
            Assert.False(config.UseCertificate);
        }
        
        [Fact]
        public void TestFlagdConfigFromUriEnabledCacheDefaultCacheSize()
        {
            CleanEnvVars();
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvVarCache, "LRU");

            var config = new FlagdConfig(new Uri("http://localhost:8013"));

            Assert.True(config.CacheEnabled);
            Assert.Equal(FlagdConfig.CacheSizeDefault, config.MaxCacheSize);
        }
        
        [Fact]
        public void TestFlagdConfigFromUriEnabledCacheApplyCacheSize()
        {
            CleanEnvVars();
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvVarCache, "LRU");
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvVarMaxCacheSize, "20");

            var config = new FlagdConfig(new Uri("http://localhost:8013"));

            Assert.True(config.CacheEnabled);
            Assert.Equal(20, config.MaxCacheSize);
        }

        private void CleanEnvVars()
        {
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvVarTLS, "");
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvVarSocketPath, "");
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvVarCache, "");
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvVarMaxCacheSize, "");
            System.Environment.SetEnvironmentVariable(FlagdConfig.EnvCertPart, "");
        }
    }
}
