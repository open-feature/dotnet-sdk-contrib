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
            System.Environment.SetEnvironmentVariable("FLAGD_TLS", "true");

            var config = new FlagdConfig();

            Assert.Equal(new System.Uri("https://localhost:8013"), config.GetUri());
        }

        [Fact]
        public void TestFlagdConfigUnixSocket()
        {
            CleanEnvVars();
            System.Environment.SetEnvironmentVariable("FLAGD_SOCKET_PATH", "tmp.sock");

            var config = new FlagdConfig();

            Assert.Equal(new System.Uri("unix://tmp.sock/"), config.GetUri());
        }

        [Fact]
        public void TestFlagdConfigEnabledCacheDefaultCacheSize()
        {
            CleanEnvVars();
            System.Environment.SetEnvironmentVariable("FLAGD_CACHE", "LRU");

            var config = new FlagdConfig();

            Assert.True(config.CacheEnabled);
            Assert.Equal(FlagdConfig.CacheSizeDefault, config.MaxCacheSize);
        }

        [Fact]
        public void TestFlagdConfigEnabledCacheApplyCacheSize()
        {
            CleanEnvVars();
            System.Environment.SetEnvironmentVariable("FLAGD_CACHE", "LRU");
            System.Environment.SetEnvironmentVariable("FLAGD_MAX_CACHE_SIZE", "20");

            var config = new FlagdConfig();

            Assert.True(config.CacheEnabled);
            Assert.Equal(20, config.MaxCacheSize);
        }

        private void CleanEnvVars()
        {
            System.Environment.SetEnvironmentVariable("FLAGD_TLS", "");
            System.Environment.SetEnvironmentVariable("FLAGD_SOCKET_PATH", "");
            System.Environment.SetEnvironmentVariable("FLAGD_CACHE", "");
            System.Environment.SetEnvironmentVariable("FLAGD_MAX_CACHE_SIZE", "");
        }
    }
}