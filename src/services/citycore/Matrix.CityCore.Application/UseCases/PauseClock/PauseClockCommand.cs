using MediatR;

namespace Matrix.CityCore.Application.UseCases.PauseClock
{
    public sealed record PauseClockCommand(Guid CityId) : IRequest<bool>;
}
