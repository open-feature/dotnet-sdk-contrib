using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using OpenFeature.Model;
using OpenFeature.Providers.Flagd.E2e.Common.Utils;
using Reqnroll;

namespace OpenFeature.Providers.Flagd.E2e.Common.Steps;

[Binding]
public class ContextSteps
{
    private readonly State _state;

    public ContextSteps(State state)
    {
        this._state = state;
    }

    [Given(@"^a context containing a key ""([^""]*)"", with type ""([^""]*)"" and with value ""(.*)""$")]
    public void GivenAContextContainingAKeyWithTypeAndWithValue(string key, string type, string value)
    {
        switch (type)
        {
            case "String":
                this._state.EvaluationContextBuilder.Set(key, new Value(value));
                break;

            case "Integer":
                this._state.EvaluationContextBuilder.Set(key, new Value(long.Parse(value)));
                break;

            case "Float":
                this._state.EvaluationContextBuilder.Set(key, new Value(double.Parse(value)));
                break;

            case "Boolean":
                this._state.EvaluationContextBuilder.Set(key, new Value(bool.Parse(value)));
                break;

            case "Object":
                using (var doc = JsonDocument.Parse(value))
                {
                    this._state.EvaluationContextBuilder.Set(key, ConvertJsonElementToValue(doc.RootElement));
                }
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

        this._state.EvaluationContextBuilder.Set(key, new Value(nestedContext));
    }

    [Given("a context containing a targeting key with value {string}")]
    public void GivenAContextContainingATargetingKeyWithValue(string value)
    {
        this._state.EvaluationContextBuilder.SetTargetingKey(value);
    }

    private static Value ConvertJsonElementToValue(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var dict = new Dictionary<string, Value>();
                foreach (var prop in element.EnumerateObject())
                {
                    dict[prop.Name] = ConvertJsonElementToValue(prop.Value);
                }
                return new Value(new Structure(dict));
            case JsonValueKind.Array:
                var list = element.EnumerateArray().Select(ConvertJsonElementToValue).ToList();
                return new Value(list);
            case JsonValueKind.String:
                return new Value(element.GetString()!);
            case JsonValueKind.Number:
                if (element.TryGetInt64(out var l))
                {
                    return new Value(l);
                }
                return new Value(element.GetDouble());
            case JsonValueKind.True:
                return new Value(true);
            case JsonValueKind.False:
                return new Value(false);
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
            default:
                return new Value();
        }
    }
}
