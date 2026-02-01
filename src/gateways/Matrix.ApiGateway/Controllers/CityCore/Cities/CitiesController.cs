using Matrix.ApiGateway.Contracts.CityCore.Cities;
using Matrix.ApiGateway.DownstreamClients.CityCore.Cities;
using Matrix.ApiGateway.Services.CityCore.Cities;
using Matrix.CityCore.Contracts.Cities.Requests;
using Matrix.CityCore.Contracts.Cities.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.CityCore.Cities
{
    [Authorize]
    [ApiController]
    [Route("api/cities")]
    public sealed class CitiesController(
        ICitiesApiClient citiesClient,
        ICityProvisioningService cityProvisioningService) : ControllerBase
    {
        private readonly ICitiesApiClient _citiesClient = citiesClient;
        private readonly ICityProvisioningService _cityProvisioningService = cityProvisioningService;

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
