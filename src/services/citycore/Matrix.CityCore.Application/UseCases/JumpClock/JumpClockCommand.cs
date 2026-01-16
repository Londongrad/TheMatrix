using MediatR;

namespace Matrix.CityCore.Application.UseCases.JumpClock
{
    public sealed record JumpClockCommand(
        Guid CityId,
        DateTimeOffset NewSimTimeUtc) : IRequest<bool>;
}
