using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.JumpClock
{
    public sealed record JumpClockCommand(
        Guid SimulationId,
        DateTimeOffset NewSimTimeUtc) : IRequest<bool>;
}
