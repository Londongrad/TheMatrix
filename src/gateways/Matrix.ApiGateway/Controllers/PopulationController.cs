using Matrix.ApiGateway.DownstreamClients.Population;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Contracts.Models;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers
{
    [ApiController]
    [Route("api/population")]
    public class PopulationController(IPopulationApiClient populationClient) : ControllerBase
    {
        private readonly IPopulationApiClient _populationClient = populationClient;

        /// <summary>
        /// Инициализировать популяцию через gateway.
        /// </summary>
        /// <remarks>
        /// Проксирует вызов в Population API: POST /api/population/init
        /// </remarks>
        [HttpPost("init")]
        public async Task<IActionResult> InitializePopulation(
            [FromQuery] int peopleCount = 10_000,
            [FromQuery] int? randomSeed = null,
            CancellationToken cancellationToken = default)
        {
            await _populationClient.InitializePopulationAsync(
                peopleCount,
                randomSeed,
                cancellationToken);

            return Accepted(new { message = "Population initialization started." });
        }

        /// <summary>
        /// Убить гражданина по ID.
        /// </summary>
        /// <remarks>
        /// Проксирует вызов в Population API: POST /api/population/{id}/kill
        /// </remarks>
        [HttpPost("{id:guid}/kill")]
        public async Task<IActionResult> KillPerson(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            PersonDto person = await _populationClient.KillPersonAsync(id, cancellationToken);
            return Ok(person);
        }

        [HttpPost("{id:guid}/resurrect")]
        public async Task<IActionResult> ResurrectPerson(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            PersonDto person = await _populationClient.ResurrectPersonAsync(id, cancellationToken);
            return Ok(person);
        }

        [HttpPut("{id:guid}/update")]
        public async Task<IActionResult> UpdatePerson(
            Guid id,
            [FromBody] UpdatePersonRequest request,
            CancellationToken cancellationToken = default)
        {
            PersonDto person = await _populationClient.UpdatePersonAsync(id, request, cancellationToken);
            return Ok(person);
        }

        /// <summary>
        /// Получить страницу граждан.
        /// </summary>
        /// <remarks>
        /// Проксирует вызов в Population API: GET /api/population/citizens?pageNumber=&pageSize=
        /// </remarks>
        [HttpGet("citizens")]
        [ProducesResponseType(typeof(PagedResult<PersonDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<PersonDto>>> GetCitizensPage(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            var result = await _populationClient.GetCitizensPageAsync(
                pageNumber,
                pageSize,
                cancellationToken);

            return Ok(result);
        }

        /// <summary>
        /// Health-check Population через Gateway.
        /// </summary>
        [HttpGet("health")]
        public async Task<IActionResult> Health(CancellationToken cancellationToken)
        {
            var isHealthy = await _populationClient.HealthAsync(cancellationToken);

            return Ok(new { status = isHealthy ? "ok" : "degraded" });
        }
    }
}