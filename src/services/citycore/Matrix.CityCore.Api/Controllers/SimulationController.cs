using Matrix.CityCore.Application.UseCases.GetCurrentSimulationTime;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.CityCore.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/citycore/[controller]")]
    public class SimulationController(ISender sender) : ControllerBase
    {
        private readonly ISender _sender = sender;

        [HttpGet("time")]
        public async Task<IActionResult> GetCurrentTime(CancellationToken cancellationToken)
        {
            var dto = await _sender.Send(new GetCurrentSimulationTimeQuery(), cancellationToken);

            return Ok(new
            {
                currentTimeUtc = dto.CurrentTime,
                simMinutesPerTick = dto.SimMinutesPerTick,
                isPaused = dto.IsPaused
            });
        }

        [HttpGet("health")]
        public IActionResult Health() => Ok(new { status = "ok" });
    }
}
