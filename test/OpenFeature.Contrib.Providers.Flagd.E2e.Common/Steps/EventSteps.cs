using System;
using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Constant;
using OpenFeature.Contrib.Providers.Flagd.E2e.Common.Utils;
using Reqnroll;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.Common.Steps;

[Binding]
public class EventSteps
{
    private const int DefaultEventFiredTimeoutMs = 12_000;

    private readonly State _state;

    public EventSteps(State state)
    {
        this._state = state;
    }

    [Given(@"a {} event handler")]
    public void GivenAEventHandler(ProviderEventTypes eventType)
    {
        this._state.Api.AddHandler(eventType, (payload) =>
        {
            this._state.Events.Add(new Event(eventType, payload));
        });
    }

    [When("a {} event was fired")]
    public async Task WhenAnEventWasFired(ProviderEventTypes eventType)
    {
        await this.WaitForEventToBeHandledAsync(eventType, DefaultEventFiredTimeoutMs);
    }

    [Then("the {} event handler should have been executed within {int}ms")]
    public async Task ThenTheEventHandlerShouldHaveBeenExecutedWithinMs(ProviderEventTypes eventType, int timeoutMs)
    {
        await this.WaitForEventToBeHandledAsync(eventType, timeoutMs);
    }

    [StepArgumentTransformation(@"^(ready|stale|change)$")]
    public static ProviderEventTypes TransformProviderEventType(string raw)
        => raw switch
        {
            "ready" => ProviderEventTypes.ProviderReady,
            "stale" => ProviderEventTypes.ProviderStale,
            "change" => ProviderEventTypes.ProviderConfigurationChanged,
            _ => throw new Exception($"Unsupported ProviderEventType '{raw}'")
        };

    private async Task WaitForEventToBeHandledAsync(ProviderEventTypes eventType, int timeoutMs)
    {
        Skip.If(eventType == ProviderEventTypes.ProviderStale,
            "Stale event is not supported for .NET flagd provider yet.");

        using var cancellationTokenSource = new CancellationTokenSource(timeoutMs);
        while (!cancellationTokenSource.IsCancellationRequested)
        {
            if (this._state.Events.Exists(e => e.EventType == eventType))
            {
                return;
            }

            try
            {
                await Task.Delay(100, cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // This is expected when the timeout is reached.
                // The loop condition will handle exiting.
            }
        }

        Assert.Fail("Timeout waiting for event to be fired");
    }
}
