using Matrix.ApiGateway.DownstreamClients.Population.People;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Contracts.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.Population
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
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
        ///     Получить страницу граждан.
        /// </summary>
        /// <remarks>
        ///     Проксирует вызов в Population API: GET /api/population/citizens?pageNumber=&pageSize=
        /// </remarks>
        [HttpGet("citizens")]
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
