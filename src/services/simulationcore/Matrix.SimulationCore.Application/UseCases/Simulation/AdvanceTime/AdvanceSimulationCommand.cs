using MediatR;

namespace Matrix.SimulationCore.Application.UseCases.Simulation.AdvanceTime
{
    public sealed record AdvanceSimulationCommand(
        Guid SimulationId,
        TimeSpan RealDelta) : IRequest<bool>;
}
