using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.ProcessTest
{
    public class FlagdSyncTestBedContainer
    {
        public IContainer Container { get; }

        public FlagdSyncTestBedContainer()
        {
            Container = new ContainerBuilder()
                .WithImage("ghcr.io/open-feature/sync-testbed:v0.5.6")
                .WithExposedPort(9090)
                .WithPortBinding(9090, true)
                .Build();
        }
    }
}