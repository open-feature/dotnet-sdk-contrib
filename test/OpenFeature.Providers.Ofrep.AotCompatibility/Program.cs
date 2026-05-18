using OpenFeature.Model;
using OpenFeature.Providers.Ofrep;
using OpenFeature.Providers.Ofrep.Configuration;

var options = new OfrepOptions("http://localhost:8010")
{
    Timeout = TimeSpan.FromSeconds(2),
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
    .Build();

using var cancellation = new CancellationTokenSource();
cancellation.Cancel();

_ = provider.GetMetadata();
_ = await provider.ResolveBooleanValueAsync("flag.bool", false, context, cancellation.Token).ConfigureAwait(false);
_ = await provider.ResolveStringValueAsync("flag.string", "fallback", context, cancellation.Token).ConfigureAwait(false);
_ = await provider.ResolveIntegerValueAsync("flag.int", 1, context, cancellation.Token).ConfigureAwait(false);
_ = await provider.ResolveDoubleValueAsync("flag.double", 1.0d, context, cancellation.Token).ConfigureAwait(false);
_ = await provider.ResolveStructureValueAsync(
    "flag.structure",
    new Value(Structure.Builder().Set("fallback", true).Build()),
    context,
    cancellation.Token).ConfigureAwait(false);

await provider.ShutdownAsync(cancellation.Token).ConfigureAwait(false);

Environment.SetEnvironmentVariable(OfrepOptions.EnvVarEndpoint, "http://localhost:8010");
using var envProvider = new OfrepProvider();
