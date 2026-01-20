using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.JumpClock
{
    public sealed record JumpClockCommand(
        Guid CityId,
        DateTimeOffset NewSimTimeUtc) : IRequest<bool>;
}
