using Matrix.Population.Application.DTOs;
using Matrix.Population.Application.UseCases.InitializePopulation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Population.Api.Controllers
{
    [ApiController]
    [Route("api/population/[controller]")]
    public class SimulationController(ISender sender) : ControllerBase
    {
        private readonly ISender _sender = sender;

        [HttpPost("init")]
        public async Task<IActionResult> InitializePopulation(
            [FromQuery] int peopleCount = 100,
            [FromQuery] int? randomSeed = null,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<PersonDto> people = await _sender.Send(
                new InitializePopulationCommand(peopleCount, randomSeed),
                cancellationToken);

            return Ok(people);
        }

        [HttpGet("health")]
        public IActionResult Health() => Ok(new { status = "ok" });
    }
}