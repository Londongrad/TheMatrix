using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.UseCases.Population.GetCitizenPage;
using Matrix.Population.Application.UseCases.Population.InitializePopulation;
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
        public async Task<IActionResult> InitializePopulation(
            [FromQuery] int peopleCount = 10_000,
            [FromQuery] int? randomSeed = null,
            CancellationToken cancellationToken = default)
        {
            await _sender.Send(
                request: new InitializePopulationCommand(
                    PeopleCount: peopleCount,
                    RandomSeed: randomSeed),
                cancellationToken: cancellationToken);

            return NoContent();
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
