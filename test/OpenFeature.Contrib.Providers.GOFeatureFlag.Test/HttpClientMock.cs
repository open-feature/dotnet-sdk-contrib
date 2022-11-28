using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.Test;

public class HttpClientMock
{
    public static Mock<HttpMessageHandler> GetResults<T>(T response)
    {
        var mockResponse = new HttpResponseMessage
        {
            Content = new StringContent(JsonConvert.SerializeObject(response)),
            StatusCode = HttpStatusCode.OK
        };

        mockResponse.Content.Headers.ContentType =
            new MediaTypeHeaderValue("application/json");
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);
        return mockHandler;
    }
}