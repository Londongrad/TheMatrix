using Matrix.ApiGateway.Contracts.CityCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.DownstreamClients.CityCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.DownstreamClients.CityCore.Simulation;
using Matrix.ApiGateway.DownstreamClients.Population.People;
using Matrix.ApiGateway.Services.CityCore.Scenarios.ClassicCity.Cities;
using Matrix.CityCore.Contracts.Scenarios.ClassicCity.Cities.Requests;
using Matrix.CityCore.Contracts.Scenarios.ClassicCity.Cities.Views;
using Matrix.CityCore.Contracts.Simulation.Views;
using Matrix.Population.Contracts.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.CityCore.Scenarios.ClassicCity.Cities
{
    [Authorize]
    [ApiController]
    [Route("api/cities")]
    public sealed class CitiesController(
        ICitiesApiClient citiesClient,
        ISimulationApiClient simulationClient,
        IPopulationApiClient populationClient,
        ICityProvisioningService cityProvisioningService) : ControllerBase
    {
        private readonly ICitiesApiClient _citiesClient = citiesClient;
        private readonly ICityProvisioningService _cityProvisioningService = cityProvisioningService;
        private readonly IPopulationApiClient _populationClient = populationClient;
        private readonly ISimulationApiClient _simulationClient = simulationClient;

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

        [HttpGet("simulation-kinds")]
        public async Task<ActionResult<IReadOnlyList<SimulationKindCatalogItemView>>> GetSimulationKinds(
            CancellationToken cancellationToken)
        {
            IReadOnlyList<SimulationKindCatalogItemView> kinds = await _citiesClient.GetSimulationKindsAsync(
                cancellationToken: cancellationToken);

            return Ok(kinds);
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
            SimulationClockView clock = await _simulationClient.GetClockAsync(
                simulationId: cityId,
                cancellationToken: cancellationToken);

            var currentDate = DateOnly.FromDateTime(clock.SimTimeUtc.UtcDateTime);

            CityPopulationSummaryDto summary = await _populationClient.GetCityPopulationSummaryAsync(
                cityId: cityId,
                currentDate: currentDate,
                cancellationToken: cancellationToken);

            return Ok(summary);
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

        [HttpPost("{cityId:guid}/population-bootstrap/retry")]
        public async Task<ActionResult<CityProvisioningView>> RetryPopulationBootstrap(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CityProvisioningView provisioning = await _cityProvisioningService.RetryPopulationBootstrapAsync(
                cityId: cityId,
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
    }
}
