using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace Matrix.SimulationSystems.Infrastructure.Tests.Http
{
    internal sealed class FakeHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return await handler(
                arg1: request,
                arg2: cancellationToken);
        }
    }

    internal static class HttpClientTestSupport
    {
        internal static HttpClient CreateHttpClient(HttpMessageHandler handler)
        {
            return new HttpClient(handler)
            {
                BaseAddress = new Uri("https://simulationsystems.test")
            };
        }

        internal static HttpResponseMessage CreateJsonResponse<T>(
            T payload,
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = JsonContent.Create(payload)
            };
        }

        internal static HttpResponseMessage CreateStringResponse(
            string payload,
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(
                    content: payload,
                    encoding: Encoding.UTF8,
                    mediaType: "application/json")
            };
        }
    }
}
