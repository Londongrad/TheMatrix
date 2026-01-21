using Matrix.CityCore.Application.UseCases.Simulation.BootstrapCity;
using Matrix.CityCore.Application.UseCases.Simulation.GetClock;
using Matrix.CityCore.Application.UseCases.Simulation.JumpClock;
using Matrix.CityCore.Application.UseCases.Simulation.PauseClock;
using Matrix.CityCore.Application.UseCases.Simulation.ResumeClock;
using Matrix.CityCore.Application.UseCases.Simulation.SetClockSpeed;
using MediatR;

namespace Matrix.CityCore.Infrastructure.Public
{
    public sealed class CityCoreClockAppService(IMediator mediator) : ICityCoreClockAppService
    {
        public async Task<Guid> BootstrapAsync(
            DateTimeOffset startSimTimeUtc,
            CancellationToken cancellationToken)
        {
            return await mediator.Send(
                request: new BootstrapCityCommand(StartSimTimeUtc: startSimTimeUtc),
                cancellationToken: cancellationToken);
        }

        public async Task<CityClockView?> GetClockAsync(
            Guid cityId,
            CancellationToken cancellationToken)
        {
            ClockDto? clock = await mediator.Send(
                request: new GetClockQuery(cityId),
                cancellationToken: cancellationToken);

            return clock is null
                ? null
                : new CityClockView(
                    CityId: clock.CityId,
                    SimTimeUtc: clock.SimTimeUtc,
                    TickId: clock.TickId,
                    Speed: clock.Speed,
                    State: clock.State);
        }

        public Task<bool> PauseAsync(
            Guid cityId,
            CancellationToken cancellationToken)
        {
            return mediator.Send(
                request: new PauseClockCommand(cityId),
                cancellationToken: cancellationToken);
        }

        public Task<bool> ResumeAsync(
            Guid cityId,
            CancellationToken cancellationToken)
        {
            return mediator.Send(
                request: new ResumeClockCommand(cityId),
                cancellationToken: cancellationToken);
        }

        public Task<bool> SetSpeedAsync(
            Guid cityId,
            decimal multiplier,
            CancellationToken cancellationToken)
        {
            return mediator.Send(
                request: new SetClockSpeedCommand(
                    CityId: cityId,
                    Multiplier: multiplier),
                cancellationToken: cancellationToken);
        }

        public Task<bool> JumpAsync(
            Guid cityId,
            DateTimeOffset newSimTimeUtc,
            CancellationToken cancellationToken)
        {
            return mediator.Send(
                request: new JumpClockCommand(
                    CityId: cityId,
                    NewSimTimeUtc: newSimTimeUtc),
                cancellationToken: cancellationToken);
        }
    }
}
