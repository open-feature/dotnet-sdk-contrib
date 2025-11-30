using OpenFeature.Contrib.Providers.Flagd.E2e.Common.Utils;
using OpenFeature.Model;
using Reqnroll;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.Common.Steps;

[Binding]
public class ContextSteps
{
    private readonly State _state;

    public ContextSteps(State state)
    {
        this._state = state;
    }

    [Given("a context containing a key {string}, with type {string} and with value {string}")]
    public void GivenAContextContainingAKeyWithTypeAndWithValue(string key, string type, string value)
    {
        var context = EvaluationContext.Builder();

        switch (type)
        {
            case "String":
                context.Set(key, value);
                break;
            default:
                break;
        }

        this._state.EvaluationContext = context.Build();
    }
}
