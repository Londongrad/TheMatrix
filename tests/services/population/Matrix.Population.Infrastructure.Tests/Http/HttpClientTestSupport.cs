using System.Net;
using System.Text;

namespace Matrix.Population.Infrastructure.Tests.Http
{
    internal static class HttpClientTestSupport
    {
        internal static HttpClient CreateClient(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            return new HttpClient(new StubHttpMessageHandler(handler))
            {
                BaseAddress = new Uri("https://population.test")
            };
        }

        internal static HttpResponseMessage JsonResponse(
            string json,
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(
                    content: json,
                    encoding: Encoding.UTF8,
                    mediaType: "application/json")
            };
        }

        internal static HttpResponseMessage EmptyResponse(HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(
                    content: string.Empty,
                    encoding: Encoding.UTF8,
                    mediaType: "application/json")
            };
        }

        internal sealed class StubHttpMessageHandler(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
        {
            public List<HttpRequestMessage> Requests { get; } = [];

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                Requests.Add(request);
                return handler(
                    arg1: request,
                    arg2: cancellationToken);
            }
        }
    }
}
