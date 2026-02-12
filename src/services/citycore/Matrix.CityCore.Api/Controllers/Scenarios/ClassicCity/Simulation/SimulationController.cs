using Matrix.CityCore.Application.UseCases.Simulation.GetClock;
using Matrix.CityCore.Application.UseCases.Simulation.JumpClock;
using Matrix.CityCore.Application.UseCases.Simulation.PauseClock;
using Matrix.CityCore.Application.UseCases.Simulation.ResumeClock;
using Matrix.CityCore.Application.UseCases.Simulation.SetClockSpeed;
using Matrix.CityCore.Contracts.Simulation.Requests;
using Matrix.CityCore.Contracts.Simulation.Views;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.CityCore.Api.Controllers.Scenarios.ClassicCity.Simulation
{
    [ApiController]
    [Authorize]
    [Route("api/cities/{cityId:guid}/simulation")]
    public sealed class SimulationController(IMediator mediator) : ControllerBase
    {
        [HttpGet]
        public async Task<IResult> GetClock(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            ClockDto? clock = await mediator.Send(
                request: new GetClockQuery(cityId),
                cancellationToken: cancellationToken);

            if (clock is null)
                return Results.NotFound();

            return Results.Ok(MapToView(clock));
        }

        [HttpPost("pause")]
        public async Task<IResult> Pause(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            bool updated = await mediator.Send(
                request: new PauseClockCommand(cityId),
                cancellationToken: cancellationToken);

            return updated
                ? Results.Ok()
                : Results.NotFound();
        }

        [HttpPost("resume")]
        public async Task<IResult> Resume(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            bool updated = await mediator.Send(
                request: new ResumeClockCommand(cityId),
                cancellationToken: cancellationToken);

            return updated
                ? Results.Ok()
                : Results.NotFound();
        }

        [HttpPost("speed")]
        public async Task<IResult> SetSpeed(
            [FromRoute] Guid cityId,
            [FromBody] SetSpeedRequest request,
            CancellationToken cancellationToken)
        {
            bool updated = await mediator.Send(
                request: new SetClockSpeedCommand(
                    SimulationId: cityId,
                    Multiplier: request.Multiplier),
                cancellationToken: cancellationToken);

            return updated
                ? Results.Ok()
                : Results.NotFound();
        }

        [HttpPost("jump")]
        public async Task<IResult> Jump(
            [FromRoute] Guid cityId,
            [FromBody] JumpClockRequest request,
            CancellationToken cancellationToken)
        {
            bool updated = await mediator.Send(
                request: new JumpClockCommand(
                    SimulationId: cityId,
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
                HostKind: clock.HostKind,
                SimulationKind: clock.SimulationKind,
                SimTimeUtc: clock.SimTimeUtc,
                TickId: clock.TickId,
                Speed: clock.Speed,
                State: clock.State.ToString());
        }
    }
}
