using System.Net;
using System.Text.Json;
using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.DownstreamClients.Common.Exceptions;
using Matrix.ApiGateway.DownstreamClients.Economy.Scenarios.ClassicCity;
using Matrix.ApiGateway.DownstreamClients.Population.Scenarios.ClassicCity;
using Matrix.ApiGateway.DownstreamClients.Resources.Scenarios.ClassicCity.Stockpiles;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Trips;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Simulation;
using Matrix.ApiGateway.DownstreamClients.SimulationSystems.Scenarios.ClassicCity.EnvironmentalConditions;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Contracts.Budget.Views;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Requests;
using Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Requests;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Topology.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Trips.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Weather.Views;
using Matrix.SimulationCore.Contracts.Simulation.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Heating.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.PowerDistribution.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Sanitation.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Requests;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.WaterDistribution.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.SimulationCore.Scenarios.ClassicCity.Cities
{
    [Authorize]
    [ApiController]
    [Route("api/scenarios/classic-city/cities")]
    public sealed class CitiesController(
        ICitiesApiClient citiesClient,
        ITripsApiClient tripsClient,
        ISimulationApiClient simulationClient,
        IClassicCityEconomyApiClient economyClient,
        IClassicCityPopulationApiClient populationClient,
        IStockpilesApiClient stockpilesClient,
        IEnvironmentalConditionsApiClient environmentalConditionsClient,
        ICityProvisioningService cityProvisioningService,
        TimeProvider timeProvider,
        ILogger<CitiesController> logger) : ControllerBase
    {
        private readonly ICitiesApiClient _citiesClient = citiesClient;
        private readonly ICityProvisioningService _cityProvisioningService = cityProvisioningService;
        private readonly IClassicCityEconomyApiClient _economyClient = economyClient;

        private readonly IEnvironmentalConditionsApiClient _environmentalConditionsClient =
            environmentalConditionsClient;

        private readonly ILogger<CitiesController> _logger = logger;
        private readonly IClassicCityPopulationApiClient _populationClient = populationClient;
        private readonly ISimulationApiClient _simulationClient = simulationClient;
        private readonly IStockpilesApiClient _stockpilesClient = stockpilesClient;
        private readonly TimeProvider _timeProvider = timeProvider;
        private readonly ITripsApiClient _tripsClient = tripsClient;

        [HttpPost]
        public async Task<ActionResult<CityProvisioningView>> Create(
            [FromBody] CreateCityRequestDto request,
            CancellationToken cancellationToken)
        {
            CityProvisioningView created = await _cityProvisioningService.CreateCityAsync(
                request: request,
                cancellationToken: cancellationToken);

            return Created(
                uri: $"/api/cities/{created.CityId}",
                value: created);
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CityListItemView>>> List(
            [FromQuery] bool includeArchived,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityListItemView> cities = await _citiesClient.ListCitiesAsync(
                includeArchived: includeArchived,
                cancellationToken: cancellationToken);

            return Ok(cities);
        }

        [HttpGet("provisioning")]
        public async Task<ActionResult<IReadOnlyList<CityListItemView>>> ListProvisioning(
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityListItemView> cities = await _citiesClient.ListProvisioningCitiesAsync(
                cancellationToken: cancellationToken);

            return Ok(cities);
        }

        [HttpGet("{cityId:guid}")]
        public async Task<ActionResult<CityView>> Get(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CityView city = await _citiesClient.GetCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            return Ok(city);
        }

        [HttpGet("{cityId:guid}/population-summary")]
        public async Task<ActionResult<CityPopulationSummaryDto>> GetPopulationSummary(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CityPopulationSummaryDto summary = await _populationClient.GetCityPopulationSummaryAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            return Ok(summary);
        }

        [HttpGet("{cityId:guid}/map")]
        public async Task<ActionResult<CityMapTopologyView>> GetMap(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CityMapTopologyView map = await _citiesClient.GetMapAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            return Ok(map);
        }

        [HttpGet("{cityId:guid}/trips/active")]
        public async Task<ActionResult<IReadOnlyList<CityActiveTripView>>> GetActiveTrips(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityActiveTripView> trips = await _tripsClient.GetActiveTripsAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            return Ok(trips);
        }

        [HttpGet("{cityId:guid}/dashboard")]
        public async Task<ActionResult<CityPopulationDashboardDto>> GetDashboard(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CityPopulationDashboardDto dashboard = await _populationClient.GetCityPopulationDashboardAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            List<CityPopulationDashboardMetricDto> metrics = [.. dashboard.Metrics];

            try
            {
                EconomySummaryView? economySummary = await _economyClient.GetCitySummaryAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);

                if (economySummary is not null)
                    metrics.AddRange(BuildEconomyMetrics(economySummary));
            }
            catch (DownstreamServiceException exception)
            {
                _logger.LogWarning(
                    exception: exception,
                    message:
                    "Skipped economy metrics for classic city dashboard because Economy returned status {StatusCode} for cityId={CityId}.",
                    (int)exception.StatusCode,
                    cityId);
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(
                    exception: exception,
                    message: "Failed to attach economy metrics to classic city dashboard for cityId={CityId}.",
                    cityId);
            }

            return Ok(
                new CityPopulationDashboardDto(
                    CityId: dashboard.CityId,
                    CurrentDate: dashboard.CurrentDate,
                    GeneratedAtUtc: dashboard.GeneratedAtUtc,
                    Metrics: metrics,
                    RecentEvents: dashboard.RecentEvents));
        }

        [HttpGet("{cityId:guid}/infrastructure/districts")]
        public async Task<ActionResult<CityDistrictInfrastructureView>> GetDistrictInfrastructure(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CityDistrictHeatingConditionsView? heating =
                await _environmentalConditionsClient.GetCityDistrictHeatingConditionsAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);
            CityDistrictWaterDistributionConditionsView? water =
                await _environmentalConditionsClient.GetCityDistrictWaterDistributionConditionsAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);
            CityDistrictPowerDistributionConditionsView? power =
                await _environmentalConditionsClient.GetCityDistrictPowerDistributionConditionsAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);
            CityDistrictSanitationConditionsView? sanitation =
                await _environmentalConditionsClient.GetCityDistrictSanitationConditionsAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);
            CityDistrictUtilityIncidentConditionsView? incidents =
                await _environmentalConditionsClient.GetCityDistrictUtilityIncidentConditionsAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);

            if (heating is null || water is null || power is null || sanitation is null || incidents is null)
                return NotFound();

            return Ok(
                new CityDistrictInfrastructureView(
                    CityId: cityId,
                    GeneratedAtUtc: _timeProvider.GetUtcNow(),
                    Heating: heating,
                    WaterDistribution: water,
                    PowerDistribution: power,
                    Sanitation: sanitation,
                    UtilityIncidents: incidents));
        }

        [HttpPost("{cityId:guid}/operator/utility-response")]
        public async Task<ActionResult<CityUtilityIncidentStatusView>> DispatchDistrictUtilityResponse(
            [FromRoute] Guid cityId,
            [FromBody] DispatchCityUtilityIncidentResponseRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                CityUtilityIncidentStatusView view =
                    await _environmentalConditionsClient.DispatchCityUtilityIncidentResponseAsync(
                        cityId: cityId,
                        request: request,
                        cancellationToken: cancellationToken);

                return Ok(view);
            }
            catch (DownstreamServiceException exception) when (exception.StatusCode == HttpStatusCode.Conflict)
            {
                CityUtilityIncidentStatusView? view = TryDeserializeConflict<CityUtilityIncidentStatusView>(exception);
                return view is null
                    ? Conflict()
                    : Conflict(view);
            }
        }

        [HttpPost("{cityId:guid}/operator/resupply")]
        public async Task<ActionResult<DispatchCityResupplyView>> DispatchDistrictResupply(
            [FromRoute] Guid cityId,
            [FromBody] DispatchCityResupplyRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                DispatchCityResupplyView view = await _stockpilesClient.DispatchCityResupplyAsync(
                    cityId: cityId,
                    request: request,
                    cancellationToken: cancellationToken);

                return Ok(view);
            }
            catch (DownstreamServiceException exception) when (exception.StatusCode == HttpStatusCode.Conflict)
            {
                DispatchCityResupplyView? view = TryDeserializeConflict<DispatchCityResupplyView>(exception);
                return view is null
                    ? Conflict()
                    : Conflict(view);
            }
        }

        [HttpGet("{cityId:guid}/residents")]
        public async Task<ActionResult<PagedResult<PersonDto>>> GetResidentsPage(
            [FromRoute] Guid cityId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            SimulationClockView clock = await _simulationClient.GetClockAsync(
                simulationId: cityId,
                cancellationToken: cancellationToken);

            var currentDate = DateOnly.FromDateTime(clock.SimTimeUtc.UtcDateTime);

            PagedResult<PersonDto> residents = await _populationClient.GetCityResidentsPageAsync(
                cityId: cityId,
                currentDate: currentDate,
                pageNumber: pageNumber,
                pageSize: pageSize,
                cancellationToken: cancellationToken);

            return Ok(residents);
        }

        [HttpGet("{cityId:guid}/residents/{personId:guid}")]
        public async Task<ActionResult<CityResidentDetailsDto>> GetResidentDetails(
            [FromRoute] Guid cityId,
            [FromRoute] Guid personId,
            CancellationToken cancellationToken = default)
        {
            SimulationClockView clock = await _simulationClient.GetClockAsync(
                simulationId: cityId,
                cancellationToken: cancellationToken);

            var currentDate = DateOnly.FromDateTime(clock.SimTimeUtc.UtcDateTime);

            CityResidentDetailsDto resident = await _populationClient.GetCityResidentDetailsAsync(
                cityId: cityId,
                personId: personId,
                currentDate: currentDate,
                cancellationToken: cancellationToken);

            return Ok(resident);
        }

        [HttpGet("{cityId:guid}/employment/catalog")]
        public async Task<ActionResult<CityEmploymentCatalogDto>> GetEmploymentCatalog(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken = default)
        {
            CityEmploymentCatalogDto catalog = await _populationClient.GetCityEmploymentCatalogAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            return Ok(catalog);
        }

        [HttpGet("{cityId:guid}/education/catalog")]
        public async Task<ActionResult<CityEducationCatalogDto>> GetEducationCatalog(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken = default)
        {
            CityEducationCatalogDto catalog = await _populationClient.GetCityEducationCatalogAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            return Ok(catalog);
        }

        [HttpPost("{cityId:guid}/employment/hire")]
        public async Task<ActionResult<CityEmploymentOperationResultDto>> HireResident(
            [FromRoute] Guid cityId,
            [FromBody] CityEmploymentOperationRequestDto request,
            CancellationToken cancellationToken = default)
        {
            SimulationClockView clock = await _simulationClient.GetClockAsync(
                simulationId: cityId,
                cancellationToken: cancellationToken);

            var currentDate = DateOnly.FromDateTime(clock.SimTimeUtc.UtcDateTime);

            CityEmploymentOperationResultDto result = await _populationClient.HireCityResidentAsync(
                cityId: cityId,
                request: new CityEmploymentOperationRequest(
                    ResidentId: request.ResidentId,
                    JobTitle: request.JobTitle,
                    WorkplaceId: request.WorkplaceId,
                    CurrentDate: currentDate),
                cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpPost("{cityId:guid}/employment/fire")]
        public async Task<ActionResult<CityEmploymentOperationResultDto>> FireResident(
            [FromRoute] Guid cityId,
            [FromBody] CityEmploymentOperationRequestDto request,
            CancellationToken cancellationToken = default)
        {
            SimulationClockView clock = await _simulationClient.GetClockAsync(
                simulationId: cityId,
                cancellationToken: cancellationToken);

            var currentDate = DateOnly.FromDateTime(clock.SimTimeUtc.UtcDateTime);

            CityEmploymentOperationResultDto result = await _populationClient.FireCityResidentAsync(
                cityId: cityId,
                request: new CityEmploymentOperationRequest(
                    ResidentId: request.ResidentId,
                    JobTitle: request.JobTitle,
                    WorkplaceId: request.WorkplaceId,
                    CurrentDate: currentDate),
                cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpPost("{cityId:guid}/employment/retire")]
        public async Task<ActionResult<CityEmploymentOperationResultDto>> RetireResident(
            [FromRoute] Guid cityId,
            [FromBody] CityEmploymentOperationRequestDto request,
            CancellationToken cancellationToken = default)
        {
            SimulationClockView clock = await _simulationClient.GetClockAsync(
                simulationId: cityId,
                cancellationToken: cancellationToken);

            var currentDate = DateOnly.FromDateTime(clock.SimTimeUtc.UtcDateTime);

            CityEmploymentOperationResultDto result = await _populationClient.RetireCityResidentAsync(
                cityId: cityId,
                request: new CityEmploymentOperationRequest(
                    ResidentId: request.ResidentId,
                    JobTitle: request.JobTitle,
                    WorkplaceId: request.WorkplaceId,
                    CurrentDate: currentDate),
                cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpPost("{cityId:guid}/education/enroll")]
        public async Task<ActionResult<CityEducationOperationResultDto>> EnrollResident(
            [FromRoute] Guid cityId,
            [FromBody] CityEducationOperationRequestDto request,
            CancellationToken cancellationToken = default)
        {
            SimulationClockView clock = await _simulationClient.GetClockAsync(
                simulationId: cityId,
                cancellationToken: cancellationToken);

            var currentDate = DateOnly.FromDateTime(clock.SimTimeUtc.UtcDateTime);

            CityEducationOperationResultDto result = await _populationClient.EnrollCityResidentAsync(
                cityId: cityId,
                request: new CityEducationOperationRequest(
                    ResidentId: request.ResidentId,
                    TargetEducationLevel: request.TargetEducationLevel,
                    InstitutionId: request.InstitutionId,
                    CurrentDate: currentDate),
                cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpPost("{cityId:guid}/education/graduate")]
        public async Task<ActionResult<CityEducationOperationResultDto>> GraduateResident(
            [FromRoute] Guid cityId,
            [FromBody] CityEducationOperationRequestDto request,
            CancellationToken cancellationToken = default)
        {
            SimulationClockView clock = await _simulationClient.GetClockAsync(
                simulationId: cityId,
                cancellationToken: cancellationToken);

            var currentDate = DateOnly.FromDateTime(clock.SimTimeUtc.UtcDateTime);

            CityEducationOperationResultDto result = await _populationClient.GraduateCityResidentAsync(
                cityId: cityId,
                request: new CityEducationOperationRequest(
                    ResidentId: request.ResidentId,
                    TargetEducationLevel: request.TargetEducationLevel,
                    InstitutionId: request.InstitutionId,
                    CurrentDate: currentDate),
                cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpPost("{cityId:guid}/education/withdraw")]
        public async Task<ActionResult<CityEducationOperationResultDto>> WithdrawResident(
            [FromRoute] Guid cityId,
            [FromBody] CityEducationOperationRequestDto request,
            CancellationToken cancellationToken = default)
        {
            SimulationClockView clock = await _simulationClient.GetClockAsync(
                simulationId: cityId,
                cancellationToken: cancellationToken);

            var currentDate = DateOnly.FromDateTime(clock.SimTimeUtc.UtcDateTime);

            CityEducationOperationResultDto result = await _populationClient.WithdrawCityResidentFromStudyAsync(
                cityId: cityId,
                request: new CityEducationOperationRequest(
                    ResidentId: request.ResidentId,
                    TargetEducationLevel: request.TargetEducationLevel,
                    InstitutionId: request.InstitutionId,
                    CurrentDate: currentDate),
                cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpPost("{cityId:guid}/civil-registry/marriages")]
        public async Task<ActionResult<CityCivilRegistryOperationResultDto>> RegisterMarriage(
            [FromRoute] Guid cityId,
            [FromBody] CityCivilRegistryOperationRequestDto request,
            CancellationToken cancellationToken = default)
        {
            SimulationClockView clock = await _simulationClient.GetClockAsync(
                simulationId: cityId,
                cancellationToken: cancellationToken);

            var currentDate = DateOnly.FromDateTime(clock.SimTimeUtc.UtcDateTime);

            CityCivilRegistryOperationResultDto result = await _populationClient.RegisterCityMarriageAsync(
                cityId: cityId,
                request: new CityCivilRegistryOperationRequest(
                    FirstResidentId: request.FirstResidentId,
                    SecondResidentId: request.SecondResidentId,
                    CurrentDate: currentDate),
                cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpPost("{cityId:guid}/civil-registry/divorces")]
        public async Task<ActionResult<CityCivilRegistryOperationResultDto>> RegisterDivorce(
            [FromRoute] Guid cityId,
            [FromBody] CityCivilRegistryOperationRequestDto request,
            CancellationToken cancellationToken = default)
        {
            SimulationClockView clock = await _simulationClient.GetClockAsync(
                simulationId: cityId,
                cancellationToken: cancellationToken);

            var currentDate = DateOnly.FromDateTime(clock.SimTimeUtc.UtcDateTime);

            CityCivilRegistryOperationResultDto result = await _populationClient.RegisterCityDivorceAsync(
                cityId: cityId,
                request: new CityCivilRegistryOperationRequest(
                    FirstResidentId: request.FirstResidentId,
                    SecondResidentId: request.SecondResidentId,
                    CurrentDate: currentDate),
                cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpGet("{cityId:guid}/provisioning")]
        public async Task<ActionResult<CityProvisioningStatusView>> GetProvisioning(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CityProvisioningStatusView provisioning = await _citiesClient.GetProvisioningStatusAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            return Ok(provisioning);
        }

        [HttpGet("{cityId:guid}/weather")]
        public async Task<ActionResult<CityWeatherView>> GetWeather(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CityWeatherView weather = await _citiesClient.GetWeatherAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            return Ok(weather);
        }

        [HttpPost("{cityId:guid}/population-bootstrap/retry")]
        public async Task<ActionResult<CityProvisioningView>> RetryPopulationBootstrap(
            [FromRoute] Guid cityId,
            [FromBody] RetryPopulationBootstrapRequestDto? request,
            CancellationToken cancellationToken)
        {
            CityProvisioningView provisioning = await _cityProvisioningService.RetryPopulationBootstrapAsync(
                cityId: cityId,
                plannedPeopleCountOverride: request?.PlannedPeopleCountOverride,
                cancellationToken: cancellationToken);

            return Ok(provisioning);
        }

        [HttpPut("{cityId:guid}/environment")]
        public async Task<IActionResult> UpdateEnvironment(
            [FromRoute] Guid cityId,
            [FromBody] UpdateCityEnvironmentRequest request,
            CancellationToken cancellationToken)
        {
            await _citiesClient.UpdateEnvironmentAsync(
                cityId: cityId,
                request: request,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpPut("{cityId:guid}/name")]
        public async Task<IActionResult> Rename(
            [FromRoute] Guid cityId,
            [FromBody] RenameCityRequest request,
            CancellationToken cancellationToken)
        {
            await _citiesClient.RenameCityAsync(
                cityId: cityId,
                request: request,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpPost("{cityId:guid}/archive")]
        public async Task<IActionResult> Archive(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            await _citiesClient.ArchiveCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpDelete("{cityId:guid}")]
        public async Task<IActionResult> Delete(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            await _citiesClient.DeleteCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        private static IReadOnlyList<CityPopulationDashboardMetricDto> BuildEconomyMetrics(EconomySummaryView summary)
        {
            return
            [
                new CityPopulationDashboardMetricDto(
                    Key: "economyBudgetBalance",
                    Label: "City budget balance",
                    Description: "Current city budget after collected taxes and municipal operating expenses.",
                    ValueKind: "money",
                    CurrentValue: summary.Balance,
                    DeltaYesterday: null,
                    DeltaMonth: null,
                    DeltaYear: null),
                new CityPopulationDashboardMetricDto(
                    Key: "economyGrossPayroll",
                    Label: "Gross payroll",
                    Description: "Cumulative wages paid to employed residents before the city income tax is withheld.",
                    ValueKind: "money",
                    CurrentValue: summary.TotalGrossPayroll,
                    DeltaYesterday: null,
                    DeltaMonth: null,
                    DeltaYear: null),
                new CityPopulationDashboardMetricDto(
                    Key: "economyIncomeTax",
                    Label: "Income tax",
                    Description: "Cumulative city income tax collected from resident payroll settlements.",
                    ValueKind: "money",
                    CurrentValue: summary.TotalIncomeTaxIncome,
                    DeltaYesterday: null,
                    DeltaMonth: null,
                    DeltaYear: null),
                new CityPopulationDashboardMetricDto(
                    Key: "economySalesTax",
                    Label: "Retail tax",
                    Description: "Cumulative city sales tax collected from household retail turnover.",
                    ValueKind: "money",
                    CurrentValue: summary.TotalSalesTaxIncome,
                    DeltaYesterday: null,
                    DeltaMonth: null,
                    DeltaYear: null),
                new CityPopulationDashboardMetricDto(
                    Key: "economyRetailTurnover",
                    Label: "Retail turnover",
                    Description: "Cumulative household spending routed through the local commerce loop.",
                    ValueKind: "money",
                    CurrentValue: summary.TotalRetailTurnover,
                    DeltaYesterday: null,
                    DeltaMonth: null,
                    DeltaYear: null),
                new CityPopulationDashboardMetricDto(
                    Key: "economyCityExpenses",
                    Label: "City expenses",
                    Description:
                    "Cumulative municipal operating expenses allocated back into city upkeep and services.",
                    ValueKind: "money",
                    CurrentValue: summary.TotalCityExpenses,
                    DeltaYesterday: null,
                    DeltaMonth: null,
                    DeltaYear: null)
            ];
        }

        private static T? TryDeserializeConflict<T>(DownstreamServiceException exception)
            where T : class
        {
            if (string.IsNullOrWhiteSpace(exception.Body))
                return null;

            try
            {
                return JsonSerializer.Deserialize<T>(
                    json: exception.Body,
                    options: new JsonSerializerOptions(JsonSerializerDefaults.Web));
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
