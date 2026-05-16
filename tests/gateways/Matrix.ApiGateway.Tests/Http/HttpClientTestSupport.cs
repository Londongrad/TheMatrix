using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using Matrix.ApiGateway.Controllers.Economy;
using Matrix.ApiGateway.DownstreamClients.Economy;
using Matrix.ApiGateway.DownstreamClients.Population.People;
using Matrix.ApiGateway.DownstreamClients.Population.Person;
using Matrix.ApiGateway.DownstreamClients.Resources.Scenarios.ClassicCity.Stockpiles;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Trips;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Simulation;
using Matrix.ApiGateway.DownstreamClients.SimulationSystems.Scenarios.ClassicCity.EnvironmentalConditions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.ApiGateway.Tests.Http;

internal static class HttpClientTestSupport
{
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

            return await OnSendAsync(request, cancellationToken);
        }
    }

    internal static HttpClient CreateHttpClient(
        RecordingHttpMessageHandler handler,
        string baseAddress = "https://gateway.test")
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri(baseAddress, UriKind.Absolute)
        };
    }

    internal static HttpResponseMessage CreateJsonResponse<T>(HttpStatusCode statusCode, T payload)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = JsonContent.Create(payload)
        };
    }

    internal static HttpResponseMessage CreateStringResponse(
        HttpStatusCode statusCode,
        string payload,
        string contentType = "application/json")
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(payload, Encoding.UTF8, contentType)
        };
    }

    internal static HttpResponseMessage CreateEmptyResponse(HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(string.Empty, Encoding.UTF8, "application/json")
        };
    }

    internal static ISimulationApiClient CreateSimulationApiClient(HttpClient httpClient)
    {
        return CreateInternalClient<ISimulationApiClient>(
            typeName: "Matrix.ApiGateway.DownstreamClients.SimulationCore.Simulation.SimulationApiClient",
            args: [httpClient]);
    }

    internal static ICitiesApiClient CreateCitiesApiClient(HttpClient httpClient)
    {
        return CreateInternalClient<ICitiesApiClient>(
            typeName: "Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Cities.CitiesApiClient",
            args: [httpClient]);
    }

    internal static ITripsApiClient CreateTripsApiClient(HttpClient httpClient)
    {
        return CreateInternalClient<ITripsApiClient>(
            typeName: "Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Trips.TripsApiClient",
            args: [httpClient]);
    }

    internal static IPopulationApiClient CreatePopulationApiClient(HttpClient httpClient)
    {
        return new PopulationApiClient(httpClient);
    }

    internal static IPersonApiClient CreatePersonApiClient(HttpClient httpClient)
    {
        return new PersonApiClient(httpClient);
    }

    internal static IEconomyApiClient CreateEconomyApiClient(HttpClient httpClient)
    {
        Type type = GetGatewayAssembly().GetType(
                        "Matrix.ApiGateway.DownstreamClients.Economy.EconomyApiClient")
                    ?? throw new InvalidOperationException("EconomyApiClient type was not found.");

        Type nullLoggerType = typeof(NullLogger<>).MakeGenericType(type);
        object logger = nullLoggerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
            ?? nullLoggerType.GetField("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
            ?? throw new InvalidOperationException($"Null logger for '{type.FullName}' could not be created.");

        object? instance = Activator.CreateInstance(
            type,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: [httpClient, logger],
            culture: null);

        return Assert.IsAssignableFrom<IEconomyApiClient>(instance);
    }

    internal static IStockpilesApiClient CreateStockpilesApiClient(HttpClient httpClient)
    {
        return CreateInternalClient<IStockpilesApiClient>(
            typeName:
            "Matrix.ApiGateway.DownstreamClients.Resources.Scenarios.ClassicCity.Stockpiles.StockpilesApiClient",
            args: [httpClient]);
    }

    internal static IEnvironmentalConditionsApiClient CreateEnvironmentalConditionsApiClient(HttpClient httpClient)
    {
        return CreateInternalClient<IEnvironmentalConditionsApiClient>(
            typeName:
            "Matrix.ApiGateway.DownstreamClients.SimulationSystems.Scenarios.ClassicCity.EnvironmentalConditions.EnvironmentalConditionsApiClient",
            args: [httpClient]);
    }

    private static TClient CreateInternalClient<TClient>(string typeName, object?[] args)
        where TClient : class
    {
        Type type = GetGatewayAssembly().GetType(typeName)
            ?? throw new InvalidOperationException($"Gateway type '{typeName}' was not found.");

        object? instance = Activator.CreateInstance(
            type,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: args,
            culture: null);

        return Assert.IsAssignableFrom<TClient>(instance);
    }

    private static Assembly GetGatewayAssembly()
    {
        return typeof(EconomyController).Assembly;
    }
}
