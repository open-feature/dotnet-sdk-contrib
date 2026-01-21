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
    public void GivenAnOptionOfTypeWithValue(string option, string type, string value)
    {
        if (this._state.FlagdConfig == null)
        {
            this._state.FlagdConfig = FlagdConfig.Builder();
        }

        if (option == "cache")
        {
            var enabled = value == "enabled";
            this._state.FlagdConfig = this._state.FlagdConfig.WithCache(enabled);
        }
        else if (option == "selector")
        {
            this._state.FlagdConfig = this._state.FlagdConfig.WithSourceSelector(value);
        }
    }
}
