using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.Common;

public class FlagdTestBedContainer
{
    public IContainer Container { get; }

    public FlagdTestBedContainer(string version)
    {
        Container = new ContainerBuilder()
            .WithImage($"ghcr.io/open-feature/flagd-testbed:v{version}")
            .WithPortBinding(8015, true)
            .WithPortBinding(8013, true)
            .Build();
    }
}
