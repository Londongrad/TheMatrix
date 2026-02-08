using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.PauseClock
{
    public sealed record PauseClockCommand(Guid SimulationId) : IRequest<bool>;
}
