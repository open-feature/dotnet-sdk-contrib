using System;
using System.Threading.Tasks;
using OpenFeature.Constant;
using OpenFeature.Model;
using Reqnroll;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.Test;

public abstract class FlagdStepDefinitionsBase
{
    private readonly ScenarioContext _scenarioContext;
    protected FeatureClient client;
    protected FeatureClient name;
    private Task<bool> booleanZeroValue;
    private Task<string> stringZeroValue;
    private Task<int> intZeroFlagValue;
    private Task<double> doubleZeroFlagValue;
    private string intFlagKey;
    private int intDefaultValue;
    private string stringFlagKey;
    private string stringDefaultValue;
    private bool readyHandlerRan = false;
    private bool changeHandlerRan = false;
    private EvaluationContext evaluationContext;

    public FlagdStepDefinitionsBase(ScenarioContext scenarioContext)
    {
        Skip.If(Environment.GetEnvironmentVariable("E2E") != "true");
        _scenarioContext = scenarioContext;
    }

    [Given(@"a flagd provider is set")]
    public void GivenAFlagdProviderIsSet()
    {
        // done in subclasses
    }

    [When(@"a PROVIDER_READY handler is added")]
    public void WhenAPROVIDER_READYHandlerIsAddedAsync()
    {

        var tcs = new TaskCompletionSource<bool>();
        EventHandlerDelegate handler = (details) =>
        {
            readyHandlerRan = true;
            tcs.SetResult(true);
        };
        client.AddHandler(ProviderEventTypes.ProviderReady, handler);
        tcs.Task.Wait(TimeSpan.FromSeconds(3));
    }

    [Then(@"the PROVIDER_READY handler must run")]
    public void ThenThePROVIDER_READYHandlerMustRun()
    {
        Assert.True(readyHandlerRan);
    }

    [When(@"a PROVIDER_CONFIGURATION_CHANGED handler is added")]
    public void WhenAPROVIDER_CONFIGURATION_CHANGEDHandlerIsAddedAsync()
    {
        var tcs = new TaskCompletionSource<bool>();
        EventHandlerDelegate handler = (details) =>
        {
            changeHandlerRan = true;
            tcs.TrySetResult(true);
        };
        client.AddHandler(ProviderEventTypes.ProviderConfigurationChanged, handler);
        tcs.Task.Wait(TimeSpan.FromSeconds(3));
    }

    [When(@"a flag with key ""(.*)"" is modified")]
    public void WhenAFlagWithKeyIsModified(string _)
    {
        // flags are changed every 1s in the container, we don't need ot do anything here.
    }

    [Then(@"the PROVIDER_CONFIGURATION_CHANGED handler must run")]
    public void ThenThePROVIDER_CONFIGURATION_CHANGEDHandlerMustRun()
    {
        Assert.True(changeHandlerRan);
    }

    [Then(@"the event details must indicate ""(.*)"" was altered")]
    public void ThenTheEventDetailsMustIndicateWasAltered(string p0)
    {
        // flags changed is not yet implemented in process provider
    }

    [When(@"a zero-value boolean flag with key ""(.*)"" is evaluated with default value ""(.*)""")]
    public void WhenAZero_ValueBooleanFlagWithKeyIsEvaluatedWithDefaultValue(string flagKey, string defaultValueString)
    {
        booleanZeroValue = client.GetBooleanValueAsync(flagKey, bool.Parse(defaultValueString));
    }

    [Then(@"the resolved boolean zero-value should be ""(.*)""")]
    public async Task ThenTheResolvedBooleanZero_ValueShouldBe(string expectedValue)
    {
        Assert.Equal(bool.Parse(expectedValue), await booleanZeroValue.ConfigureAwait(false));
    }

    [When(@"a zero-value string flag with key ""(.*)"" is evaluated with default value ""(.*)""")]
    public void WhenAZero_ValueStringFlagWithKeyIsEvaluatedWithDefaultValue(string flagKey, string defaultValueString)
    {
        stringZeroValue = client.GetStringValueAsync(flagKey, defaultValueString);
    }

    [Then(@"the resolved string zero-value should be ""(.*)""")]
    public async Task ThenTheResolvedStringZero_ValueShouldBeAsync(string expectedValue)
    {
        Assert.Equal(expectedValue, await stringZeroValue.ConfigureAwait(false));
    }

    [When(@"a zero-value integer flag with key ""(.*)"" is evaluated with default value (.*)")]
    public void WhenAZero_ValueIntegerFlagWithKeyIsEvaluatedWithDefaultValue(string flagKey, string defaultValueString)
    {
        intZeroFlagValue = client.GetIntegerValueAsync(flagKey, int.Parse(defaultValueString));
    }

