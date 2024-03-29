﻿// ------------------------------------------------------------------------------
//  <auto-generated>
//      This code was generated by SpecFlow (https://www.specflow.org/).
//      SpecFlow Version:3.9.0.0
//      SpecFlow Generator Version:3.9.0.0
// 
//      Changes to this file may cause incorrect behavior and will be lost if
//      the code is regenerated.
//  </auto-generated>
// ------------------------------------------------------------------------------
#region Designer generated code
#pragma warning disable
namespace OpenFeature.Contrib.Providers.Flagd.E2e.ProcessTest.Features
{
    using TechTalk.SpecFlow;
    using System;
    using System.Linq;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "3.9.0.0")]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public partial class FlagEvaluationFeature : object, Xunit.IClassFixture<FlagEvaluationFeature.FixtureData>, System.IDisposable
    {
        
        private static TechTalk.SpecFlow.ITestRunner testRunner;
        
        private static string[] featureTags = ((string[])(null));
        
        private Xunit.Abstractions.ITestOutputHelper _testOutputHelper;
        
#line 1 "evaluation.feature"
#line hidden
        
        public FlagEvaluationFeature(FlagEvaluationFeature.FixtureData fixtureData, OpenFeature_Contrib_Providers_Flagd_E2e_ProcessTest_XUnitAssemblyFixture assemblyFixture, Xunit.Abstractions.ITestOutputHelper testOutputHelper)
        {
            this._testOutputHelper = testOutputHelper;
            this.TestInitialize();
        }
        
        public static void FeatureSetup()
        {
            testRunner = TechTalk.SpecFlow.TestRunnerManager.GetTestRunner();
            TechTalk.SpecFlow.FeatureInfo featureInfo = new TechTalk.SpecFlow.FeatureInfo(new System.Globalization.CultureInfo("en-US"), "Features", "Flag evaluation", null, ProgrammingLanguage.CSharp, featureTags);
            testRunner.OnFeatureStart(featureInfo);
        }
        
        public static void FeatureTearDown()
        {
            testRunner.OnFeatureEnd();
            testRunner = null;
        }
        
        public void TestInitialize()
        {
        }
        
        public void TestTearDown()
        {
            testRunner.OnScenarioEnd();
        }
        
        public void ScenarioInitialize(TechTalk.SpecFlow.ScenarioInfo scenarioInfo)
        {
            testRunner.OnScenarioInitialize(scenarioInfo);
            testRunner.ScenarioContext.ScenarioContainer.RegisterInstanceAs<Xunit.Abstractions.ITestOutputHelper>(_testOutputHelper);
        }
        
        public void ScenarioStart()
        {
            testRunner.OnScenarioStart();
        }
        
        public void ScenarioCleanup()
        {
            testRunner.CollectScenarioErrors();
        }
        
        public virtual void FeatureBackground()
        {
#line 5
  #line hidden
#line 6
    testRunner.Given("a provider is registered", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line hidden
        }
        
        void System.IDisposable.Dispose()
        {
            this.TestTearDown();
        }
        
        [Xunit.SkippableFactAttribute(DisplayName="Resolves boolean value")]
        [Xunit.TraitAttribute("FeatureTitle", "Flag evaluation")]
        [Xunit.TraitAttribute("Description", "Resolves boolean value")]
        public void ResolvesBooleanValue()
        {
            string[] tagsOfScenario = ((string[])(null));
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Resolves boolean value", null, tagsOfScenario, argumentsOfScenario, featureTags);
#line 9
  this.ScenarioInitialize(scenarioInfo);
#line hidden
            if ((TagHelper.ContainsIgnoreTag(tagsOfScenario) || TagHelper.ContainsIgnoreTag(featureTags)))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
#line 5
  this.FeatureBackground();
#line hidden
#line 10
    testRunner.When("a boolean flag with key \"boolean-flag\" is evaluated with default value \"false\"", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
#line 11
    testRunner.Then("the resolved boolean value should be \"true\"", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [Xunit.SkippableFactAttribute(DisplayName="Resolves string value")]
        [Xunit.TraitAttribute("FeatureTitle", "Flag evaluation")]
        [Xunit.TraitAttribute("Description", "Resolves string value")]
        public void ResolvesStringValue()
        {
            string[] tagsOfScenario = ((string[])(null));
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Resolves string value", null, tagsOfScenario, argumentsOfScenario, featureTags);
#line 13
  this.ScenarioInitialize(scenarioInfo);
#line hidden
            if ((TagHelper.ContainsIgnoreTag(tagsOfScenario) || TagHelper.ContainsIgnoreTag(featureTags)))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
#line 5
  this.FeatureBackground();
#line hidden
#line 14
    testRunner.When("a string flag with key \"string-flag\" is evaluated with default value \"bye\"", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
#line 15
    testRunner.Then("the resolved string value should be \"hi\"", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [Xunit.SkippableFactAttribute(DisplayName="Resolves integer value")]
        [Xunit.TraitAttribute("FeatureTitle", "Flag evaluation")]
        [Xunit.TraitAttribute("Description", "Resolves integer value")]
        public void ResolvesIntegerValue()
        {
            string[] tagsOfScenario = ((string[])(null));
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Resolves integer value", null, tagsOfScenario, argumentsOfScenario, featureTags);
#line 17
  this.ScenarioInitialize(scenarioInfo);
#line hidden
            if ((TagHelper.ContainsIgnoreTag(tagsOfScenario) || TagHelper.ContainsIgnoreTag(featureTags)))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
#line 5
  this.FeatureBackground();
#line hidden
#line 18
    testRunner.When("an integer flag with key \"integer-flag\" is evaluated with default value 1", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
#line 19
    testRunner.Then("the resolved integer value should be 10", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [Xunit.SkippableFactAttribute(DisplayName="Resolves float value")]
        [Xunit.TraitAttribute("FeatureTitle", "Flag evaluation")]
        [Xunit.TraitAttribute("Description", "Resolves float value")]
        public void ResolvesFloatValue()
        {
            string[] tagsOfScenario = ((string[])(null));
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Resolves float value", null, tagsOfScenario, argumentsOfScenario, featureTags);
#line 21
  this.ScenarioInitialize(scenarioInfo);
#line hidden
            if ((TagHelper.ContainsIgnoreTag(tagsOfScenario) || TagHelper.ContainsIgnoreTag(featureTags)))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
#line 5
  this.FeatureBackground();
#line hidden
#line 22
    testRunner.When("a float flag with key \"float-flag\" is evaluated with default value 0.1", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
#line 23
    testRunner.Then("the resolved float value should be 0.5", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [Xunit.SkippableFactAttribute(DisplayName="Resolves object value")]
        [Xunit.TraitAttribute("FeatureTitle", "Flag evaluation")]
        [Xunit.TraitAttribute("Description", "Resolves object value")]
        public void ResolvesObjectValue()
        {
            string[] tagsOfScenario = ((string[])(null));
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Resolves object value", null, tagsOfScenario, argumentsOfScenario, featureTags);
#line 25
  this.ScenarioInitialize(scenarioInfo);
#line hidden
            if ((TagHelper.ContainsIgnoreTag(tagsOfScenario) || TagHelper.ContainsIgnoreTag(featureTags)))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
#line 5
  this.FeatureBackground();
#line hidden
#line 26
    testRunner.When("an object flag with key \"object-flag\" is evaluated with a null default value", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
#line 27
    testRunner.Then("the resolved object value should be contain fields \"showImages\", \"title\", and \"im" +
                        "agesPerPage\", with values \"true\", \"Check out these pics!\" and 100, respectively", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [Xunit.SkippableFactAttribute(DisplayName="Resolves boolean details")]
        [Xunit.TraitAttribute("FeatureTitle", "Flag evaluation")]
        [Xunit.TraitAttribute("Description", "Resolves boolean details")]
        public void ResolvesBooleanDetails()
        {
            string[] tagsOfScenario = ((string[])(null));
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Resolves boolean details", null, tagsOfScenario, argumentsOfScenario, featureTags);
#line 30
  this.ScenarioInitialize(scenarioInfo);
#line hidden
            if ((TagHelper.ContainsIgnoreTag(tagsOfScenario) || TagHelper.ContainsIgnoreTag(featureTags)))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
#line 5
  this.FeatureBackground();
#line hidden
#line 31
    testRunner.When("a boolean flag with key \"boolean-flag\" is evaluated with details and default valu" +
                        "e \"false\"", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
#line 32
    testRunner.Then("the resolved boolean details value should be \"true\", the variant should be \"on\", " +
                        "and the reason should be \"STATIC\"", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [Xunit.SkippableFactAttribute(DisplayName="Resolves string details")]
        [Xunit.TraitAttribute("FeatureTitle", "Flag evaluation")]
        [Xunit.TraitAttribute("Description", "Resolves string details")]
        public void ResolvesStringDetails()
        {
            string[] tagsOfScenario = ((string[])(null));
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Resolves string details", null, tagsOfScenario, argumentsOfScenario, featureTags);
#line 34
  this.ScenarioInitialize(scenarioInfo);
#line hidden
            if ((TagHelper.ContainsIgnoreTag(tagsOfScenario) || TagHelper.ContainsIgnoreTag(featureTags)))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
#line 5
  this.FeatureBackground();
#line hidden
#line 35
    testRunner.When("a string flag with key \"string-flag\" is evaluated with details and default value " +
                        "\"bye\"", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
#line 36
    testRunner.Then("the resolved string details value should be \"hi\", the variant should be \"greeting" +
                        "\", and the reason should be \"STATIC\"", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [Xunit.SkippableFactAttribute(DisplayName="Resolves integer details")]
        [Xunit.TraitAttribute("FeatureTitle", "Flag evaluation")]
        [Xunit.TraitAttribute("Description", "Resolves integer details")]
        public void ResolvesIntegerDetails()
        {
            string[] tagsOfScenario = ((string[])(null));
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Resolves integer details", null, tagsOfScenario, argumentsOfScenario, featureTags);
#line 38
  this.ScenarioInitialize(scenarioInfo);
#line hidden
            if ((TagHelper.ContainsIgnoreTag(tagsOfScenario) || TagHelper.ContainsIgnoreTag(featureTags)))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
#line 5
  this.FeatureBackground();
#line hidden
#line 39
    testRunner.When("an integer flag with key \"integer-flag\" is evaluated with details and default val" +
                        "ue 1", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
#line 40
    testRunner.Then("the resolved integer details value should be 10, the variant should be \"ten\", and" +
                        " the reason should be \"STATIC\"", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [Xunit.SkippableFactAttribute(DisplayName="Resolves float details")]
        [Xunit.TraitAttribute("FeatureTitle", "Flag evaluation")]
        [Xunit.TraitAttribute("Description", "Resolves float details")]
        public void ResolvesFloatDetails()
        {
            string[] tagsOfScenario = ((string[])(null));
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Resolves float details", null, tagsOfScenario, argumentsOfScenario, featureTags);
#line 42
  this.ScenarioInitialize(scenarioInfo);
#line hidden
            if ((TagHelper.ContainsIgnoreTag(tagsOfScenario) || TagHelper.ContainsIgnoreTag(featureTags)))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
#line 5
  this.FeatureBackground();
#line hidden
#line 43
    testRunner.When("a float flag with key \"float-flag\" is evaluated with details and default value 0." +
                        "1", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
#line 44
    testRunner.Then("the resolved float details value should be 0.5, the variant should be \"half\", and" +
                        " the reason should be \"STATIC\"", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [Xunit.SkippableFactAttribute(DisplayName="Resolves object details")]
        [Xunit.TraitAttribute("FeatureTitle", "Flag evaluation")]
        [Xunit.TraitAttribute("Description", "Resolves object details")]
        public void ResolvesObjectDetails()
        {
            string[] tagsOfScenario = ((string[])(null));
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Resolves object details", null, tagsOfScenario, argumentsOfScenario, featureTags);
#line 46
  this.ScenarioInitialize(scenarioInfo);
#line hidden
            if ((TagHelper.ContainsIgnoreTag(tagsOfScenario) || TagHelper.ContainsIgnoreTag(featureTags)))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
#line 5
  this.FeatureBackground();
#line hidden
#line 47
    testRunner.When("an object flag with key \"object-flag\" is evaluated with details and a null defaul" +
                        "t value", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
#line 48
    testRunner.Then("the resolved object details value should be contain fields \"showImages\", \"title\"," +
                        " and \"imagesPerPage\", with values \"true\", \"Check out these pics!\" and 100, respe" +
                        "ctively", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
#line 49
    testRunner.And("the variant should be \"template\", and the reason should be \"STATIC\"", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [Xunit.SkippableFactAttribute(DisplayName="Resolves based on context")]
        [Xunit.TraitAttribute("FeatureTitle", "Flag evaluation")]
        [Xunit.TraitAttribute("Description", "Resolves based on context")]
        public void ResolvesBasedOnContext()
        {
            string[] tagsOfScenario = ((string[])(null));
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Resolves based on context", null, tagsOfScenario, argumentsOfScenario, featureTags);
#line 52
  this.ScenarioInitialize(scenarioInfo);
#line hidden
            if ((TagHelper.ContainsIgnoreTag(tagsOfScenario) || TagHelper.ContainsIgnoreTag(featureTags)))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
#line 5
  this.FeatureBackground();
#line hidden
#line 53
    testRunner.When("context contains keys \"fn\", \"ln\", \"age\", \"customer\" with values \"Sulisław\", \"Świę" +
                        "topełk\", 29, \"false\"", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
#line 54
    testRunner.And("a flag with key \"context-aware\" is evaluated with default value \"EXTERNAL\"", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 55
    testRunner.Then("the resolved string response should be \"INTERNAL\"", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
#line 56
    testRunner.And("the resolved flag value is \"EXTERNAL\" when the context is empty", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [Xunit.SkippableFactAttribute(DisplayName="Flag not found")]
        [Xunit.TraitAttribute("FeatureTitle", "Flag evaluation")]
        [Xunit.TraitAttribute("Description", "Flag not found")]
        public void FlagNotFound()
        {
            string[] tagsOfScenario = ((string[])(null));
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Flag not found", null, tagsOfScenario, argumentsOfScenario, featureTags);
#line 59
  this.ScenarioInitialize(scenarioInfo);
#line hidden
            if ((TagHelper.ContainsIgnoreTag(tagsOfScenario) || TagHelper.ContainsIgnoreTag(featureTags)))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
#line 5
  this.FeatureBackground();
#line hidden
#line 60
    testRunner.When("a non-existent string flag with key \"missing-flag\" is evaluated with details and " +
                        "a default value \"uh-oh\"", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
#line 61
    testRunner.Then("the default string value should be returned", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
#line 62
    testRunner.And("the reason should indicate an error and the error code should indicate a missing " +
                        "flag with \"FLAG_NOT_FOUND\"", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [Xunit.SkippableFactAttribute(DisplayName="Type error")]
        [Xunit.TraitAttribute("FeatureTitle", "Flag evaluation")]
        [Xunit.TraitAttribute("Description", "Type error")]
        public void TypeError()
        {
            string[] tagsOfScenario = ((string[])(null));
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Type error", null, tagsOfScenario, argumentsOfScenario, featureTags);
#line 64
  this.ScenarioInitialize(scenarioInfo);
#line hidden
            if ((TagHelper.ContainsIgnoreTag(tagsOfScenario) || TagHelper.ContainsIgnoreTag(featureTags)))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
#line 5
  this.FeatureBackground();
#line hidden
#line 65
    testRunner.When("a string flag with key \"wrong-flag\" is evaluated as an integer, with details and " +
                        "a default value 13", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
#line 66
    testRunner.Then("the default integer value should be returned", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
#line 67
    testRunner.And("the reason should indicate an error and the error code should indicate a type mis" +
                        "match with \"TYPE_MISMATCH\"", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "3.9.0.0")]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
        public class FixtureData : System.IDisposable
        {
            
            public FixtureData()
            {
                FlagEvaluationFeature.FeatureSetup();
            }
            
            void System.IDisposable.Dispose()
            {
                FlagEvaluationFeature.FeatureTearDown();
            }
        }
    }
}
#pragma warning restore
#endregion
