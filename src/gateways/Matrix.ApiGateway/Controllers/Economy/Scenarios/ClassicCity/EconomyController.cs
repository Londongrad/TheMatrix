using Matrix.ApiGateway.DownstreamClients.Economy;
using Matrix.Economy.Contracts.Scenarios.ClassicCity.Budget.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.Economy.Scenarios.ClassicCity
{
    [Authorize]
    [ApiController]
    [Route("api/economy")]
    public sealed class EconomyController(IEconomyApiClient economyClient) : ControllerBase
    {
        private readonly IEconomyApiClient _economyClient = economyClient;

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
        {
            EconomySummaryView? summary = await _economyClient.GetSummaryAsync(cancellationToken);

            if (summary is null)
                return StatusCode(StatusCodes.Status502BadGateway);

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
