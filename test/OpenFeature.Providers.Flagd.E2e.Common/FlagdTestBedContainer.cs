using System.IO;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace OpenFeature.Providers.Flagd.E2e.Common;

public class FlagdTestBedContainer
{
    public IContainer Container { get; }

    public FlagdTestBedContainer(string version)
    {
        Container = new ContainerBuilder($"ghcr.io/open-feature/flagd-testbed:v{version}")
            .WithPortBinding(8013, true)
            .WithPortBinding(8014, true)
            .WithPortBinding(8015, true)
            .WithPortBinding(8016, true)
            .WithPortBinding(8080, true)
            .WithResourceMapping(new DirectoryInfo("./flags"), "/flags")
            // drop these two mappings once version.txt points at a release that bundles resolve
            .WithResourceMapping(new FileInfo("./flags/resolve-flags.json"), "/rawflags/")
            .WithResourceMapping(new FileInfo("./launchpad-configs/resolve.json"), "/configs/")
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .AddCustomWaitStrategy(new FlagdHealthWaitStrategy())
            )
            .Build();
    }
}
