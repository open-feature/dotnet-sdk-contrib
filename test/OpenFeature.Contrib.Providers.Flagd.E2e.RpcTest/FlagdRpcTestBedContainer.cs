using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.RpcTest;

public class FlagdRpcTestBedContainer
{
    public IContainer Container { get; }

    public FlagdRpcTestBedContainer(string version)
    {
        Container = new ContainerBuilder()
            .WithImage($"ghcr.io/open-feature/flagd-testbed:v{version}")
            .WithPortBinding(8013, true)
            .Build();
    }
}
