using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.UseCases.GetCitizenPage;
using Matrix.Population.Application.UseCases.InitializePopulation;
using Matrix.Population.Application.UseCases.KillPerson;
using Matrix.Population.Application.UseCases.ResurrectPerson;
using Matrix.Population.Application.UseCases.UpdatePerson;
using Matrix.Population.Contracts.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Population.Api.Controllers
{
    [ApiController]
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
                request: new InitializePopulationCommand(PeopleCount: peopleCount, RandomSeed: randomSeed),
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpGet("citizens")]
        public async Task<ActionResult<PagedResult<PersonDto>>> GetCitizensPage(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            var pagination = new Pagination(pageNumber: pageNumber, pageSize: pageSize);

            var query = new GetCitizensPageQuery(pagination);

            PagedResult<PersonDto> result = await _sender.Send(request: query, cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpPut("citizens/{id:guid}")]
        public async Task<ActionResult<PersonDto>> UpdateCitizen(
            Guid id,
            [FromBody] UpdatePersonRequest request,
            CancellationToken cancellationToken = default)
        {
            var cmd = new UpdatePersonCommand(Id: id, Changes: request);
            PersonDto person = await _sender.Send(request: cmd, cancellationToken: cancellationToken);
            return Ok(person);
        }

        [HttpPost("{id:guid}/resurrect")]
        public async Task<IActionResult> ResurrectPerson(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            PersonDto person = await _sender.Send(
                request: new ResurrectPersonCommand(id),
                cancellationToken: cancellationToken);

            return Ok(person);
        }

        [HttpPost("{id:guid}/kill")]
        public async Task<IActionResult> KillPerson(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            PersonDto person = await _sender.Send(
                request: new KillPersonCommand(id),
                cancellationToken: cancellationToken);

            return Ok(person);
        }
    }
}
