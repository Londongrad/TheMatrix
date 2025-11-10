using Matrix.ApiGateway.DownstreamClients.Population;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers
{
    [ApiController]
    [Route("api/population")]
    public class PopulationController(IPopulationApiClient populationClient) : ControllerBase
    {
        private readonly IPopulationApiClient _populationClient = populationClient;

        /// <summary>
        /// Запускает расчёт месячного дохода (через Population-сервис).
        /// </summary>
        [HttpPost("income/month/{month:int}")]
        public async Task<IActionResult> GenerateMonthlyIncome(int month, CancellationToken cancellationToken)
        {
            await _populationClient.TriggerMonthlyIncomeAsync(month, cancellationToken);

            // Пробрасываем ту же семантику, что и Population: команда принята
            return Accepted();
        }

        /// <summary>
        /// Инициализирует/перезапускает популяцию.
        /// </summary>
        [HttpPost("init")]
        public async Task<IActionResult> Initialize(
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