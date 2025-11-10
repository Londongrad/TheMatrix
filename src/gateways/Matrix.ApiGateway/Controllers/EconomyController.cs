using Matrix.ApiGateway.DownstreamClients.Economy;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers
{
    [ApiController]
    [Route("api/economy")]
    public class EconomyController(IEconomyApiClient economyClient) : ControllerBase
    {
        private readonly IEconomyApiClient _economyClient = economyClient;

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
        {
            var summary = await _economyClient.GetSummaryAsync(cancellationToken);

            if (summary is null)
                return StatusCode(StatusCodes.Status502BadGateway);

            // Здесь можно адаптировать DTO под фронт, если нужно
            return Ok(summary);
        }

        [HttpGet("health")]
        public async Task<IActionResult> Health(CancellationToken cancellationToken)
        {
            var isHealthy = await _economyClient.HealthAsync(cancellationToken);
            return Ok(new { status = isHealthy ? "ok" : "degraded" });
        }
    }
}