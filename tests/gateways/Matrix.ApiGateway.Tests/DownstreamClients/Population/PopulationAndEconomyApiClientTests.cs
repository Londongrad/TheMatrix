using System.Net;
using System.Text.Json;
using Matrix.ApiGateway.DownstreamClients.Common.Exceptions;
using Matrix.ApiGateway.DownstreamClients.Economy;
using Matrix.ApiGateway.DownstreamClients.Economy.Scenarios.ClassicCity;
using Matrix.ApiGateway.DownstreamClients.Population.People;
using Matrix.ApiGateway.DownstreamClients.Population.Person;
using Matrix.ApiGateway.DownstreamClients.Population.Scenarios.ClassicCity;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Contracts.Budget.Requests;
using Matrix.Economy.Contracts.Budget.Views;
using Matrix.Population.Contracts;
using Matrix.Population.Contracts.Models;
using Xunit;
using static Matrix.ApiGateway.Tests.Http.HttpClientTestSupport;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.DownstreamClients.Population
{
    public sealed class PopulationAndEconomyApiClientTests
    {
        [Fact]
        public async Task ClassicCityPopulationApiClientGetCityResidentsPageAsync_WhenCalled_UsesDateAndPaginationQuery()
        {
            var cityId = Guid.Parse("18f5a5d3-cb51-4626-a98e-56450b5657fc");
            DateOnly currentDate = new(
                year: 2048,
                month: 6,
                day: 8);
            PagedResult<PersonDto> page = CreateResidentsPageResult();
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    CreateJsonResponse(
                        statusCode: HttpStatusCode.OK,
                        payload: page))
            };
            IClassicCityPopulationApiClient client = CreateClassicCityPopulationApiClient(CreateHttpClient(handler));

            PagedResult<PersonDto> result = await client.GetCityResidentsPageAsync(
                cityId: cityId,
                currentDate: currentDate,
                pageNumber: 3,
                pageSize: 40,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: page.Items,
                actual: result.Items);
            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.EndsWith(
                expectedEndString:
                $"/api/population/cities/{cityId}/residents?currentDate=2048-06-08&pageNumber=3&pageSize=40",
                actualString: request.RequestUri,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public async Task PopulationApiClientGetPeoplePageAsync_WhenBodyIsEmpty_ThrowsJsonException()
        {
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(CreateEmptyResponse())
            };
            IPopulationApiClient client = CreatePopulationApiClient(CreateHttpClient(handler));

            JsonException exception = await Assert.ThrowsAsync<JsonException>(()
                => client.GetPeoplePageAsync(
                    pageNumber: 2,
                    pageSize: 15,
                    cancellationToken: CancellationToken.None));

            Assert.Contains(
                expectedSubstring: "JSON",
                actualString: exception.Message,
                comparisonType: StringComparison.OrdinalIgnoreCase);
            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.EndsWith(
                expectedEndString: PopulationApiRoutes.PeoplePath + "?pageNumber=2&pageSize=15",
                actualString: request.RequestUri,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public async Task
            ClassicCityPopulationApiClientGetCityResidentDetailsAsync_WhenJsonIsMalformed_ThrowsDownstreamServiceException()
        {
            var cityId = Guid.Parse("2d6fd248-dcda-40bb-b402-ea0bcae2d69f");
            var personId = Guid.Parse("a07d5b2e-fde8-45c5-af40-9d484fe60c47");
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    CreateStringResponse(
                        statusCode: HttpStatusCode.OK,
                        payload: "{bad-json"))
            };
            IClassicCityPopulationApiClient client = CreateClassicCityPopulationApiClient(CreateHttpClient(handler));

            DownstreamServiceException exception = await Assert.ThrowsAsync<DownstreamServiceException>(()
                => client.GetCityResidentDetailsAsync(
                    cityId: cityId,
                    personId: personId,
                    currentDate: new DateOnly(
                        year: 2048,
                        month: 6,
                        day: 8),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: HttpStatusCode.BadGateway,
                actual: exception.StatusCode);
            Assert.Contains(
                expectedSubstring: "InvalidDownstreamJson",
                actualString: exception.Body,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public async Task PersonApiClientKillAsync_WhenResponseIsSuccessful_PostsExpectedUrl()
        {
            var personId = Guid.Parse("1039b5aa-a305-4b58-9aad-99d15b5f4f39");
            PersonDto person = Assert.Single(
                CreateResidentsPageResult(personId)
                   .Items);
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    CreateJsonResponse(
                        statusCode: HttpStatusCode.OK,
                        payload: person))
            };
            IPersonApiClient client = CreatePersonApiClient(CreateHttpClient(handler));

            PersonDto result = await client.KillAsync(
                personId: personId,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: person,
                actual: result);
            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.Equal(
                expected: HttpMethod.Post,
                actual: request.Method);
            Assert.EndsWith(
                expectedEndString: $"{PopulationApiRoutes.PersonPath}/{personId}/kill",
                actualString: request.RequestUri,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public async Task ClassicCityEconomyApiClientGetCitySummaryAsync_WhenResponseIsSuccessful_ReturnsSummary()
        {
            var cityId = Guid.Parse("e0d3d66c-654f-4af5-aa7e-32c78d5580be");
            EconomySummaryView summary = CreateEconomySummaryView();
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    CreateJsonResponse(
                        statusCode: HttpStatusCode.OK,
                        payload: summary))
            };
            IClassicCityEconomyApiClient client = CreateClassicCityEconomyApiClient(CreateHttpClient(handler));

            EconomySummaryView? result = await client.GetCitySummaryAsync(
                cityId: cityId,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: summary,
                actual: result);
            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.EndsWith(
                expectedEndString: $"/api/economy/Budget/cities/{cityId}/summary",
                actualString: request.RequestUri,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public async Task ClassicCityEconomyApiClientGetCityOperationalBudgetPressureAsync_WhenCalled_ReturnsPressure()
        {
            var cityId = Guid.Parse("bbfd2c43-cbe4-43d7-b414-c780427d847d");
            CityOperationalBudgetPressureView pressure = new(
                CityId: cityId,
                EffectiveTickId: 15,
                EffectivePhase: "Budget",
                EffectiveAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 8,
                    hour: 14,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                UnitKind: "Currency",
                UnitCode: "CR",
                UnitDisplayName: "Credits",
                UnitSymbol: "C",
                Balance: 120000m,
                TotalCityExpenses: 45000m,
                MunicipalOperationsExpenses: 15000m,
                InfrastructureOperationsExpenses: 12000m,
                EmergencyOperationsExpenses: 3000m,
                GeneralAvailableAmount: 40000m,
                OperationsAvailableAmount: 22000m,
                InfrastructureAvailableAmount: 18000m,
                HealthcareAvailableAmount: 9000m,
                GeneralAuthorizationLevel: "Standard",
                OperationsAuthorizationLevel: "Standard",
                InfrastructureAuthorizationLevel: "Constrained",
                HealthcareAuthorizationLevel: "Protected",
                LastMunicipalExpenseAtUtc: "2048-06-08T13:55:00Z",
                PressureIndex: 0.36m);
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    CreateJsonResponse(
                        statusCode: HttpStatusCode.OK,
                        payload: pressure))
            };
            IClassicCityEconomyApiClient client = CreateClassicCityEconomyApiClient(CreateHttpClient(handler));

            CityOperationalBudgetPressureView? result = await client.GetCityOperationalBudgetPressureAsync(
                cityId: cityId,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: pressure,
                actual: result);
        }

        [Fact]
        public async Task ClassicCityEconomyApiClientInitializeCityEconomyAsync_WhenCalled_PostsRequestBody()
        {
            var cityId = Guid.Parse("0f37db85-8d2d-4227-b0e3-d65188cca754");
            CityEconomyBootstrapResultView bootstrap = new(
                CityId: cityId,
                BudgetCreated: true,
                CreatedAllocations: 4,
                CreatedBusinesses: 18,
                UnitKind: "Currency",
                UnitCode: "CR",
                UnitDisplayName: "Credits",
                UnitSymbol: "C");
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    CreateJsonResponse(
                        statusCode: HttpStatusCode.OK,
                        payload: bootstrap))
            };
            IClassicCityEconomyApiClient client = CreateClassicCityEconomyApiClient(CreateHttpClient(handler));

            CityEconomyBootstrapResultView result = await client.InitializeCityEconomyAsync(
                cityId: cityId,
                request: new InitializeCityEconomyRequest(
                    ScenarioKey: "classic-city",
                    EconomyProfile: "Balanced",
                    CreatedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 6,
                        day: 8,
                        hour: 14,
                        minute: 10,
                        second: 0,
                        offset: TimeSpan.Zero)),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: bootstrap,
                actual: result);
            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.Equal(
                expected: HttpMethod.Post,
                actual: request.Method);
            Assert.EndsWith(
                expectedEndString: $"/api/economy/Budget/cities/{cityId}/bootstrap",
                actualString: request.RequestUri,
                comparisonType: StringComparison.Ordinal);
            Assert.Contains(
                expectedSubstring: "\"economyProfile\":\"Balanced\"",
                actualString: request.Body,
                comparisonType: StringComparison.Ordinal);
            Assert.Contains(
                expectedSubstring: "\"scenarioKey\":\"classic-city\"",
                actualString: request.Body,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public async Task EconomyApiClientHealthAsync_WhenReadyProbeFails_ReturnsFalse()
        {
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(CreateEmptyResponse(HttpStatusCode.ServiceUnavailable))
            };
            IEconomyApiClient client = CreateEconomyApiClient(CreateHttpClient(handler));

            bool result = await client.HealthAsync(CancellationToken.None);

            Assert.False(result);
        }
    }
}
