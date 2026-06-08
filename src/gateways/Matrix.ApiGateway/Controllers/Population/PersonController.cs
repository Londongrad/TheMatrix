using Matrix.ApiGateway.DownstreamClients.Population.Person;
using Matrix.Population.Contracts;
using Matrix.Population.Contracts.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.Population
{
    [Authorize]
    [ApiController]
    [Route(PopulationApiRoutes.PersonRoute)]
    public class PersonController(IPersonApiClient personApiClient) : ControllerBase
    {
        private readonly IPersonApiClient _personApiClient = personApiClient;

        [HttpPost("kill")]
        public async Task<IActionResult> KillPerson(
            [FromRoute] Guid personId,
            CancellationToken cancellationToken = default)
        {
            PersonDto person = await _personApiClient.KillAsync(
                personId: personId,
                cancellationToken: cancellationToken);

            return Ok(person);
        }

        [HttpPost("resurrect")]
        public async Task<IActionResult> ResurrectPerson(
            [FromRoute] Guid personId,
            CancellationToken cancellationToken = default)
        {
            PersonDto person =
                await _personApiClient.ResurrectAsync(
                    personId: personId,
                    cancellationToken: cancellationToken);

            return Ok(person);
        }
    }
}
