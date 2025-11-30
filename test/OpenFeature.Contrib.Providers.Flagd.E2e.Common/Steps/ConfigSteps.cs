using OpenFeature.Contrib.Providers.Flagd.E2e.Common.Utils;
using Reqnroll;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.Common.Steps;

[Binding]
public class ConfigSteps
{
    private readonly State _state;

    public ConfigSteps(State state)
    {
        this._state = state;
    }

    [Given("an option {string} of type {string} with value {string}")]
    public void GivenAnOptionOfTypeWithValue(string cache, string cacheType, string disabled)
    {
    }
}
