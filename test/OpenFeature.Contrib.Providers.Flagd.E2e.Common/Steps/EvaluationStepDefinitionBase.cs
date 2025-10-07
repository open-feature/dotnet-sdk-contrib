using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using OpenFeature.Constant;
using OpenFeature.Contrib.Providers.Flagd.E2e.Common.Steps;
using OpenFeature.Model;
using Reqnroll;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.E2e.Common;

[Binding]
[Scope(Feature = "Flag evaluation")]
public class EvaluationStepDefinitionsBase : BaseSteps
{
    private FeatureClient client;
    private bool booleanFlagValue;
    private string stringFlagValue;
    private int intFlagValue;
    private double doubleFlagValue;
    private Value objectFlagValue;
    private FlagEvaluationDetails<bool> booleanFlagDetails;
    private FlagEvaluationDetails<string> stringFlagDetails;
    private FlagEvaluationDetails<int> intFlagDetails;
    private FlagEvaluationDetails<double> doubleFlagDetails;
    private FlagEvaluationDetails<Value> objectFlagDetails;
    private string contextAwareFlagKey;
    private string contextAwareDefaultValue;
    private string contextAwareValue;
    private EvaluationContext context;
    private string notFoundFlagKey;
    private string notFoundDefaultValue;
    private FlagEvaluationDetails<string> notFoundDetails;
    private string typeErrorFlagKey;
    private int typeErrorDefaultValue;
    private FlagEvaluationDetails<int> typeErrorDetails;

    public EvaluationStepDefinitionsBase(TestContext testContext)
        : base(testContext)
    {
    }

    [Given(@"a stable provider")]
    public async Task Givenastableprovider()
    {
        this.client = await this.CreateFeatureClientAsync().ConfigureAwait(false);
    }

    [When(@"a boolean flag with key ""(.*)"" is evaluated with default value ""(.*)""")]
    public async Task Whenabooleanflagwithkeyisevaluatedwithdefaultvalue(string flagKey, bool defaultValue)
    {
        this.booleanFlagValue = await client.GetBooleanValueAsync(flagKey, defaultValue).ConfigureAwait(false);
    }

    [Then(@"the resolved boolean value should be ""(.*)""")]
    public void Thentheresolvedbooleanvalueshouldbe(bool expectedValue)
    {
        Assert.Equal(expectedValue, this.booleanFlagValue);
    }

    [When(@"a string flag with key ""(.*)"" is evaluated with default value ""(.*)""")]
    public async Task Whenastringflagwithkeyisevaluatedwithdefaultvalue(string flagKey, string defaultValue)
    {
        this.stringFlagValue = await client.GetStringValueAsync(flagKey, defaultValue).ConfigureAwait(false);
    }

    [Then(@"the resolved string value should be ""(.*)""")]
    public void Thentheresolvedstringvalueshouldbe(string expected)
    {
        Assert.Equal(expected, this.stringFlagValue);
    }

    [When(@"an integer flag with key ""(.*)"" is evaluated with default value (.*)")]
    public async Task Whenanintegerflagwithkeyisevaluatedwithdefaultvalue(string flagKey, int defaultValue)
    {
        this.intFlagValue = await client.GetIntegerValueAsync(flagKey, defaultValue).ConfigureAwait(false);
    }

    [Then(@"the resolved integer value should be (.*)")]
    public void Thentheresolvedintegervalueshouldbe(int expected)
    {
        Assert.Equal(expected, this.intFlagValue);
    }

    [When(@"a float flag with key ""(.*)"" is evaluated with default value (.*)")]
    public async Task Whenafloatflagwithkeyisevaluatedwithdefaultvalue(string flagKey, double defaultValue)
    {
        this.doubleFlagValue = await client.GetDoubleValueAsync(flagKey, defaultValue).ConfigureAwait(false);
    }

    [Then(@"the resolved float value should be (.*)")]
    public void Thentheresolvedfloatvalueshouldbe(double expected)
    {
        Assert.Equal(expected, this.doubleFlagValue);
    }

    [When(@"an object flag with key ""(.*)"" is evaluated with a null default value")]
    public async Task Whenanobjectflagwithkeyisevaluatedwithanulldefaultvalue(string flagKey)
    {
        this.objectFlagValue = await client.GetObjectValueAsync(flagKey, new Value()).ConfigureAwait(false);
    }

    [Then(@"the resolved object value should be contain fields ""(.*)"", ""(.*)"", and ""(.*)"", with values ""(.*)"", ""(.*)"" and (.*), respectively")]
    public void Thentheresolvedobjectvalueshouldbecontainfieldsandwithvaluesandrespectively(string boolField, string stringField, string numberField, bool boolValue, string stringValue, int numberValue)
    {
        Value value = this.objectFlagValue;
        Assert.Equal(boolValue, value.AsStructure[boolField].AsBoolean);
        Assert.Equal(stringValue, value.AsStructure[stringField].AsString);
        Assert.Equal(numberValue, value.AsStructure[numberField].AsInteger);
    }

