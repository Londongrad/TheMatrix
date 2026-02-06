using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.UseCases.Population.Common;
using Matrix.Population.Application.UseCases.Population.GetCityPopulationSummary;
using Matrix.Population.Application.UseCases.Population.GetCitizenPage;
using Matrix.Population.Application.UseCases.Population.InitializeCityPopulation;
using Matrix.Population.Application.UseCases.Population.SyncCityEnvironment;
using Matrix.Population.Contracts.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Population.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class PopulationController(ISender sender) : ControllerBase
    {
        private readonly ISender _sender = sender;

        [HttpPost("init")]
        public async Task<ActionResult<CityPopulationBootstrapSummaryDto>> InitializeCityPopulation(
            [FromBody] InitializeCityPopulationRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Environment);

            IReadOnlyCollection<ResidentialBuildingSeedItem> residentialBuildings =
                (request.ResidentialBuildings ?? Array.Empty<ResidentialBuildingSeedDto>())
               .Select(x => new ResidentialBuildingSeedItem(
                    ResidentialBuildingId: x.ResidentialBuildingId,
                    DistrictId: x.DistrictId,
                    ResidentCapacity: x.ResidentCapacity))
               .ToArray();

            CityPopulationBootstrapSummaryDto result = await _sender.Send(
                request: new InitializeCityPopulationCommand(
                    CityId: request.CityId,
                    CurrentDate: request.CurrentDate,
                    PeopleCount: request.PeopleCount,
                    RandomSeed: request.RandomSeed,
                    Environment: new CityPopulationEnvironmentInput(
                        ClimateZone: request.Environment.ClimateZone,
                        Hemisphere: request.Environment.Hemisphere,
                        UtcOffsetMinutes: request.Environment.UtcOffsetMinutes),
                    ResidentialBuildings: residentialBuildings),
                cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpPut("cities/{cityId:guid}/environment")]
        public async Task<IActionResult> SyncCityEnvironment(
            [FromRoute] Guid cityId,
            [FromBody] SyncCityEnvironmentRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            await _sender.Send(
                request: new SyncCityEnvironmentCommand(
                    CityId: cityId,
                    ClimateZone: request.ClimateZone,
                    Hemisphere: request.Hemisphere,
                    UtcOffsetMinutes: request.UtcOffsetMinutes),
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpGet("cities/{cityId:guid}/summary")]
        public async Task<ActionResult<CityPopulationSummaryDto>> GetCitySummary(
            [FromRoute] Guid cityId,
            [FromQuery] DateOnly currentDate,
            CancellationToken cancellationToken = default)
        {
            CityPopulationSummaryDto? result = await _sender.Send(
                request: new GetCityPopulationSummaryQuery(
                    CityId: cityId,
                    CurrentDate: currentDate),
                cancellationToken: cancellationToken);

            return result is null ? NotFound() : Ok(result);
        }

        [HttpGet("citizens")]
        public async Task<ActionResult<PagedResult<PersonDto>>> GetCitizensPage(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            var pagination = new Pagination(
                pageNumber: pageNumber,
                pageSize: pageSize);

            var query = new GetCitizensPageQuery(pagination);

            PagedResult<PersonDto> result = await _sender.Send(
                request: query,
                cancellationToken: cancellationToken);

            return Ok(result);
        }
    }
}
