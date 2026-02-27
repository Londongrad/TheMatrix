using Matrix.Population.Application.UseCases.Person.KillPerson;
using Matrix.Population.Application.UseCases.Person.ResurrectPerson;
using Matrix.Population.Application.UseCases.Person.UpdatePerson;
using Matrix.Population.Contracts.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Population.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]/{personId:guid}")]
    public class PersonController(ISender sender) : ControllerBase
    {
        private readonly ISender _sender = sender;

        [HttpPost("resurrect")]
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
        public async Task<IActionResult> KillPerson(
            [FromRoute] Guid personId,
            CancellationToken cancellationToken = default)
        {
            PersonDto person = await _sender.Send(
                request: new KillPersonCommand(personId),
                cancellationToken: cancellationToken);

            return Ok(person);
        }

        [HttpPut]
        public async Task<IActionResult> UpdatePerson(
            [FromRoute] Guid personId,
            [FromBody] UpdatePersonRequest request,
            CancellationToken cancellationToken = default)
        {
            PersonDto person = await _sender.Send(
                request: new UpdatePersonCommand(
                    Id: personId,
                    FullName: request.FullName,
                    EducationLevel: request.EducationLevel,
                    Health: request.Health,
                    Happiness: request.Happiness,
                    Energy: request.Energy,
                    Stress: request.Stress,
                    SocialNeed: request.SocialNeed),
                cancellationToken: cancellationToken);

            return Ok(person);
        }
    }
}
