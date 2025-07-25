using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OpenFeature.Providers.GOFeatureFlag.Helpers;

/// <summary>
///     A class that encapsulates the logic for running an asynchronous function
///     periodically.
/// </summary>
public class PeriodicAsyncRunner
{
    private readonly Func<Task> _action;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly TimeSpan _interval;
    private readonly ILogger _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PeriodicAsyncRunner" /> class.
    /// </summary>
    /// <param name="action">The asynchronous function to execute periodically.</param>
    /// <param name="interval">The time interval between executions.</param>
    /// <param name="logger"></param>
    public PeriodicAsyncRunner(Func<Task> action, TimeSpan interval, ILogger logger)
    {
        this._action = action ?? throw new ArgumentNullException(nameof(action));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger cannot be null");
        this._interval = interval;
        this._cancellationTokenSource = new CancellationTokenSource();
    }

    /// <summary>
    ///     Starts the periodic execution of the async task. This method will run
    ///     indefinitely until the cancellation token is triggered.
    /// </summary>
    /// <returns>A Task representing the long-running operation.</returns>
    public async Task StartAsync()
    {
        // Loop indefinitely until cancellation is requested.
        while (!this._cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                // Wait for the specified interval before the next execution.
                // Passing the cancellation token here ensures that the delay
                // can be interrupted immediately if cancellation is requested.
                await Task.Delay(this._interval, this._cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // This exception is expected when the cancellation token is triggered
                // during the delay. We can safely break the loop.
                break;
            }

            try
            {
                // Execute the asynchronous action and wait for it to complete.
                await this._action().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, $"An error occurred during the periodic task execution: {ex.Message}");
            }
        }
    }

    /// <summary>
    ///     Stops the periodic execution of the async task by cancelling
    /// </summary>
    public Task StopAsync()
    {
        this._cancellationTokenSource.Cancel();
        return Task.CompletedTask;
    }
}
