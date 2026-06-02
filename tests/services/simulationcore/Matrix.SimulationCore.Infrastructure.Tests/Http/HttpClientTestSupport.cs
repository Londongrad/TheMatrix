using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning.Abstractions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Http
{
    internal static class HttpClientTestSupport
    {
        internal static HttpClient CreateHttpClient(
            RecordingHttpMessageHandler handler,
            string baseAddress = "https://localhost:7155")
        {
            return new HttpClient(handler)
            {
                BaseAddress = new Uri(
                    uriString: baseAddress,
                    uriKind: UriKind.Absolute)
            };
        }

        internal static HttpResponseMessage CreateJsonResponse<T>(
            HttpStatusCode statusCode,
            T payload)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = JsonContent.Create(payload)
            };
        }

        internal static HttpResponseMessage CreateStringResponse(
            HttpStatusCode statusCode,
            string payload)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(
                    content: payload,
                    encoding: Encoding.UTF8,
                    mediaType: "application/json")
            };
        }

        internal static ICityEconomyBootstrapClient CreateEconomyBootstrapClient(HttpClient httpClient)
        {
            return CreateInternalClient<ICityEconomyBootstrapClient>(
                typeName: "Matrix.SimulationCore.Infrastructure.Scenarios.ClassicCity.Integrations.Economy.CityEconomyBootstrapClient",
                httpClient: httpClient);
        }

        internal static ICityPopulationBootstrapClient CreatePopulationBootstrapClient(HttpClient httpClient)
        {
            return CreateInternalClient<ICityPopulationBootstrapClient>(
                typeName: "Matrix.SimulationCore.Infrastructure.Scenarios.ClassicCity.Integrations.Population.CityPopulationBootstrapClient",
                httpClient: httpClient);
        }

        internal static ICityRoadSegmentConditionsClient CreateRoadSegmentConditionsClient(HttpClient httpClient)
        {
            return CreateInternalClient<ICityRoadSegmentConditionsClient>(
                typeName: "Matrix.SimulationCore.Infrastructure.SimulationSystems.CityRoadSegmentConditionsClient",
                httpClient: httpClient);
        }

        private static TClient CreateInternalClient<TClient>(
            string typeName,
            HttpClient httpClient)
            where TClient : class
        {
            Type type = typeof(DependencyInjection).Assembly.GetType(typeName) ??
                        throw new InvalidOperationException($"Infrastructure type '{typeName}' was not found.");

            object? instance = Activator.CreateInstance(
                type: type,
                bindingAttr: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: [httpClient],
                culture: null);

            return Assert.IsAssignableFrom<TClient>(instance);
        }

        internal sealed record RecordedRequest(
            HttpMethod Method,
            string? RequestUri,
            string? ContentType,
            string? Body);

        internal sealed class RecordingHttpMessageHandler : HttpMessageHandler
        {
            public List<RecordedRequest> Requests { get; } = [];
            public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? OnSendAsync { get; set; }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                string? body = request.Content is null
                    ? null
                    : await request.Content.ReadAsStringAsync(cancellationToken);

                Requests.Add(
                    new RecordedRequest(
                        Method: request.Method,
                        RequestUri: request.RequestUri?.ToString(),
                        ContentType: request.Content?.Headers.ContentType?.MediaType,
                        Body: body));

                if (OnSendAsync is null)
                    throw new InvalidOperationException("No HTTP response factory configured for the test handler.");

                return await OnSendAsync(
                    arg1: request,
                    arg2: cancellationToken);
            }
        }
    }
}
