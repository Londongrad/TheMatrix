using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using Matrix.ApiGateway.Controllers.Economy;
using Matrix.ApiGateway.DownstreamClients.Economy;
using Matrix.ApiGateway.DownstreamClients.Economy.Scenarios.ClassicCity;
using Matrix.ApiGateway.DownstreamClients.Identity.Admin.Permissions;
using Matrix.ApiGateway.DownstreamClients.Identity.Admin.Roles;
using Matrix.ApiGateway.DownstreamClients.Identity.Admin.Users;
using Matrix.ApiGateway.DownstreamClients.Identity.Internal.PermissionsVersion;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Account;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Assets;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Auth;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Sessions;
using Matrix.ApiGateway.DownstreamClients.Population.People;
using Matrix.ApiGateway.DownstreamClients.Population.Person;
using Matrix.ApiGateway.DownstreamClients.Population.Scenarios.ClassicCity;
using Matrix.ApiGateway.DownstreamClients.Resources.Scenarios.ClassicCity.Stockpiles;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Trips;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Simulation;
using Matrix.ApiGateway.DownstreamClients.SimulationSystems.Scenarios.ClassicCity.EnvironmentalConditions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.ApiGateway.Tests.Http
{
    internal static class HttpClientTestSupport
    {
        internal static HttpClient CreateHttpClient(
            RecordingHttpMessageHandler handler,
            string baseAddress = "https://gateway.test")
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
            string payload,
            string contentType = "application/json")
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(
                    content: payload,
                    encoding: Encoding.UTF8,
                    mediaType: contentType)
            };
        }

        internal static HttpResponseMessage CreateEmptyResponse(HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(
                    content: string.Empty,
                    encoding: Encoding.UTF8,
                    mediaType: "application/json")
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
                typeName:
                "Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Cities.CitiesApiClient",
                args: [httpClient]);
        }

        internal static ITripsApiClient CreateTripsApiClient(HttpClient httpClient)
        {
            return CreateInternalClient<ITripsApiClient>(
                typeName:
                "Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Trips.TripsApiClient",
                args: [httpClient]);
        }

        internal static IPopulationApiClient CreatePopulationApiClient(HttpClient httpClient)
        {
            return new PopulationApiClient(httpClient);
        }

        internal static IClassicCityPopulationApiClient CreateClassicCityPopulationApiClient(HttpClient httpClient)
        {
            return CreateInternalClient<IClassicCityPopulationApiClient>(
                typeName:
                "Matrix.ApiGateway.DownstreamClients.Population.Scenarios.ClassicCity.ClassicCityPopulationApiClient",
                args: [httpClient]);
        }

        internal static IPersonApiClient CreatePersonApiClient(HttpClient httpClient)
        {
            return new PersonApiClient(httpClient);
        }

        internal static IIdentityAuthClient CreateIdentityAuthApiClient(HttpClient httpClient)
        {
            return new IdentityAuthApiClient(httpClient);
        }

        internal static IIdentitySessionsClient CreateIdentitySessionsApiClient(HttpClient httpClient)
        {
            return new IdentitySessionsApiClient(httpClient);
        }

        internal static IIdentityAccountClient CreateIdentityAccountApiClient(HttpClient httpClient)
        {
            return new IdentityAccountApiClient(httpClient);
        }

        internal static IIdentityAssetsClient CreateIdentityAssetsApiClient(HttpClient httpClient)
        {
            return new IdentityAssetsApiClient(httpClient);
        }

        internal static IIdentityAdminUsersClient CreateIdentityAdminUsersApiClient(HttpClient httpClient)
        {
            return new IdentityAdminUsersApiClient(httpClient);
        }

        internal static IIdentityAdminRolesClient CreateIdentityAdminRolesApiClient(HttpClient httpClient)
        {
            return new IdentityAdminRolesApiClient(httpClient);
        }

        internal static IIdentityAdminPermissionsClient CreateIdentityAdminPermissionsApiClient(HttpClient httpClient)
        {
            return new IdentityAdminPermissionsApiClient(httpClient);
        }

        internal static IIdentityInternalUsersClient CreateIdentityInternalUsersApiClient(HttpClient httpClient)
        {
            return new IdentityInternalUsersClient(httpClient);
        }

        internal static IEconomyApiClient CreateEconomyApiClient(HttpClient httpClient)
        {
            return Assert.IsAssignableFrom<IEconomyApiClient>(CreateEconomyApiClientInstance(httpClient));
        }

        internal static IClassicCityEconomyApiClient CreateClassicCityEconomyApiClient(HttpClient httpClient)
        {
            return CreateInternalClient<IClassicCityEconomyApiClient>(
                typeName:
                "Matrix.ApiGateway.DownstreamClients.Economy.Scenarios.ClassicCity.ClassicCityEconomyApiClient",
                args: [httpClient]);
        }

        private static object CreateEconomyApiClientInstance(HttpClient httpClient)
        {
            Type type = GetGatewayAssembly()
                           .GetType("Matrix.ApiGateway.DownstreamClients.Economy.EconomyApiClient") ??
                        throw new InvalidOperationException("EconomyApiClient type was not found.");

            Type nullLoggerType = typeof(NullLogger<>).MakeGenericType(type);
            object logger = nullLoggerType.GetProperty(
                                    name: "Instance",
                                    bindingAttr: BindingFlags.Public | BindingFlags.Static)
                              ?.GetValue(null) ??
                            nullLoggerType.GetField(
                                    name: "Instance",
                                    bindingAttr: BindingFlags.Public | BindingFlags.Static)
                              ?.GetValue(null) ??
                            throw new InvalidOperationException(
                                $"Null logger for '{type.FullName}' could not be created.");

            object? instance = Activator.CreateInstance(
                type: type,
                bindingAttr: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args:
                [
                    httpClient,
                    logger
                ],
                culture: null);

            return Assert.IsAssignableFrom<object>(instance);
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

        private static TClient CreateInternalClient<TClient>(
            string typeName,
            object?[] args)
            where TClient : class
        {
            Type type = GetGatewayAssembly()
                           .GetType(typeName) ??
                        throw new InvalidOperationException($"Gateway type '{typeName}' was not found.");

            object? instance = Activator.CreateInstance(
                type: type,
                bindingAttr: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: args,
                culture: null);

            return Assert.IsAssignableFrom<TClient>(instance);
        }

        private static Assembly GetGatewayAssembly()
        {
            return typeof(EconomyController).Assembly;
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
