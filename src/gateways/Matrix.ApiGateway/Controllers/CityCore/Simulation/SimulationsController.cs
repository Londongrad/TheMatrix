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
    [Route("api/simulations/{simulationId:guid}")]
    public sealed class SimulationsController(ISimulationApiClient simulationClient) : ControllerBase
    {
        private readonly ISimulationApiClient _simulationClient = simulationClient;

        [HttpGet]
        public async Task<ActionResult<SimulationClockView?>> GetClock(
            [FromRoute] Guid simulationId,
            CancellationToken cancellationToken)
        {
            SimulationClockView clock = await _simulationClient.GetClockAsync(
                simulationId: simulationId,
                cancellationToken: cancellationToken);

            return Ok(clock);
        }

        [HttpPost("pause")]
        public async Task<IActionResult> PauseClock(
            [FromRoute] Guid simulationId,
            CancellationToken cancellationToken)
        {
            await _simulationClient.PauseClockAsync(
                simulationId: simulationId,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpPost("resume")]
        public async Task<IActionResult> ResumeClock(
            [FromRoute] Guid simulationId,
            CancellationToken cancellationToken)
        {
            await _simulationClient.ResumeClockAsync(
                simulationId: simulationId,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpPost("speed")]
        public async Task<IActionResult> SetClockSpeed(
            [FromRoute] Guid simulationId,
            [FromBody] SetCityClockSpeedRequestDto request,
            CancellationToken cancellationToken)
        {
            await _simulationClient.SetClockSpeedAsync(
                simulationId: simulationId,
                request: new SetSpeedRequest(request.Multiplier),
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpPost("jump")]
        public async Task<IActionResult> JumpClock(
            [FromRoute] Guid simulationId,
            [FromBody] JumpCityClockRequestDto request,
            CancellationToken cancellationToken)
        {
            await _simulationClient.JumpClockAsync(
                simulationId: simulationId,
                request: new JumpClockRequest(request.NewSimTimeUtc),
                cancellationToken: cancellationToken);

            return NoContent();
        }
    }
}
