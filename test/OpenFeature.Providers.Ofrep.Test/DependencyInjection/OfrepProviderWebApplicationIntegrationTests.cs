// This file is NOT tested in dotnet framework because it uses Microsoft.AspNetCore.Builder, which is not available in .NET 6.0 and later.

#if !NETFRAMEWORK
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using OpenFeature.Providers.Ofrep.DependencyInjection;
using Xunit;

namespace OpenFeature.Providers.Ofrep.Test.DependencyInjection;

public class OfrepProviderWebApplicationIntegrationTests
{
    [Fact]
    public async Task OfrepProvider_Integration_WithTestServer_CanEvaluateFeatureFlag()
    {
        const string httpClientName = "Test";
        const string flagKey = "test-flag";

        // Arrange - Create mock OFREP server first
        await using var mockServer = await CreateMockOfrepServer();

        // Create the main application with TestServer
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        // Replace the entire HttpClientFactory
        var handler = mockServer.TestServer.CreateHandler();
        var baseUrl = mockServer.BaseUrl;

        builder.Services.AddHttpClient(httpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        builder.Services.AddOpenFeature(openFeatureBuilder =>
        {
            openFeatureBuilder
                .AddHostedFeatureLifecycle()
                .AddOfrepProvider(c =>
                {
                    c.BaseUrl = baseUrl;
                    c.HttpClientName = httpClientName;
                });
        });

        await using var app = builder.Build();
        await app.StartAsync();

        // Create a scope to resolve scoped services like IFeatureClient
        using var scope = app.Services.CreateScope();
        var featureClient = scope.ServiceProvider.GetRequiredService<IFeatureClient>();

        // Act - Try to evaluate a flag
        var result = await featureClient.GetBooleanDetailsAsync(flagKey, false);

        // Assert
        Assert.NotNull(featureClient);
        Assert.True(result.Value);
        Assert.Equal("STATIC", result.Reason);
        Assert.Equal(flagKey, result.FlagKey);
    }

    private static async Task<MockOfrepServer> CreateMockOfrepServer()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.AddConsole();

        var app = builder.Build();

        // Configure mock OFREP endpoints
        // Mock OFREP evaluate endpoint for individual flags
        app.MapPost("/ofrep/v1/evaluate/flags/{flagKey}", async (string flagKey, HttpContext context) =>
        {
            // Log the request for debugging
            app.Logger.LogInformation("OFREP evaluate request for flag: {FlagKey}", flagKey);

            // Mock evaluation response based on OFREP specification
            var response = new
            {
                key = flagKey,
                reason = "STATIC",
                variant = "on",
                value = true,
                metadata = new { }
            };

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        });

        // Add a catch-all endpoint to log unexpected requests
        app.MapFallback(async context =>
        {
            app.Logger.LogWarning("Unmatched request: {Method} {Path}", context.Request.Method, context.Request.Path);
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("Not Found");
        });

        await app.StartAsync();

        var testServer = app.Services.GetRequiredService<IServer>() as TestServer
                         ?? throw new InvalidOperationException("TestServer not found");
        // Use the TestServer's BaseAddress directly - it should be something like http://localhost:random-port
        var baseUrl = testServer.BaseAddress.ToString().TrimEnd('/');

        return new MockOfrepServer(app, baseUrl, testServer);
    }

    private sealed class MockOfrepServer(WebApplication app, string baseUrl, TestServer testServer) : IAsyncDisposable
    {
        public string BaseUrl { get; } = baseUrl;
        public TestServer TestServer { get; } = testServer;

        public async ValueTask DisposeAsync()
        {
            await app.DisposeAsync();
        }
    }
}
#endif
