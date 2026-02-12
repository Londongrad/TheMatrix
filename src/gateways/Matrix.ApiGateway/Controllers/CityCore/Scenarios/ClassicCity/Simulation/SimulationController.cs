using Matrix.ApiGateway.Contracts.CityCore.Simulation.Requests;
using Matrix.ApiGateway.DownstreamClients.CityCore.Simulation;
using Matrix.CityCore.Contracts.Simulation.Requests;
using Matrix.CityCore.Contracts.Simulation.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.CityCore.Scenarios.ClassicCity.Simulation
{
    [Authorize]
    [ApiController]
    [Route("api/cities/{cityId:guid}/simulation")]
    public sealed class SimulationController(ISimulationApiClient simulationClient) : ControllerBase
    {
        private readonly ISimulationApiClient _simulationClient = simulationClient;

        [HttpGet]
        public async Task<ActionResult<SimulationClockView?>> GetClock(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            SimulationClockView clock = await _simulationClient.GetClockAsync(
                simulationId: cityId,
                cancellationToken: cancellationToken);

            return Ok(clock);
        }

        [HttpPost("pause")]
        public async Task<IActionResult> PauseClock(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            await _simulationClient.PauseClockAsync(
                simulationId: cityId,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpPost("resume")]
        public async Task<IActionResult> ResumeClock(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            await _simulationClient.ResumeClockAsync(
                simulationId: cityId,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpPost("speed")]
        public async Task<IActionResult> SetClockSpeed(
            [FromRoute] Guid cityId,
            [FromBody] SetSimulationClockSpeedRequestDto request,
            CancellationToken cancellationToken)
        {
            await _simulationClient.SetClockSpeedAsync(
                simulationId: cityId,
                request: new SetSpeedRequest(request.Multiplier),
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpPost("jump")]
        public async Task<IActionResult> JumpClock(
            [FromRoute] Guid cityId,
            [FromBody] JumpSimulationClockRequestDto request,
            CancellationToken cancellationToken)
        {
            await _simulationClient.JumpClockAsync(
                simulationId: cityId,
                request: new JumpClockRequest(request.NewSimTimeUtc),
                cancellationToken: cancellationToken);

            return NoContent();
        }
    }
}
