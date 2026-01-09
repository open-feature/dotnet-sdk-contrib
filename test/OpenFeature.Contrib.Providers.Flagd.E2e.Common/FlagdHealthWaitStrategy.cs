using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.Common;

/// <summary>
/// Inspired from the healthcheck in the flagd-testbed docker-compose.yaml file for the flagd container.
/// </summary>
internal sealed class FlagdHealthWaitStrategy : IWaitUntil
{
    private readonly int _containerPort = 8014;
    private readonly string _path = "/healthz";
    private readonly TimeSpan _overallTimeout = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(10);
    private readonly TimeSpan _perRequestTimeout = TimeSpan.FromSeconds(5);

    public async Task<bool> UntilAsync(IContainer container)
    {
        using var overallCts = new CancellationTokenSource(this._overallTimeout);

        var host = container.Hostname;
        var mappedPort = container.GetMappedPublicPort(this._containerPort);
        var uri = new UriBuilder(Uri.UriSchemeHttp, host, mappedPort, this._path);

        using var httpClient = new HttpClient();

        while (!overallCts.IsCancellationRequested)
        {
            try
            {
                using var perRequestCts = CancellationTokenSource.CreateLinkedTokenSource(overallCts.Token);
                perRequestCts.CancelAfter(this._perRequestTimeout);

                using var response = await httpClient.GetAsync(uri.Uri, perRequestCts.Token).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch (OperationCanceledException) when (!overallCts.IsCancellationRequested)
            {
                // Per-request timeout: continue retrying
            }
            catch
            {
                // Any transient failure: continue retrying
            }

            try
            {
                await Task.Delay(this._pollInterval, overallCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        return false;
    }
}
