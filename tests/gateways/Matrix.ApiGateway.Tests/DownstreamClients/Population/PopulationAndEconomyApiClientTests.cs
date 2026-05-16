using System.Net;
using Matrix.ApiGateway.DownstreamClients.Common.Exceptions;
using Matrix.ApiGateway.DownstreamClients.Economy;
using Matrix.ApiGateway.DownstreamClients.Population.People;
using Matrix.ApiGateway.DownstreamClients.Population.Person;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Contracts.Budget.Requests;
using Matrix.Economy.Contracts.Budget.Views;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Xunit;
using static Matrix.ApiGateway.Tests.Http.HttpClientTestSupport;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.DownstreamClients.Population;

public sealed class PopulationAndEconomyApiClientTests
{
    [Fact]
    public async Task PopulationApiClientGetCityResidentsPageAsync_WhenCalled_UsesDateAndPaginationQuery()
    {
        Guid cityId = Guid.Parse("18f5a5d3-cb51-4626-a98e-56450b5657fc");
        DateOnly currentDate = new(2048, 6, 8);
        PagedResult<PersonDto> page = CreateResidentsPageResult();
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, page))
        };
        IPopulationApiClient client = CreatePopulationApiClient(CreateHttpClient(handler));

        PagedResult<PersonDto> result = await client.GetCityResidentsPageAsync(
            cityId: cityId,
            currentDate: currentDate,
            pageNumber: 3,
            pageSize: 40,
            cancellationToken: CancellationToken.None);

        Assert.Equal(page.Items, result.Items);
        RecordedRequest request = Assert.Single(handler.Requests);
        Assert.EndsWith(
            $"/api/population/cities/{cityId}/residents?currentDate=2048-06-08&pageNumber=3&pageSize=40",
            request.RequestUri,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task PopulationApiClientGetCitizensPageAsync_WhenBodyIsEmpty_ThrowsJsonException()
    {
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateEmptyResponse(HttpStatusCode.OK))
        };
        IPopulationApiClient client = CreatePopulationApiClient(CreateHttpClient(handler));

        System.Text.Json.JsonException exception = await Assert.ThrowsAsync<System.Text.Json.JsonException>(
            () => client.GetCitizensPageAsync(pageNumber: 2, pageSize: 15, cancellationToken: CancellationToken.None));

        Assert.Contains("JSON", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PopulationApiClientGetCityResidentDetailsAsync_WhenJsonIsMalformed_ThrowsDownstreamServiceException()
    {
        Guid cityId = Guid.Parse("2d6fd248-dcda-40bb-b402-ea0bcae2d69f");
        Guid personId = Guid.Parse("a07d5b2e-fde8-45c5-af40-9d484fe60c47");
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateStringResponse(HttpStatusCode.OK, "{bad-json"))
        };
        IPopulationApiClient client = CreatePopulationApiClient(CreateHttpClient(handler));

        DownstreamServiceException exception = await Assert.ThrowsAsync<DownstreamServiceException>(
            () => client.GetCityResidentDetailsAsync(
                cityId: cityId,
                personId: personId,
                currentDate: new DateOnly(2048, 6, 8),
                cancellationToken: CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadGateway, exception.StatusCode);
        Assert.Contains("InvalidDownstreamJson", exception.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PersonApiClientKillAsync_WhenResponseIsSuccessful_PostsExpectedUrl()
    {
        Guid personId = Guid.Parse("1039b5aa-a305-4b58-9aad-99d15b5f4f39");
        PersonDto person = Assert.Single(CreateResidentsPageResult(personId).Items);
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, person))
        };
        IPersonApiClient client = CreatePersonApiClient(CreateHttpClient(handler));

        PersonDto result = await client.KillAsync(personId, CancellationToken.None);

        Assert.Equal(person, result);
        RecordedRequest request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.EndsWith($"/api/person/{personId}/kill", request.RequestUri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EconomyApiClientGetCitySummaryAsync_WhenResponseIsSuccessful_ReturnsSummary()
    {
        Guid cityId = Guid.Parse("e0d3d66c-654f-4af5-aa7e-32c78d5580be");
        EconomySummaryView summary = CreateEconomySummaryView();
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, summary))
        };
        IEconomyApiClient client = CreateEconomyApiClient(CreateHttpClient(handler));

        EconomySummaryView? result = await client.GetCitySummaryAsync(cityId, CancellationToken.None);

        Assert.Equal(summary, result);
        RecordedRequest request = Assert.Single(handler.Requests);
        Assert.EndsWith($"/api/economy/Budget/cities/{cityId}/summary", request.RequestUri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EconomyApiClientGetCityOperationalBudgetPressureAsync_WhenCalled_ReturnsPressure()
    {
        Guid cityId = Guid.Parse("bbfd2c43-cbe4-43d7-b414-c780427d847d");
        CityOperationalBudgetPressureView pressure = new(
            CityId: cityId,
            EffectiveTickId: 15,
            EffectivePhase: "Budget",
            EffectiveAtUtc: new DateTimeOffset(2048, 6, 8, 14, 0, 0, TimeSpan.Zero),
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
            OnSendAsync = (_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, pressure))
        };
        IEconomyApiClient client = CreateEconomyApiClient(CreateHttpClient(handler));

        CityOperationalBudgetPressureView? result = await client.GetCityOperationalBudgetPressureAsync(cityId, CancellationToken.None);

        Assert.Equal(pressure, result);
    }

    [Fact]
    public async Task EconomyApiClientInitializeCityEconomyAsync_WhenCalled_PostsRequestBody()
    {
        Guid cityId = Guid.Parse("0f37db85-8d2d-4227-b0e3-d65188cca754");
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
            OnSendAsync = (_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, bootstrap))
        };
        IEconomyApiClient client = CreateEconomyApiClient(CreateHttpClient(handler));

        CityEconomyBootstrapResultView result = await client.InitializeCityEconomyAsync(
            cityId: cityId,
            request: new InitializeCityEconomyRequest(
                SimulationKind: "ClassicCity",
                EconomyProfile: "Balanced",
                CreatedAtUtc: new DateTimeOffset(2048, 6, 8, 14, 10, 0, TimeSpan.Zero)),
            cancellationToken: CancellationToken.None);

        Assert.Equal(bootstrap, result);
        RecordedRequest request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.EndsWith($"/api/economy/Budget/cities/{cityId}/bootstrap", request.RequestUri, StringComparison.Ordinal);
        Assert.Contains("\"economyProfile\":\"Balanced\"", request.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EconomyApiClientHealthAsync_WhenReadyProbeFails_ReturnsFalse()
    {
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateEmptyResponse(HttpStatusCode.ServiceUnavailable))
        };
        IEconomyApiClient client = CreateEconomyApiClient(CreateHttpClient(handler));

        bool result = await client.HealthAsync(CancellationToken.None);

        Assert.False(result);
    }
}
