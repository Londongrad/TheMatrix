using System.Net;
using Matrix.ApiGateway.DownstreamClients.Common.Exceptions;
using Matrix.ApiGateway.DownstreamClients.Resources.Scenarios.ClassicCity.Stockpiles;
using Matrix.ApiGateway.DownstreamClients.SimulationSystems.Scenarios.ClassicCity.EnvironmentalConditions;
using Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Requests;
using Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.EnvironmentalConditions.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Heating.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Requests;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Views;
using Xunit;
using static Matrix.ApiGateway.Tests.Http.HttpClientTestSupport;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.DownstreamClients.SimulationSystems
{
    public sealed class ResourcesAndSimulationSystemsApiClientTests
    {
        [Fact]
        public async Task StockpilesApiClientGetCityStockpilesAsync_WhenNotFound_ReturnsNull()
        {
            var cityId = Guid.Parse("aa520a7f-4680-4146-bf63-3f64689f262c");
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(CreateEmptyResponse(HttpStatusCode.NotFound))
            };
            IStockpilesApiClient client = CreateStockpilesApiClient(CreateHttpClient(handler));

            CityStockpilesView? result = await client.GetCityStockpilesAsync(
                cityId: cityId,
                cancellationToken: CancellationToken.None);

            Assert.Null(result);
            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.EndsWith(
                expectedEndString: $"/api/classic-city/cities/{cityId}/stockpiles",
                actualString: request.RequestUri,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public async Task StockpilesApiClientDispatchCityResupplyAsync_WhenResponseIsSuccessful_ReturnsView()
        {
            var cityId = Guid.Parse("ab52f633-47b2-4e85-8c1e-092267335c26");
            DispatchCityResupplyView dispatch = CreateDispatchCityResupplyView(
                cityId: cityId,
                requestedIntensity: "High",
                appliedIntensity: "High");
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    CreateJsonResponse(
                        statusCode: HttpStatusCode.OK,
                        payload: dispatch))
            };
            IStockpilesApiClient client = CreateStockpilesApiClient(CreateHttpClient(handler));

            DispatchCityResupplyView result = await client.DispatchCityResupplyAsync(
                cityId: cityId,
                request: new DispatchCityResupplyRequest(
                    Focus: ResupplyFocus.Food,
                    Intensity: ResupplyIntensity.High,
                    EmergencyOverride: true),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: dispatch,
                actual: result);
            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.Equal(
                expected: HttpMethod.Post,
                actual: request.Method);
            Assert.EndsWith(
                expectedEndString: $"/api/classic-city/cities/{cityId}/stockpiles/resupply",
                actualString: request.RequestUri,
                comparisonType: StringComparison.Ordinal);
            Assert.Contains(
                expectedSubstring: $"\"focus\":{(int)ResupplyFocus.Food}",
                actualString: request.Body,
                comparisonType: StringComparison.Ordinal);
            Assert.Contains(
                expectedSubstring: $"\"intensity\":{(int)ResupplyIntensity.High}",
                actualString: request.Body,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public async Task EnvironmentalConditionsApiClientGetCityEnvironmentalConditionsAsync_WhenNotFound_ReturnsNull()
        {
            var cityId = Guid.Parse("e95b0f3a-f0d1-4a57-b9a8-84c5f34977e4");
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(CreateEmptyResponse(HttpStatusCode.NotFound))
            };
            IEnvironmentalConditionsApiClient
                client = CreateEnvironmentalConditionsApiClient(CreateHttpClient(handler));

            CityEnvironmentalConditionsView? result = await client.GetCityEnvironmentalConditionsAsync(
                cityId: cityId,
                cancellationToken: CancellationToken.None);

            Assert.Null(result);
            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.EndsWith(
                expectedEndString: $"{ClassicCitySimulationSystemsApiRoutes.CitiesPath}/{cityId}/" +
                                   ClassicCitySimulationSystemsApiRoutes.EnvironmentalConditionsSegment,
                actualString: request.RequestUri,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public async Task
            EnvironmentalConditionsApiClientGetCityDistrictHeatingConditionsAsync_WhenResponseIsSuccessful_ReturnsView()
        {
            var cityId = Guid.Parse("0c2d91c7-4f7d-46bc-8c1b-8076ed88bc44");
            CityDistrictHeatingConditionsView heating = CreateCityDistrictHeatingConditionsView(cityId);
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    CreateJsonResponse(
                        statusCode: HttpStatusCode.OK,
                        payload: heating))
            };
            IEnvironmentalConditionsApiClient
                client = CreateEnvironmentalConditionsApiClient(CreateHttpClient(handler));

            CityDistrictHeatingConditionsView? result = await client.GetCityDistrictHeatingConditionsAsync(
                cityId: cityId,
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(
                expected: heating.CityId,
                actual: result!.CityId);
            Assert.Equal(
                expected: heating.EffectiveTickId,
                actual: result.EffectiveTickId);
            Assert.Equal(
                expected: heating.HeatingSupportIndex,
                actual: result.HeatingSupportIndex);
            Assert.Equal(
                expected: Assert.Single(heating.Districts),
                actual: Assert.Single(result.Districts));
            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.EndsWith(
                expectedEndString: $"{ClassicCitySimulationSystemsApiRoutes.CitiesPath}/{cityId}/" +
                                   ClassicCitySimulationSystemsApiRoutes.HeatingSegment + "/districts",
                actualString: request.RequestUri,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public async Task
            EnvironmentalConditionsApiClientDispatchCityUtilityIncidentResponseAsync_WhenConflictOccurs_ThrowsDownstreamServiceException()
        {
            var cityId = Guid.Parse("c27352d5-f522-4d55-b42d-a9f56a9f7b3a");
            CityUtilityIncidentStatusView conflict = CreateCityUtilityIncidentStatusView(
                cityId: cityId,
                statusIntensity: "Critical");
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    CreateJsonResponse(
                        statusCode: HttpStatusCode.Conflict,
                        payload: conflict))
            };
            IEnvironmentalConditionsApiClient
                client = CreateEnvironmentalConditionsApiClient(CreateHttpClient(handler));

            DownstreamServiceException exception = await Assert.ThrowsAsync<DownstreamServiceException>(()
                => client.DispatchCityUtilityIncidentResponseAsync(
                    cityId: cityId,
                    request: new DispatchCityUtilityIncidentResponseRequest(
                        Focus: "CriticalInfrastructure",
                        Intensity: "Critical"),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: HttpStatusCode.Conflict,
                actual: exception.StatusCode);
            Assert.Contains(
                expectedSubstring: "Critical",
                actualString: exception.Body,
                comparisonType: StringComparison.Ordinal);
        }
    }
}
