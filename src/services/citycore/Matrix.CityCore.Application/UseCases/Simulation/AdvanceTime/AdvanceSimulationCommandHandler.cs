using Matrix.CityCore.Application.Services.Simulation;
using Matrix.CityCore.Application.Services.Simulation.Abstractions;
using Matrix.CityCore.Domain.Simulation;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.AdvanceTime
{
    public sealed class AdvanceSimulationCommandHandler(ISimulationAdvanceExecutor executor)
        : IRequestHandler<AdvanceSimulationCommand, bool>
    {
        public async Task<bool> Handle(
            AdvanceSimulationCommand request,
            CancellationToken cancellationToken)
        {
            SimulationAdvanceExecutionResult result = await executor.ExecuteAsync(
                simulationId: new SimulationId(request.SimulationId),
                realDelta: request.RealDelta,
                cancellationToken: cancellationToken);

            return result.Status != SimulationAdvanceExecutionStatus.NotFound;
        }
    }
}
