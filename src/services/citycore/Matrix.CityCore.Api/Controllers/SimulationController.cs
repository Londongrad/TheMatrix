using Matrix.CityCore.Application.UseCases.GetCurrentSimulationTime;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.CityCore.Api.Controllers
{
    [ApiController]
    [Route("api/citycore/[controller]")]
    public class SimulationController(ISender sender) : ControllerBase
    {
        private readonly ISender _sender = sender;

        /// <summary>
        /// Текущее игровое время и скорость симуляции.
        /// </summary>
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

        /// <summary>
        /// Простой health-check.
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health() => Ok(new { status = "ok" });
    }
}