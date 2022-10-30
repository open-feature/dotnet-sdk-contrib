using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.Test
{
    public class UnitTest1
    {
        [Fact]
        public void TestMethod1()
        {
            Assert.Equal("No-op Provider", FlagdProvider.GetProviderName());
        }
    }
}
