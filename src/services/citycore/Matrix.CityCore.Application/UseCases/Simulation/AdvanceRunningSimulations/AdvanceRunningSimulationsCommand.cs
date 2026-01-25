using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.AdvanceRunningSimulations
{
    public sealed record AdvanceRunningSimulationsCommand(TimeSpan RealDelta)
        : IRequest<AdvanceRunningSimulationsResult>;
}
