
using Xunit;
using OpenFeatureContrib.Providers.Flagd;

namespace OpenFeatureContrib.Providers.Flagd.Test
{
    public class UnitTest1
    {
        [Fact]
        public void TestMethod1()
        {
            Assert.Equal("No-op Provider", Stub.GetProviderName());
        }
    }
}
