using Matrix.SimulationCore.Application.Services.Simulation;
using Matrix.SimulationCore.Application.Services.Simulation.Abstractions;
using MediatR;

namespace Matrix.SimulationCore.Application.UseCases.Simulation.AdvanceRunningSimulations
{
    public sealed class AdvanceRunningSimulationsCommandHandler(ISimulationBatchAdvanceExecutor batchAdvanceExecutor)
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
                NoStepDueCount: result.NoStepDueCount,
                LaggingCount: result.LaggingCount,
                FailedCount: result.FailedCount,
                TotalStepsProcessed: result.TotalStepsProcessed);
        }
    }
}
