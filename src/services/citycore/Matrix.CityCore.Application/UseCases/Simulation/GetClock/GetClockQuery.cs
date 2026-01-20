using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.GetClock
{
    public sealed record GetClockQuery(Guid CityId) : IRequest<ClockDto?>;
}
