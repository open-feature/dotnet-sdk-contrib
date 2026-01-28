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
        var contextBuilder = this._state.EvaluationContextBuilder
            ?? EvaluationContext.Builder();
        var context = contextBuilder.Build();

        switch (flag.Type)
        {
            case FlagType.Boolean:
                this._state.FlagEvaluationDetailsResult = await this._state.Client!
                    .GetBooleanDetailsAsync(flag.Key, bool.Parse(flag.DefaultValue), context)
                    .ConfigureAwait(false);
                break;
            case FlagType.Float:
                this._state.FlagEvaluationDetailsResult = await this._state.Client!
                    .GetDoubleDetailsAsync(flag.Key, double.Parse(flag.DefaultValue), context)
                    .ConfigureAwait(false);
                break;
            case FlagType.Integer:
                this._state.FlagEvaluationDetailsResult = await this._state.Client!
                    .GetIntegerDetailsAsync(flag.Key, int.Parse(flag.DefaultValue), context)
                    .ConfigureAwait(false);
                break;
            case FlagType.String:
                this._state.FlagEvaluationDetailsResult = await this._state.Client!
                    .GetStringDetailsAsync(flag.Key, flag.DefaultValue, context)
                    .ConfigureAwait(false);
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

    [Then("the resolved metadata should contain")]
    public void ThenTheResolvedMetadataShouldContain(DataTable dataTable)
    {
        foreach (var row in dataTable.Rows)
        {
            var key = row["key"];
            var type = row["metadata_type"];
            var value = row["value"];

            switch (this._state.Flag!.Type)
            {
                case FlagType.Integer:
                    this.AssertMetadata<int>(key, type, value);
                    break;
                case FlagType.Float:
                    this.AssertMetadata<double>(key, type, value);
                    break;
                case FlagType.String:
                    this.AssertMetadata<string>(key, type, value);
                    break;
                case FlagType.Boolean:
                    this.AssertMetadata<bool>(key, type, value);
                    break;
                default:
                    Assert.Fail("FlagType not yet supported.");
                    break;
            }
        }
    }

    [Then("the resolved metadata is empty")]
    public void ThenTheResolvedMetadataIsEmpty()
    {
        switch (this._state.Flag!.Type)
        {
            case FlagType.Integer:
                this.AssertEmptyMetadata<int>();
                break;
            case FlagType.Float:
                this.AssertEmptyMetadata<double>();
                break;
            case FlagType.String:
                this.AssertEmptyMetadata<string>();
                break;
            case FlagType.Boolean:
                this.AssertEmptyMetadata<bool>();
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

    private void AssertEmptyMetadata<T>()
    {
        var details = this._state.FlagEvaluationDetailsResult as FlagEvaluationDetails<T>;

        Assert.NotNull(details);
        Assert.NotNull(details.FlagMetadata);

        var count = typeof(ImmutableMetadata)
            .GetProperty("Count", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic)
            .GetValue(details.FlagMetadata) as int?;

        Assert.NotNull(count);
        Assert.Equal(0, count);
    }

    private void AssertMetadata<T>(string key, string type, string value)
    {
        var details = this._state.FlagEvaluationDetailsResult as FlagEvaluationDetails<T>;

        Assert.NotNull(details);
        Assert.NotNull(details.FlagMetadata);

        switch (type)
        {
            case "Boolean":
                {
                    var expectedValue = bool.Parse(value);
                    var actualValue = details.FlagMetadata.GetBool(key);
                    Assert.NotNull(actualValue);
                    Assert.Equal(expectedValue, actualValue);
                    break;
                }
            case "String":
                {
                    var expectedValue = value;
                    var actualValue = details.FlagMetadata.GetString(key);
                    Assert.NotNull(actualValue);
                    Assert.Equal(expectedValue, actualValue);
                    break;
                }
            case "Integer":
                {
                    var expectedValue = int.Parse(value);
                    var actualValue = details.FlagMetadata.GetInt(key);
                    Assert.NotNull(actualValue);
                    Assert.Equal(expectedValue, actualValue);
                    break;
                }
            case "Float":
                {
                    var expectedValue = double.Parse(value);
                    var actualValue = details.FlagMetadata.GetDouble(key);
                    Assert.NotNull(actualValue);
                    Assert.Equal(expectedValue, actualValue);
                    break;
                }
            default:
                {
                    Assert.Fail($"Metadata type '{type}' not supported.");
                    break;
                }
        }
    }
}
