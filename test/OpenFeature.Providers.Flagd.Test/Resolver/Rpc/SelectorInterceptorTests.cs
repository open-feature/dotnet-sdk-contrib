using Grpc.Core;
using Grpc.Core.Interceptors;
using OpenFeature.Providers.Flagd.Resolver.Rpc;
using Xunit;

namespace OpenFeature.Providers.Flagd.Test.Resolver.Rpc;

public class SelectorInterceptorTests
{
    private static readonly Method<string, string> _method = new Method<string, string>(
        MethodType.Unary,
        "test.Service",
        "Test",
        new Marshaller<string>(s => System.Text.Encoding.UTF8.GetBytes(s), b => System.Text.Encoding.UTF8.GetString(b)),
        new Marshaller<string>(s => System.Text.Encoding.UTF8.GetBytes(s), b => System.Text.Encoding.UTF8.GetString(b)));

    [Fact]
    public void AsyncUnaryCall_AddsFlagdSelectorHeader()
    {
        var interceptor = new SelectorInterceptor("my-selector");
        var context = new ClientInterceptorContext<string, string>(_method, host: null, options: default);

        ClientInterceptorContext<string, string>? capturedContext = null;
        var fakeCall = new AsyncUnaryCall<string>(
            System.Threading.Tasks.Task.FromResult(string.Empty),
            System.Threading.Tasks.Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });

        interceptor.AsyncUnaryCall("req", context, (req, ctx) =>
        {
            capturedContext = ctx;
            return fakeCall;
        });

        Assert.NotNull(capturedContext);
        var headers = capturedContext.Value.Options.Headers;
        Assert.NotNull(headers);
        Assert.Contains(headers, e => e.Key == FlagdConfig.FlagdSelectorHeaderName && e.Value == "my-selector");
    }
}
