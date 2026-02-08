using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.GetClock
{
    public sealed record GetClockQuery(Guid SimulationId) : IRequest<ClockDto?>;
}
