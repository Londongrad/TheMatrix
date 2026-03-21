using MediatR;

namespace Matrix.SimulationCore.Application.UseCases.Simulation.AdvanceRunningSimulations
{
    public sealed record AdvanceRunningSimulationsCommand(TimeSpan RealDelta)
        : IRequest<AdvanceRunningSimulationsResult>;
}
