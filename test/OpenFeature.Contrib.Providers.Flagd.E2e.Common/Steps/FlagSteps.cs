using System;
using System.Threading.Tasks;
using OpenFeature.Constant;
using OpenFeature.Contrib.Providers.Flagd.E2e.Common.Utils;
using OpenFeature.Model;
using Reqnroll;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.Common.Steps;

[Binding]
public class FlagSteps
{
    private readonly State _state;

    public FlagSteps(State state)
    {
        this._state = state;
    }

    [Given(@"a (Boolean|Float|Integer|String)(?:-flag)? with key ""(.*)"" and a default value ""(.*)""")]
    public void GivenAFlagType_FlagWithKeyAndADefaultValue(FlagType flagType, string key, string defaultType)
    {
        var flagState = new FlagState(key, defaultType, flagType);
        this._state.Flag = flagState;
    }

    [StepArgumentTransformation(@"^(Boolean|Float|Integer|String)(?:-flag)?$")]
    public static FlagType TransformFlagType(string raw)
        => raw.Replace("-flag", "").ToLowerInvariant() switch
        {
            "boolean" => FlagType.Boolean,
            "float" => FlagType.Float,
            "integer" => FlagType.Integer,
            "string" => FlagType.String,
            _ => throw new Exception($"Unsupported flag type '{raw}'")
        };

    [When("the flag was evaluated with details")]
    public async Task WhenTheFlagWasEvaluatedWithDetails()
    {
        var flag = this._state.Flag!;

        switch (flag.Type)
        {
            case FlagType.Boolean:
                this._state.FlagEvaluationDetailsResult = await this._state.Client!
                    .GetBooleanDetailsAsync(flag.Key, bool.Parse(flag.DefaultValue), this._state.EvaluationContext);
                break;
            case FlagType.Float:
                this._state.FlagEvaluationDetailsResult = await this._state.Client!
                    .GetDoubleDetailsAsync(flag.Key, double.Parse(flag.DefaultValue), this._state.EvaluationContext);
                break;
            case FlagType.Integer:
                this._state.FlagEvaluationDetailsResult = await this._state.Client!
                    .GetIntegerDetailsAsync(flag.Key, int.Parse(flag.DefaultValue), this._state.EvaluationContext);
                break;
            case FlagType.String:
                this._state.FlagEvaluationDetailsResult = await this._state.Client!
                    .GetStringDetailsAsync(flag.Key, flag.DefaultValue, this._state.EvaluationContext);
                break;
        }
    }

    [Then("the resolved details value should be {string}")]
    public void ThenTheResolvedDetailsValueShouldBe(string value)
    {
        switch (this._state.Flag!.Type)
        {
            case FlagType.Integer:
                var intValue = int.Parse(value);
                this.AssertOnDetails<int>(r => Assert.Equal(intValue, r.Value));
                break;
            case FlagType.Float:
                var floatValue = double.Parse(value);
                this.AssertOnDetails<double>(r => Assert.Equal(floatValue, r.Value));
                break;
            case FlagType.String:
                var stringValue = value;
                this.AssertOnDetails<string>(r => Assert.Equal(stringValue, r.Value));
                break;
            case FlagType.Boolean:
                var booleanValue = bool.Parse(value);
                this.AssertOnDetails<bool>(r => Assert.Equal(booleanValue, r.Value));
                break;
            default:
                Assert.Fail("FlagType not yet supported.");
                break;
        }
    }

    [Then("the reason should be {string}")]
    public void ThenTheReasonShouldBe(string reason)
    {
        switch (this._state.Flag!.Type)
        {
            case FlagType.Integer:
                this.AssertOnDetails<int>(r => Assert.Equal(reason, r.Reason));
                break;
            case FlagType.Float:
                this.AssertOnDetails<double>(r => Assert.Equal(reason, r.Reason));
                break;
            case FlagType.String:
                this.AssertOnDetails<string>(r => Assert.Equal(reason, r.Reason));
                break;
            case FlagType.Boolean:
                this.AssertOnDetails<bool>(r => Assert.Equal(reason, r.Reason));
                break;
            default:
                Assert.Fail("FlagType not yet supported.");
                break;
        }
    }

    [Then("the error-code should be {string}")]
    public void ThenTheError_CodeShouldBe(string error)
    {
        ErrorType errorType = ErrorType.None;
        if (!string.IsNullOrEmpty(error))
        {
            errorType = EnumHelpers.ParseFromDescription<ErrorType>(error);
        }

        switch (this._state.Flag!.Type)
        {
            case FlagType.Integer:
                this.AssertOnDetails<int>(r => Assert.Equal(errorType, r.ErrorType));
                break;
            case FlagType.Float:
                this.AssertOnDetails<double>(r => Assert.Equal(errorType, r.ErrorType));
                break;
            case FlagType.String:
                this.AssertOnDetails<string>(r => Assert.Equal(errorType, r.ErrorType));
                break;
            case FlagType.Boolean:
                this.AssertOnDetails<bool>(r => Assert.Equal(errorType, r.ErrorType));
                break;
            default:
                Assert.Fail("FlagType not yet supported.");
                break;
        }
    }

    private void AssertOnDetails<T>(Action<FlagEvaluationDetails<T>> assertion)
    {
        var details = this._state.FlagEvaluationDetailsResult as FlagEvaluationDetails<T>;

        Assert.NotNull(details);
        assertion(details);
    }
}
