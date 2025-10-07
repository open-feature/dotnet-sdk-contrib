using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using Reqnroll.Microsoft.Extensions.DependencyInjection;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.Common;

[Binding]
public class Setup
{
    [ScenarioDependencies]
    public static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var context = new TestContext();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(context);

        return services;
    }
}

public class TestContext
{
    public ResolverType ProviderResolverType { get; set; }
}
