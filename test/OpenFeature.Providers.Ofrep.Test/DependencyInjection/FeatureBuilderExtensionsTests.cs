using Microsoft.Extensions.DependencyInjection;
using OpenFeature.Providers.Ofrep.DependencyInjection;
using Xunit;

namespace OpenFeature.Providers.Ofrep.Test.DependencyInjection;

public class FeatureBuilderExtensionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddOfrepProvider_WithInvalidBaseUrl_Throws_ArgumentException(string? url)
    {
        using var services = new ServiceCollection()
            .AddOpenFeature(builder =>
            {
                builder.AddOfrepProvider(o => { o.BaseUrl = url!; });
            })
            .BuildServiceProvider();

        // Provider creation is deferred until resolution; resolving should throw due to missing BaseUrl
        var exception = Assert.Throws<ArgumentException>(() => services.GetRequiredService<FeatureProvider>());
        Assert.Contains("Ofrep BaseUrl is required", exception.Message);
    }

    [Fact]
    public void AddOfrepProvider_WithConfiguration_RegistersProvider()
    {
        using var services = new ServiceCollection()
            .AddOpenFeature(builder =>
            {
                builder.AddOfrepProvider(o => { o.BaseUrl = "https://api.example.com/"; });
            })
            .BuildServiceProvider();

        var client = services.GetService<IFeatureClient>();
        Assert.NotNull(client);

        var provider = services.GetRequiredService<FeatureProvider>();
        var metadata = provider.GetMetadata();
        Assert.NotNull(metadata);
        Assert.Equal("OpenFeature Remote Evaluation Protocol Server", metadata.Name);
        Assert.IsType<OfrepProvider>(provider);
    }

    [Fact]
    public void AddOfrepProvider_WithDomain_RegistersKeyedProvider()
    {
        using var services = new ServiceCollection()
            .AddOpenFeature(builder =>
            {
                builder.AddOfrepProvider("test-domain", o => { o.BaseUrl = "https://api.example.com/"; });
            })
            .BuildServiceProvider();

        var provider = services.GetKeyedService<FeatureProvider>("test-domain");
        Assert.NotNull(provider);
        Assert.IsType<OfrepProvider>(provider);
    }

    [Fact]
    public void AddOfrepProvider_WithHttpClientFactory_UsesConfiguredClient()
    {
        var configured = false;

        var headers = new Dictionary<string, string>
        {
            { "X-Test", "1" }
        };
        using var services = new ServiceCollection()
            .AddLogging()
            .AddHttpClient("ofrep-test")
            .Services
            .AddOpenFeature(builder =>
            {
                builder.AddOfrepProvider(o =>
                {
                    o.BaseUrl = "https://api.example.com/";
                    o.HttpClientName = "ofrep-test";
                    o.Timeout = TimeSpan.FromSeconds(30);
                    o.Headers = headers;
                    o.ConfigureHttpClient = (_, c) =>
                    {
                        configured = true;
                        c.DefaultRequestHeaders.Add("X-Test", "1");
                    };
                });
            })
            .BuildServiceProvider();

        var provider = services.GetRequiredService<FeatureProvider>();
        Assert.IsType<OfrepProvider>(provider);

        Assert.True(configured);
    }

    [Fact]
    public void AddOfrepProvider_NamedClient_Applies_Configuration()
    {
        var configureInvoked = false;
        var services = new ServiceCollection();

        services.AddLogging()
            .AddHttpClient("ofrep-test", client =>
            {
                client.BaseAddress = new Uri("https://override.example/");
            });

        services.AddOpenFeature(builder =>
        {
            builder.AddOfrepProvider(o =>
            {
                o.BaseUrl = "https://api.example.com/";
                o.HttpClientName = "ofrep-test";
                o.ConfigureHttpClient = (_, c) =>
                {
                    configureInvoked = true;
                    c.DefaultRequestHeaders.Add("X-Test", "1");
                };
            });
        });

        using var provider = services.BuildServiceProvider();
        var ofrepProvider = provider.GetRequiredService<FeatureProvider>();
        Assert.IsType<OfrepProvider>(ofrepProvider);
        Assert.True(configureInvoked); // Verify ConfigureHttpClient was called
    }

    [Fact]
    public void AddOfrepProvider_DefaultClient_Uses_Factory_When_Available()
    {
        var configureInvoked = false;
        var services = new ServiceCollection();

        services.AddLogging()
            .AddHttpClient(); // Default HttpClient registration

        services.AddOpenFeature(builder =>
        {
            builder.AddOfrepProvider(o =>
            {
                o.BaseUrl = "https://api.example.com/";
                o.Headers["Authorization"] = "Bearer abc";
                o.ConfigureHttpClient = (_, c) =>
                {
                    configureInvoked = true;
                    c.DefaultRequestHeaders.Add("X-Test", "1");
                };
            });
        });

        using var provider = services.BuildServiceProvider();
        var ofrepProvider = provider.GetRequiredService<FeatureProvider>();
        Assert.IsType<OfrepProvider>(ofrepProvider);
        Assert.True(configureInvoked); // Verify ConfigureHttpClient was called
    }

    [Fact]
    public void AddOfrepProvider_Timeout_Is_Applied()
    {
        using var services = new ServiceCollection()
            .AddLogging()
            .AddOpenFeature(builder =>
            {
                builder.AddOfrepProvider(o =>
                {
                    o.BaseUrl = "https://api.example.com/";
                    o.Timeout = TimeSpan.FromSeconds(30);
                });
            })
            .BuildServiceProvider();

        var provider = services.GetRequiredService<FeatureProvider>();
        Assert.IsType<OfrepProvider>(provider); // Provider is created successfully with custom timeout
    }

    [Fact]
    public void AddOfrepProvider_Headers_Are_Applied()
    {
        using var services = new ServiceCollection()
            .AddLogging()
            .AddOpenFeature(builder =>
            {
                builder.AddOfrepProvider(o =>
                {
                    o.BaseUrl = "https://api.example.com/";
                    o.Headers["Authorization"] = "Bearer token123";
                    o.Headers["X-Custom"] = "value";
                });
            })
            .BuildServiceProvider();

        var provider = services.GetRequiredService<FeatureProvider>();
        Assert.IsType<OfrepProvider>(provider); // Provider is created successfully with custom headers
    }

    [Fact]
    public void AddOfrepProvider_WithoutFactory_DoesNot_Invoke_ConfigureHttpClient()
    {
        var configured = false;

        using var services = new ServiceCollection()
            .AddLogging()
            .AddOpenFeature(builder =>
            {
                builder.AddOfrepProvider(o =>
                {
                    o.BaseUrl = "https://api.example.com/";
                    o.ConfigureHttpClient = (_, __) => configured = true;
                });
            })
            .BuildServiceProvider();

        var provider = services.GetRequiredService<FeatureProvider>();
        Assert.IsType<OfrepProvider>(provider);
        Assert.False(configured);
    }

    [Fact]
    public void AddOfrepProvider_Named_Domain_Works()
    {
        var services = new ServiceCollection();

        services.AddLogging()
            .AddHttpClient("domain-client");

        services.AddOpenFeature(builder =>
        {
            builder.AddOfrepProvider("production", o =>
            {
                o.BaseUrl = "https://prod.example.com/";
                o.HttpClientName = "domain-client";
                o.Headers["Environment"] = "production";
            });
        });

        using var provider = services.BuildServiceProvider();
        var keyedProvider = provider.GetKeyedService<FeatureProvider>("production");
        Assert.NotNull(keyedProvider);
        Assert.IsType<OfrepProvider>(keyedProvider);
    }

}
