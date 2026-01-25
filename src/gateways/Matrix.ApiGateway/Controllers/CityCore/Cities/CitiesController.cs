using Matrix.ApiGateway.DownstreamClients.CityCore.Cities;
using Matrix.CityCore.Contracts.Cities.Requests;
using Matrix.CityCore.Contracts.Cities.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.CityCore.Cities
{
    [Authorize]
    [ApiController]
    [Route("api/cities")]
    public sealed class CitiesController(ICitiesApiClient citiesClient) : ControllerBase
    {
        private readonly ICitiesApiClient _citiesClient = citiesClient;

        [HttpPost]
        public async Task<ActionResult<CityCreatedView>> Create(
            [FromBody] CreateCityRequest request,
            CancellationToken cancellationToken)
        {
            CityCreatedView created = await _citiesClient.CreateCityAsync(
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
