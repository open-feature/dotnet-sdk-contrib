using System.Net;
using System.Text;

namespace OpenFeature.Providers.Ofrep.Test.Helpers;

internal class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<(HttpStatusCode statusCode, string content, string? etag, Exception? exception)>
        _responses = new();

    private readonly List<HttpRequestMessage> _requests = new();

    public IReadOnlyList<HttpRequestMessage> Requests => this._requests.AsReadOnly();

    public void SetupResponse(HttpStatusCode statusCode, string content, string? etag = null)
    {
        this._responses.Enqueue((statusCode, content, etag, null));
    }

    public void SetupException(Exception exception)
    {
        this._responses.Enqueue((HttpStatusCode.OK, "", null, exception));
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        this._requests.Add(request);

        if (!this._responses.TryDequeue(out var response))
        {
            response = (HttpStatusCode.OK, "{}", null, null);
        }

        if (response.exception != null)
        {
            throw response.exception;
        }

        var httpResponse = new HttpResponseMessage(response.statusCode)
        {
            Content = new StringContent(response.content, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrEmpty(response.etag))
        {
            httpResponse.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue(response.etag);
        }

        // If status code indicates an error and no specific exception was set,
        // let EnsureSuccessStatusCode handle it
        return Task.FromResult(httpResponse);
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
