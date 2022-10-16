
using Xunit;
using OpenFeature.Contrib.Providers.Flagd;

namespace OpenFeature.Contrib.Providers.Flagd.Test
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
