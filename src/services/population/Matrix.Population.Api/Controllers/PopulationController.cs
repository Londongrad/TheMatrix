using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.UseCases.Population.GetCitizenPage;
using Matrix.Population.Application.UseCases.Population.InitializeCityPopulation;
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
                    ResidentialBuildings: residentialBuildings),
                cancellationToken: cancellationToken);

            return Ok(result);
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
