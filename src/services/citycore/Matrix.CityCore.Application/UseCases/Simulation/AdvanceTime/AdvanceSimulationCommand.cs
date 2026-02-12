using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.AdvanceTime
{
    public sealed record AdvanceSimulationCommand(
        Guid SimulationId,
        TimeSpan RealDelta) : IRequest<bool>;
}
