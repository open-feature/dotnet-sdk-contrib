using System.Net;
using System.Text;
using OpenFeature.Constant;
using OpenFeature.Model;
using OpenFeature.Providers.Ofrep;
using OpenFeature.Providers.Ofrep.Configuration;

// Minimal loopback OFREP evaluation server so this AOT binary exercises a real
// request/response round-trip instead of pre-cancelled no-op calls.
const int serverPort = 8010;
using var listener = new HttpListener();
listener.Prefixes.Add($"http://127.0.0.1:{serverPort}/");
listener.Start();

var serverTask = Task.Run(async () =>
{
    while (listener.IsListening)
    {
        HttpListenerContext context;
        try
        {
            context = await listener.GetContextAsync().ConfigureAwait(false);
        }
        catch (HttpListenerException)
        {
            break; // listener stopped
        }
        catch (ObjectDisposedException)
        {
            break; // listener disposed while waiting for a request
        }

        var (statusCode, responseBody) = GetResponse(ExtractFlagKey(context.Request.Url?.AbsolutePath));
        var bytes = Encoding.UTF8.GetBytes(responseBody);
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes).ConfigureAwait(false);
        context.Response.Close();
    }
});

var options = new OfrepOptions($"http://127.0.0.1:{serverPort}")
{
    Timeout = TimeSpan.FromSeconds(5),
    Headers = new Dictionary<string, string>
    {
        ["Authorization"] = "Bearer aot-test-token",
        ["X-OpenFeature-Test"] = "native-aot"
    }
};

using var provider = new OfrepProvider(options);

var context = EvaluationContext.Builder()
    .Set("targetingKey", "native-aot-user")
    .Set("plan", "gold")
    .Set("updatedAt", new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc))
    .Set("tags", new Value(new List<Value> { new("beta"), new("gold") }))
    .Set("attributes", Structure.Builder()
        .Set("tier", 3)
        .Set("loyal", true)
        .Build())
    .Build();

// Successful round-trips: each resolution must return the value served by the
// loopback server with no error state. The client converts serialization and
// deserialization failures into error results instead of throwing, so asserting
// the returned values/error state is what makes AOT regressions fail the build.
var boolResult = await provider.ResolveBooleanValueAsync("flag.bool", false, context).ConfigureAwait(false);
Check(boolResult.Value, "bool value");
Check(boolResult.ErrorType == ErrorType.None, "bool error state");
Check(boolResult.Reason == "TARGETING_MATCH", "bool reason");
Check(boolResult.Variant == "on", "bool variant");

var stringResult = await provider.ResolveStringValueAsync("flag.string", "fallback", context).ConfigureAwait(false);
Check(stringResult.Value == "resolved", "string value");
Check(stringResult.ErrorType == ErrorType.None, "string error state");

var intResult = await provider.ResolveIntegerValueAsync("flag.int", 1, context).ConfigureAwait(false);
Check(intResult.Value == 42, "int value");
Check(intResult.ErrorType == ErrorType.None, "int error state");

var doubleResult = await provider.ResolveDoubleValueAsync("flag.double", 1.0d, context).ConfigureAwait(false);
Check(doubleResult.Value == 4.2d, "double value");
Check(doubleResult.ErrorType == ErrorType.None, "double error state");

var structureResult = await provider.ResolveStructureValueAsync(
    "flag.structure",
    new Value(Structure.Builder().Set("fallback", true).Build()),
    context).ConfigureAwait(false);
Check(structureResult.ErrorType == ErrorType.None, "structure error state");
Check(structureResult.Value.AsStructure!.GetValue("inner").AsBoolean == true, "structure value");

await provider.ShutdownAsync().ConfigureAwait(false);
listener.Stop();
try
{
    await serverTask.ConfigureAwait(false);
}
catch (HttpListenerException)
{
    // Expected when the listener is stopped while GetContextAsync is pending.
}

Environment.SetEnvironmentVariable(OfrepOptions.EnvVarEndpoint, "http://127.0.0.1:8010");
using var envProvider = new OfrepProvider();

return Environment.ExitCode;

static string? ExtractFlagKey(string? path)
{
    if (path is null)
    {
        return null;
    }

    var separator = path.LastIndexOf('/');
    return separator >= 0 ? path[(separator + 1)..] : path;
}

static (int StatusCode, string Body) GetResponse(string? flagKey)
{
    return flagKey switch
    {
        "flag.bool" => (200, """{"key":"flag.bool","value":true,"reason":"TARGETING_MATCH","variant":"on"}"""),
        "flag.string" => (200, """{"key":"flag.string","value":"resolved","reason":"TARGETING_MATCH","variant":"on"}"""),
        "flag.int" => (200, """{"key":"flag.int","value":42,"reason":"TARGETING_MATCH","variant":"on"}"""),
        "flag.double" => (200, """{"key":"flag.double","value":4.2,"reason":"TARGETING_MATCH","variant":"on"}"""),
        "flag.structure" => (200, """{"key":"flag.structure","value":{"inner":true},"reason":"TARGETING_MATCH","variant":"on"}"""),
        _ => (404, """{"errorCode":"flag_not_found","errorDetails":"unexpected flag key in AOT test"}""")
    };
}

static void Check(bool condition, string what)
{
    if (!condition)
    {
        Console.Error.WriteLine($"AOT validation failed: {what}");
        Environment.ExitCode = 1;
    }
}
