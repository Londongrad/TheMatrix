using Matrix.BuildingBlocks.Application.Authorization;
using Matrix.Population.Application.UseCases.Person.KillPerson;
using Matrix.Population.Application.UseCases.Person.ResurrectPerson;
using Matrix.Population.Contracts.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Population.Api.Controllers
{
    [ApiController]
    [Route("api/internal/[controller]/{personId:guid}")]
    public class PersonController(ISender sender) : ControllerBase
    {
        private readonly ISender _sender = sender;

        [HttpPost("resurrect")]
        [Authorize(Policy = PermissionKeys.PopulationPeopleResurrect)]
        public async Task<IActionResult> ResurrectPerson(
            [FromRoute] Guid personId,
            CancellationToken cancellationToken = default)
        {
            PersonDto person = await _sender.Send(
                request: new ResurrectPersonCommand(personId),
                cancellationToken: cancellationToken);

            return Ok(person);
        }

        [HttpPost("kill")]
        [Authorize(Policy = PermissionKeys.PopulationPeopleKill)]
        public async Task<IActionResult> KillPerson(
            [FromRoute] Guid personId,
            CancellationToken cancellationToken = default)
        {
            PersonDto person = await _sender.Send(
                request: new KillPersonCommand(personId),
                cancellationToken: cancellationToken);

            return Ok(person);
        }
    }
}