    [When(@"a boolean flag with key ""(.*)"" is evaluated with details and default value ""(.*)""")]
    public async Task Whenabooleanflagwithkeyisevaluatedwithdetailsanddefaultvalue(string flagKey, bool defaultValue)
    {
        this.booleanFlagDetails = await client.GetBooleanDetailsAsync(flagKey, defaultValue).ConfigureAwait(false);
    }

    [Then(@"the resolved boolean details value should be ""(.*)"", the variant should be ""(.*)"", and the reason should be ""(.*)""")]
    public void Thentheresolvedbooleandetailsvalueshouldbethevariantshouldbeandthereasonshouldbe(bool expectedValue, string expectedVariant, string expectedReason)
    {
        var result = this.booleanFlagDetails;
        Assert.Equal(expectedValue, result.Value);
        Assert.Equal(expectedVariant, result.Variant);
        Assert.Equal(expectedReason, result.Reason);
    }

    [When(@"a string flag with key ""(.*)"" is evaluated with details and default value ""(.*)""")]
    public async Task Whenastringflagwithkeyisevaluatedwithdetailsanddefaultvalue(string flagKey, string defaultValue)
    {
        this.stringFlagDetails = await client.GetStringDetailsAsync(flagKey, defaultValue).ConfigureAwait(false);
    }

    [Then(@"the resolved string details value should be ""(.*)"", the variant should be ""(.*)"", and the reason should be ""(.*)""")]
    public void Thentheresolvedstringdetailsvalueshouldbethevariantshouldbeandthereasonshouldbe(string expectedValue, string expectedVariant, string expectedReason)
    {
        var result = this.stringFlagDetails;
        Assert.Equal(expectedValue, result.Value);
        Assert.Equal(expectedVariant, result.Variant);
        Assert.Equal(expectedReason, result.Reason);
    }

    [When(@"an integer flag with key ""(.*)"" is evaluated with details and default value (.*)")]
    public async Task Whenanintegerflagwithkeyisevaluatedwithdetailsanddefaultvalue(string flagKey, int defaultValue)
    {
        this.intFlagDetails = await client.GetIntegerDetailsAsync(flagKey, defaultValue).ConfigureAwait(false);
    }

    [Then(@"the resolved integer details value should be (.*), the variant should be ""(.*)"", and the reason should be ""(.*)""")]
    public void Thentheresolvedintegerdetailsvalueshouldbethevariantshouldbeandthereasonshouldbe(int expectedValue, string expectedVariant, string expectedReason)
    {
        var result = this.intFlagDetails;
        Assert.Equal(expectedValue, result.Value);
        Assert.Equal(expectedVariant, result.Variant);
        Assert.Equal(expectedReason, result.Reason);
    }

    [When(@"a float flag with key ""(.*)"" is evaluated with details and default value (.*)")]
    public async Task Whenafloatflagwithkeyisevaluatedwithdetailsanddefaultvalue(string flagKey, double defaultValue)
    {
        this.doubleFlagDetails = await client.GetDoubleDetailsAsync(flagKey, defaultValue).ConfigureAwait(false);
    }

    [Then(@"the resolved float details value should be (.*), the variant should be ""(.*)"", and the reason should be ""(.*)""")]
    public void Thentheresolvedfloatdetailsvalueshouldbethevariantshouldbeandthereasonshouldbe(double expectedValue, string expectedVariant, string expectedReason)
    {
        var result = this.doubleFlagDetails;
        Assert.Equal(expectedValue, result.Value);
        Assert.Equal(expectedVariant, result.Variant);
        Assert.Equal(expectedReason, result.Reason);
    }

    [When(@"an object flag with key ""(.*)"" is evaluated with details and a null default value")]
    public async Task Whenanobjectflagwithkeyisevaluatedwithdetailsandanulldefaultvalue(string flagKey)
    {
        this.objectFlagDetails = await client.GetObjectDetailsAsync(flagKey, new Value()).ConfigureAwait(false);
    }

    [Then(@"the resolved object details value should be contain fields ""(.*)"", ""(.*)"", and ""(.*)"", with values ""(.*)"", ""(.*)"" and (.*), respectively")]
    public void Thentheresolvedobjectdetailsvalueshouldbecontainfieldsandwithvaluesandrespectively(string boolField, string stringField, string numberField, bool boolValue, string stringValue, int numberValue)
    {
        Value value = this.objectFlagDetails.Value;
        Assert.Equal(boolValue, value.AsStructure[boolField].AsBoolean);
        Assert.Equal(stringValue, value.AsStructure[stringField].AsString);
        Assert.Equal(numberValue, value.AsStructure[numberField].AsInteger);
    }

