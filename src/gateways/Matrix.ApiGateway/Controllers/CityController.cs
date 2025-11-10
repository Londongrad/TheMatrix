using Matrix.ApiGateway.DownstreamClients.CityCore;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers
{
    [ApiController]
    [Route("api/city")]
    public class CityController(ICityCoreApiClient cityCoreClient) : ControllerBase
    {
        private readonly ICityCoreApiClient _cityCoreClient = cityCoreClient;

        [HttpGet("time")]
        public async Task<IActionResult> GetCurrentTime(CancellationToken cancellationToken)
        {
            var dto = await _cityCoreClient.GetCurrentTimeAsync(cancellationToken);

            if (dto is null)
                return StatusCode(StatusCodes.Status502BadGateway);

            // Можно отдать dto как есть
            return Ok(dto);

            // либо при желании адаптировать под фронт,
            // если захочешь другие имена/формат
        }

        [HttpGet("health")]
        public async Task<IActionResult> Health(CancellationToken cancellationToken)
        {
            var isHealthy = await _cityCoreClient.HealthAsync(cancellationToken);

            return Ok(new { status = isHealthy ? "ok" : "degraded" });
        }
    }
}