using Matrix.ApiGateway.Contracts.CityCore.Simulation.Requests;
using Matrix.ApiGateway.DownstreamClients.CityCore.Simulation;
using Matrix.CityCore.Contracts.Simulation.Requests;
using Matrix.CityCore.Contracts.Simulation.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.CityCore.Simulation
{
    [Authorize]
    [ApiController]
    [Route("api/cities/{cityId:guid}/simulation")]
    public sealed class SimulationController(ICityCoreApiClient cityCoreClient) : ControllerBase
    {
        private readonly ICityCoreApiClient _cityCoreClient = cityCoreClient;

        [HttpGet]
        public async Task<ActionResult<SimulationClockView?>> GetClock(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            SimulationClockView clock = await _cityCoreClient.GetClockAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            return Ok(clock);
        }

        [HttpPost("pause")]
        public async Task<IActionResult> PauseClock(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            await _cityCoreClient.PauseClockAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpPost("resume")]
        public async Task<IActionResult> ResumeClock(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            await _cityCoreClient.ResumeClockAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpPost("speed")]
        public async Task<IActionResult> SetClockSpeed(
            [FromRoute] Guid cityId,
            [FromBody] SetCityClockSpeedRequestDto request,
            CancellationToken cancellationToken)
        {
            await _cityCoreClient.SetClockSpeedAsync(
                cityId: cityId,
                request: new SetSpeedRequest(request.Multiplier),
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpPost("jump")]
        public async Task<IActionResult> JumpClock(
            [FromRoute] Guid cityId,
            [FromBody] JumpCityClockRequestDto request,
            CancellationToken cancellationToken)
        {
            await _cityCoreClient.JumpClockAsync(
                cityId: cityId,
                request: new JumpClockRequest(request.NewSimTimeUtc),
                cancellationToken: cancellationToken);

            return NoContent();
        }
    }
}
