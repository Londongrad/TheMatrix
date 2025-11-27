using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.UseCases.GetCitizenPage;
using Matrix.Population.Application.UseCases.InitializePopulation;
using Matrix.Population.Application.UseCases.KillPerson;
using Matrix.Population.Application.UseCases.ResurrectPerson;
using Matrix.Population.Application.UseCases.UpdatePerson;
using Matrix.Population.Contracts.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Population.Api.Controllers
{
    [Authorize]
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
                new InitializePopulationCommand(peopleCount, randomSeed),
                cancellationToken);

            return NoContent();
        }

        [HttpGet("citizens")]
        public async Task<ActionResult<PagedResult<PersonDto>>> GetCitizensPage(
           [FromQuery] int pageNumber = 1,
           [FromQuery] int pageSize = 100,
           CancellationToken cancellationToken = default)
        {
            var pagination = new Pagination(pageNumber, pageSize);

            var query = new GetCitizensPageQuery(pagination);

            PagedResult<PersonDto> result = await _sender.Send(query, cancellationToken);

            return Ok(result);
        }

        [HttpPut("citizens/{id:guid}")]
        public async Task<ActionResult<PersonDto>> UpdateCitizen(
            Guid id,
            [FromBody] UpdatePersonRequest request,
            CancellationToken cancellationToken = default)
        {
            var cmd = new UpdatePersonCommand(id, request);
            var person = await _sender.Send(cmd, cancellationToken);
            return Ok(person);
        }

        [HttpPost("{id:guid}/resurrect")]
        public async Task<IActionResult> ResurrectPerson(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            PersonDto person = await _sender.Send(
                new ResurrectPersonCommand(id),
                cancellationToken);

            return Ok(person);
        }

        [HttpPost("{id:guid}/kill")]
        public async Task<IActionResult> KillPerson(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            PersonDto person = await _sender.Send(
                new KillPersonCommand(id),
                cancellationToken);

            return Ok(person);
        }
    }
}