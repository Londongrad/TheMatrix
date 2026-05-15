using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Requests;
using Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Requests;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Views;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.Controllers.SimulationCore.Scenarios.ClassicCity;

public sealed class CitiesControllerTests
{
    [Fact]
    public async Task GetDashboard_WhenEconomySummaryIsAvailable_AppendsEconomyMetrics()
    {
        Guid cityId = Guid.Parse("1a87a441-fdb8-4224-9d27-7f48d4f41516");
        CityPopulationDashboardDto dashboard = CreateCityPopulationDashboardDto(cityId);
        var populationClient = new RecordingPopulationApiClient
        {
            DashboardResult = dashboard
        };
        var economyClient = new RecordingEconomyApiClient
        {
            CitySummaryResult = CreateEconomySummaryView()
        };
        var controller = CreateCitiesController(
            populationClient: populationClient,
            economyClient: economyClient);

        ActionResult<CityPopulationDashboardDto> actionResult = await controller.GetDashboard(cityId, CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        CityPopulationDashboardDto view = Assert.IsType<CityPopulationDashboardDto>(ok.Value);
        Assert.Equal(cityId, populationClient.LastDashboardCityId);
        Assert.Equal(cityId, economyClient.LastCitySummaryCityId);
        Assert.Equal(dashboard.Metrics.Count + 6, view.Metrics.Count);
        Assert.Contains(view.Metrics, x => x.Key == "economyBudgetBalance");
        Assert.Contains(view.Metrics, x => x.Key == "economyGrossPayroll");
        Assert.Contains(view.Metrics, x => x.Key == "economyIncomeTax");
        Assert.Contains(view.Metrics, x => x.Key == "economySalesTax");
        Assert.Contains(view.Metrics, x => x.Key == "economyRetailTurnover");
        Assert.Contains(view.Metrics, x => x.Key == "economyCityExpenses");
        Assert.Equal(dashboard.RecentEvents, view.RecentEvents);
    }

    [Fact]
    public async Task GetDashboard_WhenEconomyFails_ReturnsPopulationDashboardOnly()
    {
        Guid cityId = Guid.Parse("31893803-e632-4cd7-b08d-8ff784c79206");
        CityPopulationDashboardDto dashboard = CreateCityPopulationDashboardDto(cityId);
        var populationClient = new RecordingPopulationApiClient
        {
            DashboardResult = dashboard
        };
        var economyClient = new RecordingEconomyApiClient
        {
            GetCitySummaryException = CreateDownstreamServiceException(
                statusCode: System.Net.HttpStatusCode.BadGateway,
                serviceName: "economy")
        };
        var controller = CreateCitiesController(
            populationClient: populationClient,
            economyClient: economyClient);

        ActionResult<CityPopulationDashboardDto> actionResult = await controller.GetDashboard(cityId, CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        CityPopulationDashboardDto view = Assert.IsType<CityPopulationDashboardDto>(ok.Value);
        Assert.Equal(dashboard.Metrics, view.Metrics);
        Assert.Equal(dashboard.RecentEvents, view.RecentEvents);
    }

    [Fact]
    public async Task GetDistrictInfrastructure_WhenAnySliceIsMissing_ReturnsNotFound()
    {
        Guid cityId = Guid.Parse("e6bd2dfc-a93f-4aea-abdf-556308c2a0f4");
        var environmentalConditionsClient = new RecordingEnvironmentalConditionsApiClient
        {
            ReturnSanitationNull = true
        };
        var controller = CreateCitiesController(environmentalConditionsClient: environmentalConditionsClient);

        ActionResult<CityDistrictInfrastructureView> actionResult = await controller.GetDistrictInfrastructure(cityId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(actionResult.Result);
    }

    [Fact]
    public async Task GetDistrictInfrastructure_WhenAllSlicesExist_ReturnsCombinedView()
    {
        Guid cityId = Guid.Parse("b177e44d-c0ae-4e18-ae27-c4f28fc73fb9");
        var heating = CreateCityDistrictHeatingConditionsView(cityId);
        var water = CreateCityDistrictWaterDistributionConditionsView(cityId);
        var power = CreateCityDistrictPowerDistributionConditionsView(cityId);
        var sanitation = CreateCityDistrictSanitationConditionsView(cityId);
        var utilityIncidents = CreateCityDistrictUtilityIncidentConditionsView(cityId);
        var environmentalConditionsClient = new RecordingEnvironmentalConditionsApiClient
        {
            HeatingResult = heating,
            WaterResult = water,
            PowerResult = power,
            SanitationResult = sanitation,
            UtilityIncidentResult = utilityIncidents
        };
        var controller = CreateCitiesController(environmentalConditionsClient: environmentalConditionsClient);
        DateTimeOffset beforeUtc = DateTimeOffset.UtcNow;

        ActionResult<CityDistrictInfrastructureView> actionResult = await controller.GetDistrictInfrastructure(cityId, CancellationToken.None);

        DateTimeOffset afterUtc = DateTimeOffset.UtcNow;
        OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        CityDistrictInfrastructureView view = Assert.IsType<CityDistrictInfrastructureView>(ok.Value);
        Assert.Equal(cityId, view.CityId);
        Assert.Same(heating, view.Heating);
        Assert.Same(water, view.WaterDistribution);
        Assert.Same(power, view.PowerDistribution);
        Assert.Same(sanitation, view.Sanitation);
        Assert.Same(utilityIncidents, view.UtilityIncidents);
        Assert.InRange(view.GeneratedAtUtc, beforeUtc, afterUtc);
    }

    [Fact]
    public async Task DispatchDistrictUtilityResponse_WhenDownstreamReturnsConflict_MapsConflictBody()
    {
        Guid cityId = Guid.Parse("c7a3da3e-2671-405c-a049-f4fead610ab0");
        CityUtilityIncidentStatusView conflictView = CreateCityUtilityIncidentStatusView(cityId, statusIntensity: "Critical");
        var environmentalConditionsClient = new RecordingEnvironmentalConditionsApiClient
        {
            DispatchException = CreateConflictException(conflictView, serviceName: "simulationsystems")
        };
        var controller = CreateCitiesController(environmentalConditionsClient: environmentalConditionsClient);

        ActionResult<CityUtilityIncidentStatusView> actionResult = await controller.DispatchDistrictUtilityResponse(
            cityId: cityId,
            request: new DispatchCityUtilityIncidentResponseRequest(
                Focus: "CriticalInfrastructure",
                Intensity: "Critical"),
            cancellationToken: CancellationToken.None);

        ConflictObjectResult conflict = Assert.IsType<ConflictObjectResult>(actionResult.Result);
        CityUtilityIncidentStatusView view = Assert.IsType<CityUtilityIncidentStatusView>(conflict.Value);
        Assert.Equal(cityId, view.CityId);
        Assert.Equal("Critical", view.AppliedIntensity);
        Assert.Equal("Critical", view.BudgetAuthorizedIntensity);
    }

    [Fact]
    public async Task DispatchDistrictResupply_WhenDownstreamReturnsConflict_MapsConflictBody()
    {
        Guid cityId = Guid.Parse("2023c998-6be5-44ef-8eb6-bfa942fd5d78");
        DispatchCityResupplyView conflictView = CreateDispatchCityResupplyView(
            cityId: cityId,
            requestedIntensity: "High",
            appliedIntensity: "High");
        var stockpilesClient = new RecordingStockpilesApiClient
        {
            DispatchException = CreateConflictException(conflictView, serviceName: "resources")
        };
        var controller = CreateCitiesController(stockpilesClient: stockpilesClient);

        ActionResult<DispatchCityResupplyView> actionResult = await controller.DispatchDistrictResupply(
            cityId: cityId,
            request: new DispatchCityResupplyRequest(
                Focus: ResupplyFocus.EmergencyWater,
                Intensity: ResupplyIntensity.High),
            cancellationToken: CancellationToken.None);

        ConflictObjectResult conflict = Assert.IsType<ConflictObjectResult>(actionResult.Result);
        DispatchCityResupplyView view = Assert.IsType<DispatchCityResupplyView>(conflict.Value);
        Assert.Equal(cityId, view.CityId);
        Assert.Equal("High", view.RequestedIntensity);
        Assert.Equal("High", view.AppliedIntensity);
    }

    [Fact]
    public async Task GetResidentsPage_WhenCalled_UsesSimulationClockDate()
    {
        Guid cityId = Guid.Parse("fdac2b90-c8c4-4736-acd0-b5e9d02be1bf");
        DateTimeOffset simTimeUtc = new(2048, 6, 7, 23, 45, 0, TimeSpan.Zero);
        PagedResult<PersonDto> page = CreateResidentsPageResult();
        var simulationClient = new RecordingSimulationApiClient
        {
            ClockResult = CreateSimulationClockView(simulationId: cityId, simTimeUtc: simTimeUtc)
        };
        var populationClient = new RecordingPopulationApiClient
        {
            ResidentsPageResult = page
        };
        var controller = CreateCitiesController(
            simulationClient: simulationClient,
            populationClient: populationClient);

        ActionResult<PagedResult<PersonDto>> actionResult = await controller.GetResidentsPage(
            cityId: cityId,
            pageNumber: 3,
            pageSize: 40,
            cancellationToken: CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        PagedResult<PersonDto> view = Assert.IsType<PagedResult<PersonDto>>(ok.Value);
        Assert.Same(page, view);
        Assert.Equal(DateOnly.FromDateTime(simTimeUtc.UtcDateTime), populationClient.LastResidentsPageCurrentDate);
        Assert.Equal(3, populationClient.LastResidentsPageNumber);
        Assert.Equal(40, populationClient.LastResidentsPageSize);
        Assert.Equal(cityId, populationClient.LastResidentsPageCityId);
    }

    [Fact]
    public async Task GetResidentDetails_WhenCalled_UsesSimulationClockDate()
    {
        Guid cityId = Guid.Parse("287886e2-296f-4c66-82b7-f9ff0d2c7d94");
        Guid personId = Guid.Parse("2ebfc973-bffe-4a22-9862-a62f6ee90ac1");
        DateTimeOffset simTimeUtc = new(2048, 6, 8, 6, 10, 0, TimeSpan.Zero);
        CityResidentDetailsDto resident = CreateCityResidentDetailsDto(personId);
        var simulationClient = new RecordingSimulationApiClient
        {
            ClockResult = CreateSimulationClockView(simulationId: cityId, simTimeUtc: simTimeUtc)
        };
        var populationClient = new RecordingPopulationApiClient
        {
            ResidentDetailsResult = resident
        };
        var controller = CreateCitiesController(
            simulationClient: simulationClient,
            populationClient: populationClient);

        ActionResult<CityResidentDetailsDto> actionResult = await controller.GetResidentDetails(
            cityId: cityId,
            personId: personId,
            cancellationToken: CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        CityResidentDetailsDto view = Assert.IsType<CityResidentDetailsDto>(ok.Value);
        Assert.Same(resident, view);
        Assert.Equal(DateOnly.FromDateTime(simTimeUtc.UtcDateTime), populationClient.LastResidentDetailsCurrentDate);
        Assert.Equal(cityId, populationClient.LastResidentDetailsCityId);
        Assert.Equal(personId, populationClient.LastResidentDetailsPersonId);
    }
}
