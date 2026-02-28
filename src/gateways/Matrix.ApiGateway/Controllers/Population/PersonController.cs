using Matrix.ApiGateway.DownstreamClients.Population.Person;
using Matrix.Population.Contracts.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.Population
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/{id:guid}")]
    public class PersonController(IPersonApiClient personApiClient) : ControllerBase
    {
        private readonly IPersonApiClient _personApiClient = personApiClient;

        [HttpPost("kill")]
        public async Task<IActionResult> KillPerson(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            PersonDto person = await _personApiClient.KillAsync(
                personId: id,
                cancellationToken: cancellationToken);

            return Ok(person);
        }

        [HttpPost("resurrect")]
        public async Task<IActionResult> ResurrectPerson(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            PersonDto person =
                await _personApiClient.ResurrectAsync(
                    personId: id,
                    cancellationToken: cancellationToken);

            return Ok(person);
        }
    }
}
