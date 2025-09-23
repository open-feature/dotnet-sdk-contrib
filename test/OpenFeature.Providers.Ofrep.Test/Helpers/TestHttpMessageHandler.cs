using System.Net;
#if NETFRAMEWORK
using System.Net.Http;
#endif
using System.Text;

namespace OpenFeature.Providers.Ofrep.Test.Helpers;

internal class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<(HttpResponseMessage? responseMessage, Exception? exception)>
        _responses = new();

    private readonly List<HttpRequestMessage> _requests = new();

    public IReadOnlyList<HttpRequestMessage> Requests => this._requests.AsReadOnly();

    public void SetupResponse(HttpStatusCode statusCode, string content)
    {
        var httpResponse = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

        this.SetupResponse(httpResponse);
    }

    public void SetupResponse(HttpResponseMessage responseMessage)
    {
        this._responses.Enqueue((responseMessage, null));
    }

    public void SetupException(Exception exception)
    {
        this._responses.Enqueue((null, exception));
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        this._requests.Add(request);

#if NETFRAMEWORK
        var response = this._responses.Count > 0 ? this._responses.Dequeue() : (new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", Encoding.UTF8, "application/json") }, null);
#else
        if (!this._responses.TryDequeue(out var response))
        {
            response = (new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", Encoding.UTF8, "application/json") }, null);
        }
#endif

        if (response.exception != null)
        {
            throw response.exception;
        }

        // Return the pre-built response message
        return Task.FromResult(response.responseMessage!);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var request in this._requests)
            {
                request.Dispose();
            }

            this._requests.Clear();
        }

        base.Dispose(disposing);
    }
}
