using Matrix.ApiGateway.DownstreamClients.Economy;
using Matrix.ApiGateway.DownstreamClients.Economy.Models;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.Economy
{
    [ApiController]
    [Route("api/economy")]
    public class EconomyController(IEconomyApiClient economyClient) : ControllerBase
    {
        private readonly IEconomyApiClient _economyClient = economyClient;

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
        {
            EconomySummaryDto? summary = await _economyClient.GetSummaryAsync(cancellationToken);

            if (summary is null)
                return StatusCode(StatusCodes.Status502BadGateway);

            // Здесь можно адаптировать DTO под фронт, если нужно
            return Ok(summary);
        }

        [HttpGet("health")]
        public async Task<IActionResult> Health(CancellationToken cancellationToken)
        {
            bool isHealthy = await _economyClient.HealthAsync(cancellationToken);
            return Ok(
                new
                {
                    status = isHealthy
                        ? "ok"
                        : "degraded"
                });
        }
    }
}
