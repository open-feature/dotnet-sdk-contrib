using System.Linq;
using System.Text.Json.Nodes;
using OpenFeature.Model;
using Xunit;

namespace OpenFeature.Providers.Flagd.Core.Test;

public class FractionalContextInjectionTest
{
    private const string Flags = @"{
      ""flags"": {
        ""variant"": {
          ""state"": ""ENABLED"",
          ""variants"": { ""red"": ""red"", ""blue"": ""blue"" },
          ""defaultVariant"": ""blue"",
          ""targeting"": { ""fractional"": [ ""k"", [ { ""var"": ""v"" }, 100 ] ] }
        },
        ""bucket"": {
          ""state"": ""ENABLED"",
          ""variants"": { ""red"": ""red"", ""blue"": ""blue"" },
          ""defaultVariant"": ""blue"",
          ""targeting"": { ""fractional"": [ ""k"", { ""var"": ""v"" } ] }
        },
        ""weight"": {
          ""state"": ""ENABLED"",
          ""variants"": { ""red"": ""red"", ""blue"": ""blue"" },
          ""defaultVariant"": ""blue"",
          ""targeting"": { ""fractional"": [ ""k"", [ ""red"", { ""var"": ""v"" } ], [ ""blue"", 0 ] ] }
        }
      }
    }";

    private const string TenantId = "acme-corp";

    private readonly FlagdCore _core;

    public FractionalContextInjectionTest()
    {
        _core = new FlagdCore();
        _core.SetConfigurations(Flags);
    }

    // contextValue is set as "v" (caller-controlled); "tenant-id" stands in for a value the app adds itself
    private string Resolve(string flagKey, string contextValue)
    {
        var context = EvaluationContext.Builder()
            .Set("v", ToValue(JsonNode.Parse(contextValue)))
            .Set("tenant-id", TenantId)
            .Build();
        return _core.ResolveString(flagKey, "default", context).Value;
    }

    private static Value ToValue(JsonNode node)
    {
        switch (node)
        {
            case null:
                return new Value();
            case JsonObject o:
                var s = Structure.Builder();
                foreach (var kv in o) s.Set(kv.Key, ToValue(kv.Value));
                return new Value(s.Build());
            case JsonArray a:
                return new Value(a.Select(ToValue).ToList());
            default:
                var v = node.AsValue();
                if (v.TryGetValue<bool>(out var b)) return new Value(b);
                if (v.TryGetValue<string>(out var str)) return new Value(str);
                return new Value(v.GetValue<double>());
        }
    }

    // control: context values used as data must still work, so the tests below can't pass by ignoring them
    [Theory]
    [InlineData("variant", @"""red""")]
    [InlineData("bucket", @"[""red"", 100]")]
    [InlineData("weight", "1")]
    public void ContextValueAsData(string flagKey, string contextValue)
    {
        Assert.Equal("red", Resolve(flagKey, contextValue));
    }

    [Fact]
    public void ContextValueAsBucketIsNotEvaluatedAsLogic()
    {
        // the injected logic selects red, so red means it ran; blue is the default
        Assert.Equal("blue", Resolve("bucket", @"[{""if"": [true, ""red"", ""red""]}, 100]"));
    }

    [Fact]
    public void ContextValueAsWeightIsNotEvaluatedAsLogic()
    {
        // the injected logic selects red, so red means it ran; blue is the default
        Assert.Equal("blue", Resolve("weight", @"{""+"": [1, 0]}"));
    }

    [Fact]
    public void InformationDisclosure()
    {
        const string alphabet = "abcdefghijklmnopqrstuvwxyz0123456789-_";
        var recovered = "";
        for (var i = 0; i < 20; i++)
        {
            var found = false;
            foreach (var c in alphabet)
            {
                var guess = recovered + c;
                var field = @"{""if"": [{""starts_with"": [{""var"": ""tenant-id""}, """ + guess + @"""]}, ""red"", ""blue""]}";
                if (Resolve("variant", field) == "red")
                {
                    recovered = guess;
                    found = true;
                    break;
                }
            }
            if (!found) break;
        }

        // the request field must not be able to reveal the server-side value
        Assert.NotEqual(TenantId, recovered);
    }

    [Fact]
    public void DenialOfService()
    {
        // a self-referencing value used to recurse until the process crashed with a stack overflow
        Assert.Equal("blue", Resolve("variant", @"{""fractional"": [""k"", [{""var"": ""v""}, 100]]}"));
    }
}
