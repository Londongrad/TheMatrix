using Matrix.SimulationCore.Application.UseCases.Simulation.GetClock;
using Matrix.SimulationCore.Application.UseCases.Simulation.JumpClock;
using Matrix.SimulationCore.Application.UseCases.Simulation.PauseClock;
using Matrix.SimulationCore.Application.UseCases.Simulation.ResumeClock;
using Matrix.SimulationCore.Application.UseCases.Simulation.SetClockSpeed;
using Matrix.SimulationCore.Contracts.Simulation.Requests;
using Matrix.SimulationCore.Contracts.Simulation.Views;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.SimulationCore.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/simulations/{simulationId:guid}")]
    public sealed class SimulationsController(IMediator mediator) : ControllerBase
    {
        [HttpGet]
        public async Task<IResult> GetClock(
            [FromRoute] Guid simulationId,
            CancellationToken cancellationToken)
        {
            ClockDto? clock = await mediator.Send(
                request: new GetClockQuery(simulationId),
                cancellationToken: cancellationToken);

            if (clock is null)
                return Results.NotFound();

            return Results.Ok(MapToView(clock));
        }

        [HttpPost("pause")]
        public async Task<IResult> Pause(
            [FromRoute] Guid simulationId,
            CancellationToken cancellationToken)
        {
            bool updated = await mediator.Send(
                request: new PauseClockCommand(simulationId),
                cancellationToken: cancellationToken);

            return updated
                ? Results.Ok()
                : Results.NotFound();
        }

        [HttpPost("resume")]
        public async Task<IResult> Resume(
            [FromRoute] Guid simulationId,
            CancellationToken cancellationToken)
        {
            bool updated = await mediator.Send(
                request: new ResumeClockCommand(simulationId),
                cancellationToken: cancellationToken);

            return updated
                ? Results.Ok()
                : Results.NotFound();
        }

        [HttpPost("speed")]
        public async Task<IResult> SetSpeed(
            [FromRoute] Guid simulationId,
            [FromBody] SetSpeedRequest request,
            CancellationToken cancellationToken)
        {
            bool updated = await mediator.Send(
                request: new SetClockSpeedCommand(
                    SimulationId: simulationId,
                    Multiplier: request.Multiplier),
                cancellationToken: cancellationToken);

            return updated
                ? Results.Ok()
                : Results.NotFound();
        }

        [HttpPost("jump")]
        public async Task<IResult> Jump(
            [FromRoute] Guid simulationId,
            [FromBody] JumpClockRequest request,
            CancellationToken cancellationToken)
        {
            bool updated = await mediator.Send(
                request: new JumpClockCommand(
                    SimulationId: simulationId,
                    NewSimTimeUtc: request.NewSimTimeUtc),
                cancellationToken: cancellationToken);

            return updated
                ? Results.Ok()
                : Results.NotFound();
        }

        private static SimulationClockView MapToView(ClockDto clock)
        {
            return new SimulationClockView(
                SimulationId: clock.SimulationId,
                HostId: clock.HostId,
                HostKind: clock.HostTypeKey,
                SimulationKind: clock.ScenarioKey,
                SimTimeUtc: clock.SimTimeUtc,
                TickId: clock.TickId,
                Speed: clock.Speed,
                State: clock.State.ToString());
        }
    }
}
