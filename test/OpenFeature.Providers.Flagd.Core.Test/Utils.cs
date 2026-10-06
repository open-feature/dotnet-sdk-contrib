namespace OpenFeature.Providers.Flagd.Core.Test;

public class Utils
{
    public static string validFlagConfig = @"{
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

    public static string invalidFlagConfig = @"{
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

    public static string flags = @"{
  ""$evaluators"":{
    ""emailWithFaas"": {
        ""ends_with"": [{""var"":""email""}, ""@faas.com""]
      }
    },
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
    ""targetingBoolFlagUsingFlagdProperty"": {
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
                  ""$flagd.flagKey""
                ]
              },
              ""targetingBoolFlagUsingFlagdProperty""
            ]
          },
          ""bool1"",
          null
        ]
      }
    },
    ""targetingBoolFlagUsingFlagdPropertyTimestamp"": {
      ""state"": ""ENABLED"",
      ""variants"": {
        ""bool1"": true,
        ""bool2"": false
      },
      ""defaultVariant"": ""bool2"",
            ""targeting"": {
        ""if"": [
          {
            "">"": [
              {
                ""var"": [
                  ""$flagd.timestamp""
                ]
              },
              ""0""
            ]
          },
          ""bool1"",
          null
        ]
      }
    },
    ""targetingBoolFlagUsingSharedEvaluator"": {
      ""state"": ""ENABLED"",
      ""variants"": {
        ""bool1"": true,
        ""bool2"": false
      },
      ""defaultVariant"": ""bool2"",
      ""targeting"": {
        ""if"": [{ ""$ref"": ""emailWithFaas"" }, ""bool1""]
      }
    },
  ""targetingBoolFlagUsingSharedEvaluatorReturningBoolType"": {
      ""state"": ""ENABLED"",
      ""variants"": {
        ""true"": true,
        ""false"": false
      },
      ""defaultVariant"": ""true"",
      ""targeting"": {
        ""if"": [{ ""$ref"": ""emailWithFaas"" }, true]
      }
    },
    ""targetingBoolFlagWithMissingDefaultVariant"": {
      ""state"": ""ENABLED"",
      ""variants"": {
        ""bool1"": true,
        ""bool2"": false
      },
      ""defaultVariant"": ""true"",
      ""targeting"": {
        ""if"": [{ ""$ref"": ""emailWithFaas"" }, ""bool1""]
      }
    },
    ""targetingBoolFlagWithUnexpectedVariantType"": {
      ""state"": ""ENABLED"",
      ""variants"": {
        ""bool1"": 20,
        ""bool2"": 30
      },
      ""defaultVariant"": ""true"",
      ""targeting"": {
        ""if"": [{ ""$ref"": ""emailWithFaas"" }, ""bool1""]
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
    },
    ""metadata-flag"": {
      ""state"": ""ENABLED"",
      ""variants"": {
        ""on"": true,
        ""off"": false
      },
      ""defaultVariant"": ""on"",
      ""metadata"": {
        ""string"": ""1.0.2"",
          ""integer"": 2,
          ""boolean"": true,
          ""float"": 0.1
      }
    }
  }
}";

    public static string metadataFlags = @"{
  ""flags"":{
    ""metadata-flag"": {
      ""state"": ""ENABLED"",
      ""variants"": {
        ""on"": true,
        ""off"": false
      },
      ""defaultVariant"": ""on"",
      ""metadata"":{
        ""string"": ""1.0.2"",
          ""integer"": 2,
          ""boolean"": true,
          ""float"": 0.1
      }
    },
    ""without-metadata-flag"": {
      ""state"": ""ENABLED"",
      ""variants"": {
        ""on"": true,
        ""off"": false
      },
      ""defaultVariant"": ""on""
    }
  },
  ""metadata"": {
    ""string"": ""1.0.3"",
    ""integer"": 3,
    ""boolean"": false,
    ""float"": 0.2
  }
}";

    public static string invalidFlagSetMetadata = @"{
  ""flags"":{
    ""without-metadata-flag"": {
      ""state"": ""ENABLED"",
      ""variants"": {
        ""on"": true,
        ""off"": false
      },
      ""defaultVariant"": ""on""
    }
  },
  ""metadata"": {
    ""string"": {""in"": ""valid""},
    ""integer"": 3,
    ""boolean"": false,
    ""float"": 0.2
  }
}";
    public static string invalidFlagMetadata = @"{
  ""flags"":{
    ""invalid-metadata-flag"": {
      ""state"": ""ENABLED"",
      ""variants"": {
        ""on"": true,
        ""off"": false
      },
      ""defaultVariant"": ""on"",
      ""metadata"": {
        ""string"": ""1.0.2"",
          ""integer"": 2,
          ""boolean"": true,
          ""float"": {""in"": ""valid""}
      }
    }
  }
}";
}
