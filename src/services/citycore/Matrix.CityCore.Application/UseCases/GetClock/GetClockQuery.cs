using MediatR;

namespace Matrix.CityCore.Application.UseCases.GetClock
{
    public sealed record GetClockQuery(Guid CityId) : IRequest<ClockDto?>;
}
