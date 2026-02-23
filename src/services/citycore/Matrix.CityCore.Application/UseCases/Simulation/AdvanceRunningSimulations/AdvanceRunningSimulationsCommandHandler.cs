using Matrix.CityCore.Application.Services.Simulation;
using Matrix.CityCore.Application.Services.Simulation.Abstractions;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.AdvanceRunningSimulations
{
    public sealed class AdvanceRunningSimulationsCommandHandler(
        ISimulationBatchAdvanceExecutor batchAdvanceExecutor)
        : IRequestHandler<AdvanceRunningSimulationsCommand, AdvanceRunningSimulationsResult>
    {
        public async Task<AdvanceRunningSimulationsResult> Handle(
            AdvanceRunningSimulationsCommand request,
            CancellationToken cancellationToken)
        {
            SimulationBatchAdvanceResult result = await batchAdvanceExecutor.ExecuteAsync(
                realDelta: request.RealDelta,
                cancellationToken: cancellationToken);

            return new AdvanceRunningSimulationsResult(
                ProcessedCount: result.ProcessedCount,
                AdvancedCount: result.AdvancedCount,
                SkippedCount: result.SkippedCount,
                FailedCount: result.FailedCount);
        }
    }
}
