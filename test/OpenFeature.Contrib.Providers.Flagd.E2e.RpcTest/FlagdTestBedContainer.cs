using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.RpcTest
{
    public class FlagdTestBedContainer
    {
        public IContainer Container { get; }

        public FlagdTestBedContainer()
        {
            Container = new ContainerBuilder()
                .WithImage("ghcr.io/open-feature/flagd-testbed:v0.5.21")
                .WithExposedPort(8013)
                .WithPortBinding(8013, true)
                .Build();
        }
    }
}