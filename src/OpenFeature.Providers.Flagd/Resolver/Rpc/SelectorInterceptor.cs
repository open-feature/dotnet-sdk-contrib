using Grpc.Core;
using Grpc.Core.Interceptors;

namespace OpenFeature.Providers.Flagd.Resolver.Rpc;

/// <summary>
/// gRPC client interceptor that adds the <c>flagd-selector</c> metadata header
/// to outgoing calls so the flagd server can serve the requested flag set.
/// </summary>
internal class SelectorInterceptor : Interceptor
{
    private readonly string _selector;

    internal SelectorInterceptor(string selector)
    {
        _selector = selector;
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        return continuation(request, WithSelector(context));
    }

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        return continuation(request, WithSelector(context));
    }

    private ClientInterceptorContext<TRequest, TResponse> WithSelector<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context)
        where TRequest : class
        where TResponse : class
    {
        var headers = context.Options.Headers ?? new Metadata();
        headers.Add(FlagdConfig.FlagdSelectorHeaderName, _selector);
        var options = context.Options.WithHeaders(headers);
        return new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, options);
    }
}
