using Matrix.ApiGateway.DownstreamClients.Population.People;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Contracts.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.Population
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PopulationController(IPopulationApiClient populationClient) : ControllerBase
    {
        private readonly IPopulationApiClient _populationClient = populationClient;

        [HttpGet("citizens")]
        public async Task<ActionResult<PagedResult<PersonDto>>> GetCitizensPage(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            PagedResult<PersonDto> result = await _populationClient.GetCitizensPageAsync(
                pageNumber: pageNumber,
                pageSize: pageSize,
                cancellationToken: cancellationToken);

            return Ok(result);
        }
    }
}
