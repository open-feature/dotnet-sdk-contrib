using System.Collections.Immutable;
using AutoFixture;
using OpenFeature.Constant;
using OpenFeature.Model;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flagd.Test
{
    public class UnitTestJsonEvaluator
    {
        static string validFlagConfig = @"{
            ""flags"": {
                ""validFlag"": {
                    ""state"": ""ENABLED"",
                        ""variants"": {
                        ""on"": true,
                            ""off"": false
                        },
                        ""defaultVariant"": ""on""
                    }
                }
        }";
        
        static string invalidFlagConfig = @"{
  ""flags"": {
    ""invalidFlag"": {
      ""notState"": ""ENABLED"",
      ""notVariants"": {
        ""on"": true,
        ""off"": false
      },
      ""notDefaultVariant"": ""on""
    }
  }
}";
        
        static string flags = @"{
  ""flags"": {
    ""staticBoolFlag"": {
      ""state"": ""ENABLED"",
      ""variants"": {
        ""on"": true,
        ""off"": false
      },
      ""defaultVariant"": ""on""
    },
		""staticStringFlag"": {
      ""state"": ""ENABLED"",
      ""variants"": {
        ""red"": ""#CC0000"",
        ""blue"": ""#0000CC""
      },
      ""defaultVariant"": ""red""
    },
		""staticFloatFlag"": {
      ""state"": ""ENABLED"",
      ""variants"": {
        ""one"": 1.000000,
        ""two"": 2
      },
      ""defaultVariant"": ""one""
    },
	""staticIntFlag"": {
		""state"": ""ENABLED"",
		""variants"": {
		  ""one"": 1,
		  ""two"": 2
		},
		""defaultVariant"": ""one""
	  },
		""staticObjectFlag"": {
      ""state"": ""ENABLED"",
      ""variants"": {
        ""obj1"": {""abc"": 123},
        ""obj2"": {
					""xyz"": true
				}
      },
      ""defaultVariant"": ""obj1""
    },
		""targetingBoolFlag"": {
      ""state"": ""ENABLED"",
      ""variants"": {
        ""bool1"": true,
        ""bool2"": false
      },
      ""defaultVariant"": ""bool2"",
			""targeting"": {
        ""if"": [
          {
            ""=="": [
              {
                ""var"": [
                  ""color""
                ]
              },
              ""yellow""
            ]
          },
          ""bool1"",
          null
        ]
      }
    },
		""targetingStringFlag"": {
      ""state"": ""ENABLED"",
      ""variants"": {
        ""str1"": ""my-string"",
        ""str2"": ""other""
      },
      ""defaultVariant"": ""str2"",
			""targeting"": {
        ""if"": [
          {
            ""=="": [
              {
                ""var"": [
                  ""color""
                ]
              },
              ""yellow""
            ]
          },
          ""str1"",
          null
        ]
      }
    },
		""targetingFloatFlag"": {
      ""state"": ""ENABLED"",
      ""variants"": {
        ""number1"": 100.000000,
        ""number2"": 200
      },
      ""defaultVariant"": ""number2"",
			""targeting"": {
        ""if"": [
          {
            ""=="": [
              {
                ""var"": [
                  ""color""
                ]
              },
              ""yellow""
            ]
          },
          ""number1"",
          null
        ]
      }
    },
	""targetingNumberFlag"": {
		""state"": ""ENABLED"",
		""variants"": {
		  ""number1"": 100,
		  ""number2"": 200
		},
		""defaultVariant"": ""number2"",
			  ""targeting"": {
		  ""if"": [
			{
			  ""=="": [
				{
				  ""var"": [
					""color""
				  ]
				},
				""yellow""
			  ]
			},
			""number1"",
			null
		  ]
		}
	  },
		""targetingObjectFlag"": {
      ""state"": ""ENABLED"",
      ""variants"": {
        ""object1"": { ""key"": true },
        ""object2"": {}
      },
      ""defaultVariant"": ""object2"",
			""targeting"": {
        ""if"": [
          {
            ""=="": [
              {
                ""var"": [
                  ""color""
                ]
              },
              ""yellow""
            ]
          },
          ""object1"",
          null
        ]
      }
    },
	""disabledFlag"": {
      ""state"": ""DISABLED"",
      ""variants"": {
        ""on"": true,
        ""off"": false
      },
      ""defaultVariant"": ""on""
    }
  }
}";
            
        [Fact]
        public void TestJsonEvaluatorAddFlagConfig()
        {
            var fixture = new Fixture();
            
            var jsonEvaluator = new JsonEvaluator(fixture.Create<string>());
                
            jsonEvaluator.Sync(FlagConfigurationUpdateType.ADD, validFlagConfig);
            
            var result = jsonEvaluator.ResolveBooleanValue("validFlag", false);
            
            Assert.True(result.Value);
            
        }
        
        [Fact]
        public void TestJsonEvaluatorAddStaticStringEvaluation()
        {
            var fixture = new Fixture();

            var jsonEvaluator = new JsonEvaluator(fixture.Create<string>());
            
            jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, flags);
            
            var result = jsonEvaluator.ResolveStringValue("staticStringFlag", "");
            
            Assert.Equal("#CC0000", result.Value);
            Assert.Equal("red", result.Variant);
            Assert.Equal(Reason.Static, result.Reason);

        }
        
        [Fact]
        public void TestJsonEvaluatorDynamicBoolEvaluation()
        {
          var fixture = new Fixture();

          var jsonEvaluator = new JsonEvaluator(fixture.Create<string>());

          jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, flags);
          
          var attributes = ImmutableDictionary.CreateBuilder<string, Value>();
          attributes.Add("color", new Value("yellow"));
          
          var builder = EvaluationContext.Builder();
          builder
            .Set("color", "yellow");
          
          var result = jsonEvaluator.ResolveBooleanValue("targetingBoolFlag", false, builder.Build());
          
          Assert.True(result.Value);
          Assert.Equal("bool1", result.Variant);
          Assert.Equal(Reason.TargetingMatch, result.Reason);
        }
        
        [Fact]
        public void TestJsonEvaluatorDynamicStringEvaluation()
        {
          var fixture = new Fixture();

          var jsonEvaluator = new JsonEvaluator(fixture.Create<string>());

          jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, flags);

          var attributes = ImmutableDictionary.CreateBuilder<string, Value>();
          attributes.Add("color", new Value("yellow"));
          
          var builder = EvaluationContext.Builder();
          builder
            .Set("color", "yellow");
          
          var result = jsonEvaluator.ResolveStringValue("targetingStringFlag", "", builder.Build());
          
          Assert.Equal("my-string", result.Value);
          Assert.Equal("str1", result.Variant);
          Assert.Equal(Reason.TargetingMatch, result.Reason);
        }
        
        [Fact]
        public void TestJsonEvaluatorDynamicFloatEvaluation()
        {
          var fixture = new Fixture();

          var jsonEvaluator = new JsonEvaluator(fixture.Create<string>());

          jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, flags);

          var attributes = ImmutableDictionary.CreateBuilder<string, Value>();
          attributes.Add("color", new Value("yellow"));

          var builder = EvaluationContext.Builder();
          builder
            .Set("color", "yellow");

          var result = jsonEvaluator.ResolveDoubleValue("targetingFloatFlag", 0, builder.Build());
          
          Assert.Equal(100, result.Value);
          Assert.Equal("number1", result.Variant);
          Assert.Equal(Reason.TargetingMatch, result.Reason);
        }
        
        [Fact]
        public void TestJsonEvaluatorDynamicIntEvaluation()
        {
          var fixture = new Fixture();

          var jsonEvaluator = new JsonEvaluator(fixture.Create<string>());

          jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, flags);

          var attributes = ImmutableDictionary.CreateBuilder<string, Value>();
          attributes.Add("color", new Value("yellow"));

          var builder = EvaluationContext.Builder();
          builder
            .Set("color", "yellow");
          
          var result = jsonEvaluator.ResolveIntegerValue("targetingNumberFlag", 0, builder.Build());
          
          Assert.Equal(100, result.Value);
          Assert.Equal("number1", result.Variant);
          Assert.Equal(Reason.TargetingMatch, result.Reason);
        }
        
        [Fact]
        public void TestJsonEvaluatorDynamicObjectEvaluation()
        {
          var fixture = new Fixture();

          var jsonEvaluator = new JsonEvaluator(fixture.Create<string>());

          jsonEvaluator.Sync(FlagConfigurationUpdateType.ALL, flags);

          var attributes = ImmutableDictionary.CreateBuilder<string, Value>();
          attributes.Add("color", new Value("yellow"));

          var builder = EvaluationContext.Builder();
          builder
            .Set("color", "yellow");

          var result = jsonEvaluator.ResolveStructureValue("targetingObjectFlag", null, builder.Build());
          
          Assert.True(result.Value.AsStructure.AsDictionary()["key"].AsBoolean);
          Assert.Equal("object1", result.Variant);
          Assert.Equal(Reason.TargetingMatch, result.Reason);
        }
    }    
}
