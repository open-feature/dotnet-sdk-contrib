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
                .WithImage("ghcr.io/open-feature/flagd-testbed:v0.5.21")
                .WithPortBinding(8015, true)
                .Build();
        }
    }
}