    [Then(@"the resolved integer zero-value should be (.*)")]
    public async Task ThenTheResolvedIntegerZero_ValueShouldBeAsync(int expectedValue)
    {
        Assert.Equal(expectedValue, await intZeroFlagValue.ConfigureAwait(false));
    }

    [When(@"a zero-value float flag with key ""(.*)"" is evaluated with default value (.*)")]
    public void WhenAZero_ValueFloatFlagWithKeyIsEvaluatedWithDefaultValue(string flagKey, decimal defaultValue)
    {
        doubleZeroFlagValue = client.GetDoubleValueAsync(flagKey, decimal.ToDouble(defaultValue));
    }

    [Then(@"the resolved float zero-value should be (.*)")]
    public async Task ThenTheResolvedFloatZero_ValueShouldBeAsync(decimal expectedValue)
    {
        Assert.Equal(decimal.ToDouble(expectedValue), await doubleZeroFlagValue.ConfigureAwait(false));
    }

    [When(@"a string flag with key ""(.*)"" is evaluated with default value ""(.*)""")]
    public void WhenAStringFlagWithKeyIsEvaluatedWithDefaultValue(string flagKey, string defaultValue)
    {
        stringFlagKey = flagKey;
        stringDefaultValue = defaultValue;
    }

    [When(@"an integer flag with key ""(.*)"" is evaluated with default value (.*)")]
    public void WhenAnIntegerFlagWithKeyIsEvaluatedWithDefaultValue(string flagKey, int defaultValue)
    {
        intFlagKey = flagKey;
        intDefaultValue = defaultValue;
    }

    [When(@"a context containing a nested property with outer key ""(.*)"" and inner key ""(.*)"", with value ""(.*)""")]
    public void WhenAContextContainingANestedPropertyWithOuterKeyAndInnerKeyWithValue(string outerKey, string innerKey, string innerValue)
    {
        Structure innerStuct = Structure.Builder().Set(innerKey, new Value(innerValue)).Build();
        evaluationContext = EvaluationContext.Builder().Set(outerKey, new Value(innerStuct)).Build();
    }

    [When(@"a context containing a nested property with outer key ""(.*)"" and inner key ""(.*)"", with value (.*)")]
    public void WhenAContextContainingANestedPropertyWithOuterKeyAndInnerKeyWithValue(string outerKey, string innerKey, int innerValue)
    {
        Structure innerStuct = Structure.Builder().Set(innerKey, new Value(innerValue)).Build();
        evaluationContext = EvaluationContext.Builder().Set(outerKey, new Value(innerStuct)).Build();
    }

    [When(@"a context containing a key ""(.*)"", with value ""(.*)""")]
    public void WhenAContextContainingAKeyWithValue(string key, string val)
    {
        evaluationContext = EvaluationContext.Builder().Set(key, new Value(val)).Build();
    }

    [When(@"a context containing a targeting key with value ""(.*)""")]
    public void WhenAContextContainingATargetingKeyWithValue(string targetingKey)
    {
        // TODO: this is a bug - we are not flattening the targetingKey, so it's necessary to set one as well :(
        evaluationContext = EvaluationContext.Builder().SetTargetingKey(targetingKey).Set("targetingKey", targetingKey).Build();
    }

    [When(@"a context containing a key ""(.*)"", with value (.*)")]
    public void WhenAContextContainingAKeyWithValue(string key, long val) // we have to use long here to support timestamps
    {
        evaluationContext = EvaluationContext.Builder().Set(key, new Value(val)).Build();
    }

    [Then(@"the returned value should be ""(.*)""")]
    public async Task ThenTheReturnedValueShouldBe(string expectedValue)
    {
        var details = await client.GetStringDetailsAsync(stringFlagKey, stringDefaultValue, evaluationContext).ConfigureAwait(false);
        Assert.Equal(expectedValue, details.Value);
    }

    [Then(@"the returned value should be (.*)")]
    public async Task ThenTheReturnedValueShouldBeAsync(long expectedValue)
    {
        var details = await client.GetIntegerDetailsAsync(intFlagKey, intDefaultValue, evaluationContext).ConfigureAwait(false);
        Assert.Equal(expectedValue, details.Value);
    }

    [Then(@"the returned reason should be ""(.*)""")]
    public async Task ThenTheReturnedReasonShouldBeAsync(string expectedReason)
    {
        var details = await client.GetStringDetailsAsync(stringFlagKey, stringDefaultValue, evaluationContext).ConfigureAwait(false);
        Assert.Equal(expectedReason, details.Reason);
    }

}