    [Then(@"the variant should be ""(.*)"", and the reason should be ""(.*)""")]
    public void Giventhevariantshouldbeandthereasonshouldbe(string expectedVariant, string expectedReason)
    {
        Assert.Equal(expectedVariant, this.objectFlagDetails.Variant);
        Assert.Equal(expectedReason, this.objectFlagDetails.Reason);
    }

    [When(@"context contains keys ""(.*)"", ""(.*)"", ""(.*)"", ""(.*)"" with values ""(.*)"", ""(.*)"", (.*), ""(.*)""")]
    public void Whencontextcontainskeyswithvalues(string field1, string field2, string field3, string field4, string value1, string value2, int value3, string value4)
    {
        var attributes = ImmutableDictionary.CreateBuilder<string, Value>();
        this.context = EvaluationContext.Builder()
            .Set(field1, new Value(value1))
            .Set(field2, new Value(value2))
            .Set(field3, new Value(value3))
            .Set(field4, new Value(bool.Parse(value4)))
            .Build();
    }

    [When(@"a flag with key ""(.*)"" is evaluated with default value ""(.*)""")]
    public async Task Givenaflagwithkeyisevaluatedwithdefaultvalue(string flagKey, string defaultValue)
    {
        contextAwareFlagKey = flagKey;
        contextAwareDefaultValue = defaultValue;
        contextAwareValue = await client.GetStringValueAsync(flagKey, contextAwareDefaultValue, context).ConfigureAwait(false);
    }

    [Then(@"the resolved string response should be ""(.*)""")]
    public void Thentheresolvedstringresponseshouldbe(string expected)
    {
        Assert.Equal(expected, this.contextAwareValue);
    }

    [Then(@"the resolved flag value is ""(.*)"" when the context is empty")]
    public async Task Giventheresolvedflagvalueiswhenthecontextisempty(string expected)
    {
        string emptyContextValue = await client.GetStringValueAsync(contextAwareFlagKey, contextAwareDefaultValue, EvaluationContext.Empty).ConfigureAwait(false);
        Assert.Equal(expected, emptyContextValue);
    }

    [When(@"a non-existent string flag with key ""(.*)"" is evaluated with details and a default value ""(.*)""")]
    public async Task Whenanonexistentstringflagwithkeyisevaluatedwithdetailsandadefaultvalue(string flagKey, string defaultValue)
    {
        this.notFoundFlagKey = flagKey;
        this.notFoundDefaultValue = defaultValue;
        this.notFoundDetails = await client.GetStringDetailsAsync(this.notFoundFlagKey, this.notFoundDefaultValue).ConfigureAwait(false);
    }

    [Then(@"the default string value should be returned")]
    public void Thenthedefaultstringvalueshouldbereturned()
    {
        Assert.Equal(this.notFoundDefaultValue, this.notFoundDetails.Value);
    }

    [Then(@"the reason should indicate an error and the error code should indicate a missing flag with ""(.*)""")]
    public void Giventhereasonshouldindicateanerrorandtheerrorcodeshouldindicateamissingflagwith(string errorCode)
    {
        Assert.Equal(Reason.Error.ToString(), notFoundDetails.Reason);
        Assert.Contains(errorCode, GetErrorTypeDescription(notFoundDetails.ErrorType));
    }

    [When(@"a string flag with key ""(.*)"" is evaluated as an integer, with details and a default value (.*)")]
    public async Task Whenastringflagwithkeyisevaluatedasanintegerwithdetailsandadefaultvalue(string flagKey, int defaultValue)
    {
        this.typeErrorFlagKey = flagKey;
        this.typeErrorDefaultValue = defaultValue;
        this.typeErrorDetails = await client.GetIntegerDetailsAsync(this.typeErrorFlagKey, this.typeErrorDefaultValue).ConfigureAwait(false);
    }

    [Then(@"the default integer value should be returned")]
    public void Thenthedefaultintegervalueshouldbereturned()
    {
        Assert.Equal(this.typeErrorDefaultValue, this.typeErrorDetails.Value);
    }

    [Then(@"the reason should indicate an error and the error code should indicate a type mismatch with ""(.*)""")]
    public void Giventhereasonshouldindicateanerrorandtheerrorcodeshouldindicateatypemismatchwith(string errorCode)
    {
        Assert.Equal(Reason.Error.ToString(), typeErrorDetails.Reason);
        Assert.Contains(errorCode, GetErrorTypeDescription(typeErrorDetails.ErrorType));
    }

    // convenience method to get the enum description.
    private static string GetErrorTypeDescription(Enum value)
    {
        FieldInfo info = value.GetType().GetField(value.ToString());
        DescriptionAttribute[] attributes = (DescriptionAttribute[])info.GetCustomAttributes(typeof(DescriptionAttribute));
        return attributes[0].Description;
    }
}
