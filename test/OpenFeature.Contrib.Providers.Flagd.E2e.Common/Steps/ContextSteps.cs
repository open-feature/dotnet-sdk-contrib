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
        this._state.EvaluationContext ??= EvaluationContext.Builder();

        switch (type)
        {
            case "String":
                this._state.EvaluationContext.Set(key, new Value(value));
                break;

            case "Integer":
                this._state.EvaluationContext.Set(key, new Value(long.Parse(value)));
                break;

            default:
                break;
        }
    }

    [Given("a context containing a nested property with outer key {string} and inner key {string}, with value {string}")]
    public void GivenAContextContainingANestedPropertyWithOuterKeyAndInnerKeyWithValue(string key, string innerKey, string value)
    {
        var nestedContext = Structure.Builder()
            .Set(innerKey, new Value(value))
            .Build();

        this._state.EvaluationContext ??= EvaluationContext.Builder();

        this._state.EvaluationContext.Set(key, new Value(nestedContext));
    }

    [Given("a context containing a targeting key with value {string}")]
    public void GivenAContextContainingATargetingKeyWithValue(string value)
    {
        this._state.EvaluationContext ??= EvaluationContext.Builder();

        this._state.EvaluationContext.SetTargetingKey(value);
    }
}
