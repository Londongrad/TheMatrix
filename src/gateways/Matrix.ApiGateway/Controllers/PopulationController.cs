using Matrix.ApiGateway.DownstreamClients.Population;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Contracts.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/population")]
    public class PopulationController(IPopulationApiClient populationClient) : ControllerBase
    {
        private readonly IPopulationApiClient _populationClient = populationClient;

        /// <summary>
        ///     Инициализировать популяцию через gateway.
        /// </summary>
        /// <remarks>
        ///     Проксирует вызов в Population API: POST /api/population/init
        /// </remarks>
        [HttpPost("init")]
        public async Task<IActionResult> InitializePopulation(
            [FromQuery] int peopleCount = 10_000,
            [FromQuery] int? randomSeed = null,
            CancellationToken cancellationToken = default)
        {
            await _populationClient.InitializePopulationAsync(
                peopleCount: peopleCount,
                randomSeed: randomSeed,
                cancellationToken: cancellationToken);

            return Accepted(
                new
                {
                    message = "Population initialization started."
                });
        }

        /// <summary>
        ///     Убить гражданина по ID.
        /// </summary>
        /// <remarks>
        ///     Проксирует вызов в Population API: POST /api/population/{id}/kill
        /// </remarks>
        [HttpPost("{id:guid}/kill")]
        public async Task<IActionResult> KillPerson(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            PersonDto person = await _populationClient.KillPersonAsync(
                id: id,
                cancellationToken: cancellationToken);
            return Ok(person);
        }

        [HttpPost("{id:guid}/resurrect")]
        public async Task<IActionResult> ResurrectPerson(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            PersonDto person =
                await _populationClient.ResurrectPersonAsync(
                    id: id,
                    cancellationToken: cancellationToken);
            return Ok(person);
        }

        [HttpPut("{id:guid}/update")]
        public async Task<IActionResult> UpdatePerson(
            Guid id,
            [FromBody] UpdatePersonRequest request,
            CancellationToken cancellationToken = default)
        {
            PersonDto person =
                await _populationClient.UpdatePersonAsync(
                    id: id,
                    request: request,
                    cancellationToken: cancellationToken);
            return Ok(person);
        }

        /// <summary>
        ///     Получить страницу граждан.
        /// </summary>
        /// <remarks>
        ///     Проксирует вызов в Population API: GET /api/population/citizens?pageNumber=&pageSize=
        /// </remarks>
        [HttpGet("citizens")]
        [ProducesResponseType(
            type: typeof(PagedResult<PersonDto>),
            statusCode: StatusCodes.Status200OK)]
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
