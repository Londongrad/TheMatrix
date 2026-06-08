using Matrix.ApiGateway.DownstreamClients.Population.People;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Contracts;
using Matrix.Population.Contracts.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.Population
{
    [Authorize]
    [ApiController]
    [Route(PopulationApiRoutes.PeopleRoute)]
    public class PopulationController(IPopulationApiClient populationClient) : ControllerBase
    {
        private readonly IPopulationApiClient _populationClient = populationClient;

        [HttpGet]
        public async Task<ActionResult<PagedResult<PersonDto>>> GetPeoplePage(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            PagedResult<PersonDto> result = await _populationClient.GetPeoplePageAsync(
                pageNumber: pageNumber,
                pageSize: pageSize,
                cancellationToken: cancellationToken);

            return Ok(result);
        }
    }
}
