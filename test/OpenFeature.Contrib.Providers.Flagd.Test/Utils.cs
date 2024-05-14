using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OpenFeature.Contrib.Providers.Flagd.Test
{
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
        ""if"": [{ $ref: ""emailWithFaas"" }, ""bool1""]
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

        /// <summary>
        /// Repeatedly runs the supplied assertion until it doesn't throw, or the timeout is reached.
        /// </summary>
        /// <param name="assertionFunc">Function which makes an assertion</param>
        /// <param name="timeoutMillis">Timeout in millis (defaults to 1000)</param>
        /// <param name="pollIntervalMillis">Poll interval (defaults to 100</param>
        /// <returns></returns>
        public static async Task AssertUntilAsync(Action<CancellationToken> assertionFunc, int timeoutMillis = 1000, int pollIntervalMillis = 100)
        {
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(default(CancellationToken)))
            {

                cts.CancelAfter(timeoutMillis);

                var exceptions = new List<Exception>();
                var message = "AssertUntilAsync timeout reached.";

                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        assertionFunc(cts.Token);
                        return;
                    }
                    catch (TaskCanceledException) when (cts.IsCancellationRequested)
                    {
                        throw new AggregateException(message, exceptions);
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                    }

                    try
                    {
                        await Task.Delay(pollIntervalMillis, cts.Token).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                        throw new AggregateException(message, exceptions);
                    }
                }
                throw new AggregateException(message, exceptions);
            }
        }

        internal static void CleanEnvVars()
        {
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarTLS, "");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarSocketPath, "");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarCache, "");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarMaxCacheSize, "");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvCertPart, "");
            Environment.SetEnvironmentVariable(FlagdConfig.EnvVarResolverType, "");
        }
    }
}